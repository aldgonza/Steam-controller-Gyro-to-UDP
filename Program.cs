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
    public int PauseButton=1;
    public int CenterKey=(int)Keys.F8,PauseKey=(int)Keys.F9,CalibrateKey=(int)Keys.F10;
    public int CenterPad,TogglePausePad,CalibratePad;
}
public sealed class MainWindow : Form {
    readonly MotionEngine engine=new MotionEngine(); HidWorker worker; UdpClient sender;
    readonly Label status=new Label(), values=new Label(), instructions=new Label();
    readonly NumericUpDown port=new NumericUpDown(),yaw=new NumericUpDown(),pitch=new NumericUpDown();
    readonly CheckBox roll=new CheckBox(),invertYaw=new CheckBox(),invertPitch=new CheckBox(),enableSensor=new CheckBox();
    readonly ComboBox pauseButton=new ComboBox(); readonly Button start=new Button(),pause=new Button(),center=new Button(),calibrate=new Button();
    readonly Timer timer=new Timer(); bool running,oldF8,oldF9,oldF10,editingBindings; DateTime demoEnd=DateTime.MinValue;
    string settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.xml");
    Settings settings=new Settings(); readonly uint[] masks={0,0x20,0x20000,0x40000,0x80,0x100};
    public MainWindow() {
        Text="Steam controller Gyro to UDP  |  prototype 0.2"; ClientSize=new Size(650,535); MinimumSize=new Size(666,574);
        Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(245,247,250); StartPosition=FormStartPosition.CenterScreen;
        AutoScaleMode=AutoScaleMode.Dpi;
        try {if(File.Exists(settingsPath)) using(var f=File.OpenRead(settingsPath)) settings=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(f);} catch {settings=new Settings();}
        AddLabel("Controller motion → cockpit view",22,18,610,32,16,true);
        AddLabel("Steam Controller (2026) • wired USB or receiver • Windows x64",24,55,600,25,10,false);
        AddLabel("OpenTrack UDP port",24,102,180,28,10,false);
        port.SetBounds(213,102,100,28);port.Minimum=1024;port.Maximum=65535;port.Value=Math.Max(1024,Math.Min(65535,settings.Port));Controls.Add(port);
        AddLabel("Destination: 127.0.0.1 (this PC)",330,104,290,28,10,false);
        AddLabel("Yaw / pitch gain",24,145,180,28,10,false);
        SetupGain(yaw,213,145,settings.YawGain);SetupGain(pitch,330,145,settings.PitchGain);
        invertYaw.Text="Invert yaw";invertYaw.SetBounds(24,190,145,28);invertYaw.Checked=settings.InvertYaw;Controls.Add(invertYaw);
        invertPitch.Text="Invert pitch";invertPitch.SetBounds(184,190,145,28);invertPitch.Checked=settings.InvertPitch;Controls.Add(invertPitch);
        roll.Text="Send roll";roll.SetBounds(350,190,145,28);roll.Checked=settings.Roll;Controls.Add(roll);
        AddLabel("Hold to pause",24,235,180,28,10,false);
        pauseButton.SetBounds(213,233,250,30);pauseButton.DropDownStyle=ComboBoxStyle.DropDownList;
        pauseButton.Items.AddRange(new object[]{"None (keyboard only)","R3 / right stick click","L4","L5","R4","R5"});
        pauseButton.SelectedIndex=Math.Max(0,Math.Min(5,settings.PauseButton));Controls.Add(pauseButton);
        pauseButton.SelectedIndexChanged+=(s,e)=>{int selected=pauseButton.SelectedIndex;if(selected!=0&&(selected==settings.CenterPad||selected==settings.TogglePausePad||selected==settings.CalibratePad)){pauseButton.SelectedIndex=0;MessageBox.Show(this,"That button is assigned in Keybinds. Choose another button or remove its other assignment first.","Controller binding conflict");}};
        var keybinds=new Button {Text="Keybinds..."};keybinds.SetBounds(478,232,148,32);keybinds.Click+=(s,e)=>EditBindings();Controls.Add(keybinds);
        enableSensor.Text="Request raw motion from controller (transient IMU setting)";
        enableSensor.SetBounds(24,278,600,30);enableSensor.Checked=settings.EnableSensor;Controls.Add(enableSensor);
        start.Text="Start bridge";start.SetBounds(24,322,143,36);start.Click+=(s,e)=>Toggle();Controls.Add(start);
        center.Text="Center\n"+Hotkeys.Label(settings.CenterKey);center.SetBounds(177,322,137,36);center.Click+=(s,e)=>engine.Recenter();Controls.Add(center);
        pause.Text="Pause\n"+Hotkeys.Label(settings.PauseKey);pause.SetBounds(324,322,137,36);pause.Click+=(s,e)=>TogglePause();Controls.Add(pause);
        calibrate.Text="Calibrate\n"+Hotkeys.Label(settings.CalibrateKey);calibrate.SetBounds(471,322,155,36);calibrate.Click+=(s,e)=>engine.Calibrate();Controls.Add(calibrate);
        status.SetBounds(24,375,602,43);status.Text="Ready. Start, then leave the controller still for 2 seconds.";Controls.Add(status);
        values.SetBounds(24,422,605,25);values.Font=new Font("Consolas",10);Controls.Add(values);
        instructions.SetBounds(24,454,610,43);instructions.Text="OpenTrack: UDP over network, port 4242 → freetrack 2.0 Enhanced.\nNuclear Option: enable TrackIR for cockpit camera.";Controls.Add(instructions);
        var demo=new LinkLabel {Text="Test OpenTrack with 5 seconds of simulated yaw"};demo.SetBounds(24,505,600,25);
        demo.LinkClicked+=(s,e)=> {if(!running) Toggle();if(running) demoEnd=DateTime.UtcNow.AddSeconds(5);};Controls.Add(demo);
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
    void EditBindings() {
        editingBindings=true;lock(engine.Gate)engine.BindingActionsSuspended=true;
        try {using(var dialog=new KeybindWindow(settings,pauseButton.SelectedIndex))if(dialog.ShowDialog(this)==DialogResult.OK)SaveSettings();}
        finally {oldF8=Hotkeys.Down(settings.CenterKey);oldF9=Hotkeys.Down(settings.PauseKey);oldF10=Hotkeys.Down(settings.CalibrateKey);lock(engine.Gate)engine.BindingActionsSuspended=false;editingBindings=false;}
    }
    static uint Mask(int index) {return index>=0&&index<KeybindWindow.ButtonMasks.Length?KeybindWindow.ButtonMasks[index]:0;}
    void TickBridge() {
        if(!editingBindings){if(Hotkeys.Pressed(settings.CenterKey,ref oldF8))engine.Recenter();if(Hotkeys.Pressed(settings.PauseKey,ref oldF9))TogglePause();if(Hotkeys.Pressed(settings.CalibrateKey,ref oldF10))engine.Calibrate();}
        lock(engine.Gate) {engine.PauseMask=masks[pauseButton.SelectedIndex];engine.RecenterMask=Mask(settings.CenterPad);engine.TogglePauseMask=Mask(settings.TogglePausePad);engine.CalibrateMask=Mask(settings.CalibratePad);}
        center.Text="Center\n"+Hotkeys.Label(settings.CenterKey);calibrate.Text="Calibrate\n"+Hotkeys.Label(settings.CalibrateKey);
        pause.Text=(engine.Paused?"Resume\n":"Pause\n")+Hotkeys.Label(settings.PauseKey);
        if(!running)return;
        worker.EnableSensor=enableSensor.Checked;
        var pose=engine.Pose((double)yaw.Value,(double)pitch.Value,roll.Checked,invertYaw.Checked,invertPitch.Checked);
        bool demo=DateTime.UtcNow<demoEnd;
        if(demo) {pose=new double[]{0,0,0,20*Math.Sin((5-(demoEnd-DateTime.UtcNow).TotalSeconds)*Math.PI),0,0};status.Text="SIMULATED YAW TEST — watch OpenTrack's octopus.";}
        else lock(engine.Gate) {
            if(!engine.Ready)status.Text=worker.Status+"\n"+engine.CalibrationStatus;
            else if(engine.Age>0.5)status.Text="Motion timestamps stalled. Check Steam's gyro setting or reconnect controller.";
            else if(engine.Paused || (engine.PauseMask!=0&&(engine.Buttons&engine.PauseMask)!=0))status.Text="Paused: release the hold button, or use your toggle-pause binding.";
            else status.Text=worker.Status+"\nCalibrated. Use Keybinds to change center, pause or recalibration controls.";
            values.Text=String.Format("Yaw {0,6:0.0}°  Pitch {1,6:0.0}°  {2:0} Hz  Buttons {3:X8}",pose[3],pose[4],engine.Rate,engine.Buttons);
        }
        try {var packet=MotionEngine.Packet(pose);sender.Send(packet,packet.Length);} catch(SocketException) {status.Text="UDP receiver unavailable. Start OpenTrack with the matching port.";}
    }
    void Cleanup() {
        timer.Stop();Stop();
        SaveSettings();
    }
    void SaveSettings() {
        try {settings.Port=(int)port.Value;settings.YawGain=yaw.Value;settings.PitchGain=pitch.Value;settings.Roll=roll.Checked;settings.InvertYaw=invertYaw.Checked;settings.InvertPitch=invertPitch.Checked;settings.EnableSensor=enableSensor.Checked;settings.PauseButton=pauseButton.SelectedIndex;
            using(var f=File.Create(settingsPath))new XmlSerializer(typeof(Settings)).Serialize(f,settings);
        } catch { /* Portable app can run from a read-only location. */ }
    }
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        string folder=AppDomain.CurrentDomain.BaseDirectory;
        if(args.Length>0 && args[0]=="--self-test") {try {Tests.Run();File.WriteAllText(Path.Combine(folder,"self-test.txt"),"PASS: motion/packet tests; keyboard modifiers/conflicts; controller toggle edges; binding suspension; settings persistence and v0.1 migration. Network delivery is a separate --udp-test.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"self-test.txt"),ex.ToString());return 1;}}
        if(args.Length>0 && args[0]=="--udp-test") {try {Tests.UdpLoopback();File.WriteAllText(Path.Combine(folder,"udp-test.txt"),"PASS: 48-byte OpenTrack UDP packet sent and received over localhost.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"udp-test.txt"),"FAILED: localhost UDP delivery test.\r\n"+ex);return 1;}}
        if(args.Length>0 && args[0]=="--probe") {try {var lines=new System.Collections.Generic.List<string>();foreach(var d in NativeHid.Enumerate()) {lines.Add(d.ToString()+" candidate="+NativeHid.Supported(d));lines.Add(d.Path);}if(lines.Count==0)lines.Add("No Valve HID interfaces visible to this process.");File.WriteAllLines(Path.Combine(folder,"diagnostics.txt"),lines.ToArray());return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"diagnostics.txt"),ex.ToString());return 1;}}
        if(args.Length>0 && args[0]=="--ui-smoke") {Application.EnableVisualStyles();using(var form=new MainWindow()){form.Opacity=0;form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(folder,"ui-check.png"));}form.Close();}return 0;}
        if(args.Length>0 && args[0]=="--keybinds-ui-smoke") {Application.EnableVisualStyles();using(var form=new KeybindWindow(new Settings(),1)){form.Opacity=0;form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(folder,"keybinds-ui-check.png"));}form.Close();}return 0;}
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainWindow());return 0;
    }
}
}
