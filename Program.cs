using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SteamGyroOpenTrack {
public sealed class Settings {
    public int Port=4242; public decimal YawGain=1,PitchGain=1; public bool Roll,InvertYaw,InvertPitch,EnableSensor=true;
    public string CenterKey="F8",PauseKey="F9",CalibrateKey="F10";
    public string HoldPauseKey="None";
}
public sealed class MainWindow : Form {
    readonly MotionEngine engine=new MotionEngine(); HidWorker worker; UdpClient sender;
    readonly Label status=new Label(), values=new Label(), instructions=new Label();
    readonly NumericUpDown port=new NumericUpDown(),yaw=new NumericUpDown(),pitch=new NumericUpDown();
    readonly CheckBox roll=new CheckBox(),invertYaw=new CheckBox(),invertPitch=new CheckBox(),enableSensor=new CheckBox();
    readonly Button start=new Button(),pause=new Button(),center=new Button(),calibrate=new Button();
    readonly Timer timer=new Timer(); bool running,oldF8,oldF9,oldF10,settingsLoadFailed,editingBindings; DateTime demoEnd=DateTime.MinValue;
    int centerKey,pauseKey,calibrateKey,holdPauseKey;
    string settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.xml");
    Settings settings=new Settings();
    public MainWindow() {
        Text="Gyro to UDP"; ClientSize=new Size(650,480); MinimumSize=new Size(666,519);
        Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(245,247,250); StartPosition=FormStartPosition.CenterScreen;
        AutoScaleMode=AutoScaleMode.Dpi;
        string configWarning="";
        try {if(File.Exists(settingsPath)) using(var f=File.OpenRead(settingsPath)) settings=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(f);} catch {settings=new Settings();settingsLoadFailed=true;configWarning="Could not read settings.xml. Using defaults; existing file will be preserved.";}
        if(!Hotkeys.TryParse(settings.CenterKey,out centerKey)||!Hotkeys.TryParse(settings.PauseKey,out pauseKey)||!Hotkeys.TryParse(settings.CalibrateKey,out calibrateKey)||!Hotkeys.TryParse(settings.HoldPauseKey,out holdPauseKey)) {
            configWarning="Invalid shortcut in settings.xml. Unrecognized shortcuts are disabled.";
            LoadBindings();
        }
        AddLabel("Steam Controller (2026) | wired USB or receiver | Windows x64",24,55,600,25,10,false);
        AddLabel("OpenTrack UDP port",24,102,180,28,10,false);
        port.SetBounds(213,102,100,28);port.Minimum=1024;port.Maximum=65535;port.Value=Math.Max(1024,Math.Min(65535,settings.Port));Controls.Add(port);
        AddLabel("Destination: 127.0.0.1 (this PC)",330,104,290,28,10,false);
        AddLabel("Yaw / pitch gain",24,145,180,28,10,false);
        SetupGain(yaw,213,145,settings.YawGain);SetupGain(pitch,330,145,settings.PitchGain);
        invertYaw.Text="Invert yaw";invertYaw.SetBounds(24,190,145,28);invertYaw.Checked=settings.InvertYaw;Controls.Add(invertYaw);
        invertPitch.Text="Invert pitch";invertPitch.SetBounds(184,190,145,28);invertPitch.Checked=settings.InvertPitch;Controls.Add(invertPitch);
        roll.Text="Send roll";roll.SetBounds(350,190,145,28);roll.Checked=settings.Roll;Controls.Add(roll);
        var keyboard=new Button {Text="Keyboard shortcuts..."};keyboard.SetBounds(24,232,200,34);keyboard.Click+=(s,e)=>EditKeyboardBindings();Controls.Add(keyboard);
        AddLabel("Map a Steam paddle to the same key.",240,237,386,28,10,false);
        enableSensor.Text="Request raw motion from controller (transient IMU setting)";
        enableSensor.SetBounds(24,278,600,30);enableSensor.Checked=settings.EnableSensor;Controls.Add(enableSensor);
        start.Text="Start bridge";start.SetBounds(24,322,143,36);start.Click+=(s,e)=>Toggle();Controls.Add(start);
        center.Text="Center\n"+Hotkeys.Label(centerKey);center.SetBounds(177,322,137,40);center.Click+=(s,e)=>engine.Recenter();Controls.Add(center);
        pause.Text="Pause\n"+Hotkeys.Label(pauseKey);pause.SetBounds(324,322,137,40);pause.Click+=(s,e)=>TogglePause();Controls.Add(pause);
        calibrate.Text="Calibrate\n"+Hotkeys.Label(calibrateKey);calibrate.SetBounds(471,322,155,40);calibrate.Click+=(s,e)=>engine.Calibrate();Controls.Add(calibrate);
        status.SetBounds(24,375,602,43);status.Text=configWarning.Length==0?"Ready. Start, then leave the controller still for 2 seconds.":configWarning;Controls.Add(status);
        values.SetBounds(24,422,605,25);values.Font=new Font("Consolas",10);Controls.Add(values);
        instructions.SetBounds(24,454,610,25);instructions.Text="OpenTrack: UDP over network -> freetrack 2.0 Enhanced.";Controls.Add(instructions);
        var demo=new LinkLabel {Text="Test OpenTrack with 5 seconds of simulated yaw"};demo.SetBounds(24,485,600,25);
        demo.LinkClicked+=(s,e)=> {if(!running) Toggle();if(running) demoEnd=DateTime.UtcNow.AddSeconds(5);};Controls.Add(demo);
        foreach(Control control in Controls)control.Top-=40;
        timer.Interval=16;timer.Tick+=(s,e)=>TickBridge();timer.Start();FormClosing+=(s,e)=>Cleanup();
    }
    void AddLabel(string text,int x,int y,int w,int h,int size,bool bold) {var l=new Label {Text=text,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)};l.SetBounds(x,y,w,h);Controls.Add(l);}
    void SetupGain(NumericUpDown n,int x,int y,decimal value) {n.SetBounds(x,y,100,28);n.Minimum=0.1m;n.Maximum=5;n.DecimalPlaces=1;n.Increment=0.1m;n.Value=Math.Max(n.Minimum,Math.Min(n.Maximum,value));Controls.Add(n);}
    void TogglePause() {lock(engine.Gate) engine.Paused=!engine.Paused;}
    void Toggle() {
        if(running) {Stop();return;}
        try {sender=new UdpClient();sender.Connect(IPAddress.Loopback,(int)port.Value);worker=new HidWorker(engine);worker.EnableSensor=enableSensor.Checked;engine.Calibrate();worker.Start();running=true;start.Text="Stop bridge";port.Enabled=false;}
        catch(Exception ex) {Stop();status.Text=ex.Message;}
    }
    void Stop() {running=false;demoEnd=DateTime.MinValue;if(worker!=null){worker.Dispose();worker=null;}if(sender!=null){sender.Close();sender=null;}engine.Disconnected();start.Text="Start bridge";port.Enabled=true;status.Text="Stopped. OpenTrack will hold its last received pose.";}
    void LoadBindings() {
        Hotkeys.TryParse(settings.CenterKey,out centerKey);Hotkeys.TryParse(settings.PauseKey,out pauseKey);
        Hotkeys.TryParse(settings.CalibrateKey,out calibrateKey);Hotkeys.TryParse(settings.HoldPauseKey,out holdPauseKey);
    }
    void EditKeyboardBindings() {
        editingBindings=true;lock(engine.Gate)engine.HoldPaused=true;
        try {
            using(var dialog=new KeyboardMapperDialog(new int[]{centerKey,pauseKey,calibrateKey,holdPauseKey})) {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                int[] chosen=dialog.Bindings;
                string[] previous={settings.CenterKey,settings.PauseKey,settings.CalibrateKey,settings.HoldPauseKey};
                settings.CenterKey=Hotkeys.ConfigName(chosen[0]);settings.PauseKey=Hotkeys.ConfigName(chosen[1]);
                settings.CalibrateKey=Hotkeys.ConfigName(chosen[2]);settings.HoldPauseKey=Hotkeys.ConfigName(chosen[3]);
                if(!SaveSettings()) {
                    settings.CenterKey=previous[0];settings.PauseKey=previous[1];settings.CalibrateKey=previous[2];settings.HoldPauseKey=previous[3];
                    MessageBox.Show(this,"Could not save settings.xml. Check that the folder is writable and the existing XML is valid. Your previous shortcuts are still active.","Shortcuts not saved");return;
                }
                LoadBindings();
                center.Text="Center\n"+Hotkeys.Label(centerKey);calibrate.Text="Calibrate\n"+Hotkeys.Label(calibrateKey);
            }
        }finally{
            // A key used to finish capture must be released before it can trigger an action.
            oldF8=Hotkeys.Down(centerKey);oldF9=Hotkeys.Down(pauseKey);oldF10=Hotkeys.Down(calibrateKey);
            lock(engine.Gate)engine.HoldPaused=Hotkeys.Down(holdPauseKey);
            editingBindings=false;
        }
    }
    void TickBridge() {
        if(!editingBindings){if(Hotkeys.Pressed(centerKey,ref oldF8))engine.Recenter();if(Hotkeys.Pressed(pauseKey,ref oldF9))TogglePause();if(Hotkeys.Pressed(calibrateKey,ref oldF10))engine.Calibrate();}
        lock(engine.Gate)engine.HoldPaused=editingBindings||Hotkeys.Down(holdPauseKey);
        pause.Text=(engine.Paused?"Resume\n":"Pause\n")+Hotkeys.Label(pauseKey);
        if(!running)return;
        worker.EnableSensor=enableSensor.Checked;
        var pose=engine.Pose((double)yaw.Value,(double)pitch.Value,roll.Checked,invertYaw.Checked,invertPitch.Checked);
        bool demo=DateTime.UtcNow<demoEnd;
        if(demo) {pose=new double[]{0,0,0,20*Math.Sin((5-(demoEnd-DateTime.UtcNow).TotalSeconds)*Math.PI),0,0};status.Text="SIMULATED YAW TEST — watch OpenTrack's octopus.";}
        else lock(engine.Gate) {
            if(!engine.Ready)status.Text=worker.Status+"\n"+engine.CalibrationStatus;
            else if(engine.Age>0.5)status.Text="Motion timestamps stalled. Check Steam's gyro setting or reconnect controller.";
            else if(engine.Paused || engine.HoldPaused)status.Text="Paused: release the hold shortcut, or use your toggle-pause shortcut.";
            else status.Text=worker.Status+"\nCalibrated.";
            values.Text=String.Format("Yaw {0,6:0.0}°  Pitch {1,6:0.0}°  {2:0} Hz",pose[3],pose[4],engine.Rate);
        }
        try {var packet=MotionEngine.Packet(pose);sender.Send(packet,packet.Length);} catch(SocketException) {status.Text="UDP receiver unavailable. Start OpenTrack with the matching port.";}
    }
    void Cleanup() {
        timer.Stop();Stop();
        SaveSettings();
    }
    bool SaveSettings() {
        if(settingsLoadFailed)return false;
        try {settings.Port=(int)port.Value;settings.YawGain=yaw.Value;settings.PitchGain=pitch.Value;settings.Roll=roll.Checked;settings.InvertYaw=invertYaw.Checked;settings.InvertPitch=invertPitch.Checked;settings.EnableSensor=enableSensor.Checked;
            using(var f=File.Create(settingsPath))new XmlSerializer(typeof(Settings)).Serialize(f,settings);
            return true;
        } catch {return false;}
    }
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        string folder=AppDomain.CurrentDomain.BaseDirectory;
        if(args.Length>0 && args[0]=="--self-test") {try {Tests.Run();File.WriteAllText(Path.Combine(folder,"self-test.txt"),"PASS: motion/packet tests; keyboard capture and Escape cancellation; shortcut conflicts; keyboard hold pause; settings persistence and legacy migration; direct controller actions removed. Network delivery is a separate --udp-test.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"self-test.txt"),ex.ToString());return 1;}}
        if(args.Length>0 && args[0]=="--udp-test") {try {Tests.UdpLoopback();File.WriteAllText(Path.Combine(folder,"udp-test.txt"),"PASS: 48-byte OpenTrack UDP packet sent and received over localhost.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"udp-test.txt"),"FAILED: localhost UDP delivery test.\r\n"+ex);return 1;}}
        if(args.Length>0 && args[0]=="--probe") {try {var lines=new System.Collections.Generic.List<string>();foreach(var d in NativeHid.Enumerate()) {lines.Add(d.ToString()+" candidate="+NativeHid.Supported(d));lines.Add(d.Path);}if(lines.Count==0)lines.Add("No Valve HID interfaces visible to this process.");File.WriteAllLines(Path.Combine(folder,"diagnostics.txt"),lines.ToArray());return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"diagnostics.txt"),ex.ToString());return 1;}}
        if(args.Length>0 && args[0]=="--ui-smoke") {Application.EnableVisualStyles();using(var form=new MainWindow()){form.Opacity=0;form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(folder,"ui-check.png"));}form.Close();}return 0;}
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainWindow());return 0;
    }
}
}
