using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SteamGyroOpenTrack {
public static class Tests {
    static void Assert(bool b,string message) {if(!b)throw new Exception(message);}
    static void Near(double actual,double expected,double tolerance,string message) {Assert(Math.Abs(actual-expected)<tolerance,message+": "+actual+" != "+expected);}
    static void Put(byte[] b,int offset,short value) {Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,offset,2);}
    public static void Run() {
        foreach(byte id in new byte[]{0x42,0x45,0x47}) {
            var b=new byte[65];b[0]=id;Buffer.BlockCopy(BitConverter.GetBytes((uint)0x20),0,b,2,4);
            if(id==0x47) {Put(b,32,125);} else Buffer.BlockCopy(BitConverter.GetBytes((uint)4000),0,b,30,4);
            Put(b,34,16384);Put(b,36,-8192);Put(b,38,4096);Put(b,40,1638);Put(b,42,-819);Put(b,44,409);
            var s=Sample.Parse(b,65);Assert(s!=null,"Parser rejected state");Assert(s.Buttons==0x20,"Button offset");
            Near(s.X,99.9755859375,0.0001,"Gyro X");Near(s.Y,24.96337890625,0.0001,"Gyro Y mapping");Near(s.Z,49.98779296875,0.0001,"Gyro Z mapping");
            Near(s.Ax,1,0.0001,"Accel X");Near(s.Ay,0.25,0.0001,"Accel Y");Near(s.Az,0.5,0.0001,"Accel Z");
            Near(s.Tick*s.TickScale,0.004,0.000001,"Timestamp conversion");
        }
        Assert(Sample.Parse(new byte[10],10)==null,"Short report accepted");Assert(Sample.Parse(new byte[64],64)==null,"Non-state report accepted");
        var q=Quaternion.Identity;for(int i=0;i<250;i++)q=(q*Quaternion.Increment(0,90,0,0.004)).Normalized();Near(q.Angles()[0],90,0.00001,"90 degree yaw");
        Near(Quaternion.Increment(45,0,0,1).Angles()[1],45,0.00001,"Pitch");Near(Quaternion.Increment(0,0,35,1).Angles()[2],35,0.00001,"Roll");
        var e=new MotionEngine();uint tick=0xfffff000;
        for(int i=0;i<600;i++){tick=unchecked(tick+4000);e.Update(new Sample{Tick=tick,TickBits=32,TickScale=0.000001,X=0.5,Y=-0.25,Z=0.1,Ay=1});}
        Assert(e.Ready&&!e.Calibrating,"Calibration failed");Near(e.BiasY,-0.25,0.00001,"Bias");
        e.Recenter();for(int i=0;i<250;i++){tick=unchecked(tick+4000);e.Update(new Sample{Tick=tick,TickBits=32,TickScale=0.000001,X=0.5,Y=89.75,Z=0.1,Ay=1});}
        Near(e.Pose(1,1,false,false,false)[3],90,0.0001,"Integrated engine yaw after wrap");
        double angle=e.Pose(1,1,false,false,false)[3];
        e.HoldPaused=true;
        for(int i=0;i<50;i++){tick+=4000;e.Update(new Sample{Tick=tick,TickBits=32,TickScale=0.000001,Y=180,Ay=1,Buttons=0x20});}
        Near(e.Pose(1,1,false,false,false)[3],angle,0.0001,"Clutch failed");
        e.HoldPaused=false;
        e.Paused=true;tick+=4000;e.Update(new Sample{Tick=tick,TickBits=32,TickScale=0.000001,Y=180,Ay=1});Near(e.Pose(1,1,false,false,false)[3],angle,0.0001,"Toggle pause failed");
        e.Recenter();Near(e.Pose(1,1,false,false,false)[3],0,0.0001,"Center failed");
        e.Paused=false;tick+=2000000;e.Update(new Sample{Tick=tick,TickBits=32,TickScale=0.000001,Y=180,Ay=1});Near(e.Pose(1,1,false,false,false)[3],0,0.0001,"Long gap integrated");
        // 16-bit timestamp wrap and duplicate suppression.
        var shortTick=new MotionEngine();shortTick.Calibrating=false;shortTick.Ready=true;shortTick.Update(new Sample{Tick=65500,TickBits=16,TickScale=0.000032,Ay=1});
        var afterWrap=new Sample{Tick=89,TickBits=16,TickScale=0.000032,Y=90,Ay=1};shortTick.Update(afterWrap);shortTick.Update(afterWrap);
        Near(shortTick.Pose(1,1,false,false,false)[3],0.36,0.00001,"16 bit wrap or duplicate failed");
        var packet=MotionEngine.Packet(new double[]{0,0,0,12.5,-8.25,0});Assert(packet.Length==48,"Packet length");Near(BitConverter.ToDouble(packet,24),12.5,0.00001,"OpenTrack yaw index");
        bool rejected=false;try{MotionEngine.Packet(new double[]{0,0,0,Double.NaN,0,0});}catch(ArgumentException){rejected=true;}Assert(rejected,"NaN sent");
        int combo=(int)(Keys.Control|Keys.Alt|Keys.K);
        Assert(Hotkeys.Matches(combo,(int)Keys.K,true,true,false),"Modifier hotkey rejected");
        Assert(!Hotkeys.Matches(combo,(int)Keys.K,true,false,false),"Missing modifier accepted");
        Assert(!Hotkeys.Matches((int)Keys.F8,(int)Keys.F8,true,false,false),"Plain binding fired with extra Ctrl");
        Assert(!Hotkeys.Matches(0,0,false,false,false),"Disabled hotkey matched");
        int parsed;
        Assert(Hotkeys.Label((int)Keys.OemMinus)=="-","Minus display label");
        Assert(Hotkeys.Label((int)(Keys.Control|Keys.OemMinus))=="Ctrl+-","Modified minus display label");
        foreach(Keys punctuation in new[]{Keys.OemMinus,Keys.Oemplus,Keys.Oemcomma,Keys.OemPeriod,Keys.OemQuestion,Keys.OemSemicolon,Keys.OemQuotes,Keys.OemOpenBrackets,Keys.OemCloseBrackets,Keys.OemPipe,Keys.OemBackslash,Keys.Oemtilde}) {
            int binding=(int)(Keys.Control|punctuation);
            Assert(!Hotkeys.Label(binding).Contains("Oem"),"Technical punctuation label");
            Assert(Hotkeys.TryParse(Hotkeys.ConfigName(binding),out parsed)&&parsed==binding,"Punctuation settings round trip");
        }
        Assert(Hotkeys.TryParse("F8",out parsed)&&parsed==(int)Keys.F8,"Named key parsing");
        Assert(Hotkeys.TryParse("Ctrl+Alt+K",out parsed)&&parsed==combo,"Modifier config parsing");
        Assert(Hotkeys.TryParse("119",out parsed)&&parsed==(int)Keys.F8,"Legacy key parsing");
        Assert(Hotkeys.TryParse("None",out parsed)&&parsed==0,"Disabled named binding");
        Assert(Hotkeys.TryParse("0",out parsed)&&parsed==0,"Disabled legacy binding");
        Assert(!Hotkeys.TryParse("NotARealKey",out parsed)&&parsed==0,"Invalid key accepted");
        Assert(!Hotkeys.TryParse("Ctrl",out parsed),"Modifier-only key accepted");
        Assert(!Hotkeys.TryParse("99999999",out parsed),"Invalid key bits accepted");
        int captured;
        Assert(KeyboardCapture.Interpret(Keys.Escape,out captured)==CaptureResult.Cancelled,"Escape did not cancel capture");
        Assert(KeyboardCapture.Interpret(Keys.ControlKey,out captured)==CaptureResult.Ignored,"Modifier captured alone");
        Assert(KeyboardCapture.Interpret(Keys.Shift|Keys.F13,out captured)==CaptureResult.Accepted&&captured==(int)(Keys.Shift|Keys.F13),"Modified key capture");
        Assert(KeyboardCapture.Interpret(Keys.Tab,out captured)==CaptureResult.Accepted&&captured==(int)Keys.Tab,"Tab capture");
        Assert(KeyboardCapture.Interpret(Keys.Enter,out captured)==CaptureResult.Accepted&&captured==(int)Keys.Enter,"Enter capture");
        Assert(KeyboardCapture.Interpret(Keys.Back,out captured)==CaptureResult.Accepted&&captured==(int)Keys.Back,"Backspace capture");
        Assert(!KeyboardCapture.Distinct(new int[]{combo,0,combo,0}),"Duplicate shortcuts accepted");
        Assert(KeyboardCapture.Distinct(new int[]{0,0,combo,0}),"Disabled shortcuts rejected as duplicates");
        using(var capture=new CaptureShortcutDialog("Center view")) {
            capture.HandleShortcut(Keys.ControlKey);Assert(capture.DialogResult==DialogResult.None,"Modifier prematurely closed capture");
            capture.HandleShortcut(Keys.Escape);Assert(capture.DialogResult==DialogResult.Cancel,"Capture cancellation result");
        }
        using(var capture=new CaptureShortcutDialog("Toggle pause")) {
            capture.HandleShortcut(Keys.Control|Keys.F12);Assert(capture.DialogResult==DialogResult.OK&&capture.CapturedKey==(int)(Keys.Control|Keys.F12),"Capture acceptance result");
        }
        var original=new int[]{(int)Keys.F8,(int)Keys.F9,(int)Keys.F10,0};
        using(var mapper=new KeyboardMapperDialog(original)) {
            original[0]=0;Assert(mapper.Bindings[0]==(int)Keys.F8,"Mapper mutated or reused caller settings");
            var detached=mapper.Bindings;detached[0]=0;Assert(mapper.Bindings[0]==(int)Keys.F8,"Mapper exposed live settings");
        }
        var serializer=new XmlSerializer(typeof(Settings));var saved=new Settings{CenterKey="Ctrl+Alt+K",HoldPauseKey="F7",CalibrateKey="None"};string xml;
        using(var writer=new StringWriter()){serializer.Serialize(writer,saved);xml=writer.ToString();}
        using(var reader=new StringReader(xml)){var restored=(Settings)serializer.Deserialize(reader);Assert(restored.CenterKey=="Ctrl+Alt+K"&&restored.HoldPauseKey=="F7"&&restored.CalibrateKey=="None","Binding persistence");}
        Assert(!xml.Contains("PauseButton")&&!xml.Contains("CenterPad")&&!xml.Contains("TogglePausePad")&&!xml.Contains("CalibratePad"),"Removed controller mappings were serialized");
        using(var reader=new StringReader("<Settings><PauseButton>1</PauseButton></Settings>")){var old=(Settings)serializer.Deserialize(reader);Assert(old.CenterKey=="F8"&&old.PauseKey=="F9"&&old.CalibrateKey=="F10","v0.1 defaults migration");}
        using(var reader=new StringReader("<Settings><CenterKey>119</CenterKey><PauseKey>120</PauseKey><CalibrateKey>121</CalibrateKey><CenterPad>2</CenterPad><PauseButton>1</PauseButton></Settings>")){var old=(Settings)serializer.Deserialize(reader);Assert(Hotkeys.TryParse(old.CenterKey,out parsed)&&parsed==(int)Keys.F8&&old.HoldPauseKey=="None","Legacy keyboard config migration");}
        var buttons=new MotionEngine{Calibrating=false,Ready=true};
        buttons.Update(new Sample{Tick=1000,TickBits=32,TickScale=0.000001,Buttons=0x80});
        buttons.Update(new Sample{Tick=5000,TickBits=32,TickScale=0.000001,Buttons=0x20,Y=90,Ay=1});
        Assert(!buttons.Paused&&!buttons.HoldPaused&&!buttons.Calibrating,"Raw controller buttons still trigger actions");
        Near(buttons.Pose(1,1,false,false,false)[3],0.36,0.00001,"Raw controller button still pauses motion");
    }
    public static void UdpLoopback() {
        var packet=MotionEngine.Packet(new double[]{0,0,0,12.5,-8.25,0});
        using(var listener=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))using(var sender=new UdpClient()){
            listener.Client.ReceiveTimeout=1000;sender.Send(packet,packet.Length,(IPEndPoint)listener.Client.LocalEndPoint);IPEndPoint remote=null;var got=listener.Receive(ref remote);
            Assert(got.Length==48,"UDP loopback size");Near(BitConverter.ToDouble(got,32),-8.25,0.00001,"UDP pitch");
        }
    }
}
}
