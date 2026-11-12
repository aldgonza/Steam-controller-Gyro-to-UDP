using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SteamGyroOpenTrack {
public static class Hotkeys {
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    public static string Label(int value) {return value==0?"None":new KeysConverter().ConvertToString((Keys)value);}
    public static bool Matches(int value,int key,bool ctrl,bool alt,bool shift) {
        return value!=0 && (value&0xffff)==key && (((value&(int)Keys.Control)!=0)==ctrl)
            && (((value&(int)Keys.Alt)!=0)==alt) && (((value&(int)Keys.Shift)!=0)==shift);
    }
    public static bool Down(int value) {
        return Matches(value,value&0xffff,(GetAsyncKeyState(0x11)&0x8000)!=0,(GetAsyncKeyState(0x12)&0x8000)!=0,(GetAsyncKeyState(0x10)&0x8000)!=0)
            && (GetAsyncKeyState(value&0xffff)&0x8000)!=0;
    }
    public static bool Pressed(int value,ref bool previous) {bool down=Down(value);bool edge=down&&!previous;previous=down;return edge;}
    public static bool Distinct(int[] bindings) {for(int i=0;i<bindings.Length;i++)for(int j=i+1;j<bindings.Length;j++)if(bindings[i]!=0&&bindings[i]==bindings[j])return false;return true;}
}
public sealed class KeyCapture : TextBox {
    int binding; public int Binding {get{return binding;}set{binding=value;Text=Hotkeys.Label(value);}}
    public KeyCapture() {ReadOnly=true;BackColor=Color.White;ShortcutsEnabled=false;}
    protected override bool IsInputKey(Keys keyData) {return (keyData&Keys.KeyCode)!=Keys.Tab || base.IsInputKey(keyData);}
    bool CaptureBinding(Keys data) {
        Keys key=data&Keys.KeyCode;
        if(key==Keys.Escape||key==Keys.Tab)return false;
        if(key==Keys.ControlKey||key==Keys.ShiftKey||key==Keys.Menu||key==Keys.LWin||key==Keys.RWin)return true;
        Binding=key==Keys.Delete||key==Keys.Back?0:(int)data;return true;
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {return CaptureBinding(keyData)||base.ProcessCmdKey(ref msg,keyData);}
    protected override void OnKeyDown(KeyEventArgs e) {if(CaptureBinding(e.KeyData)){e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
}
public sealed class KeybindWindow : Form {
    public static readonly uint[] ButtonMasks={0,0x20,0x20000,0x40000,0x80,0x100};
    static readonly object[] Names={"None","R3 / right stick click","L4","L5","R4","R5"};
    readonly KeyCapture center=new KeyCapture(),pause=new KeyCapture(),calibrate=new KeyCapture();
    readonly ComboBox centerPad=new ComboBox(),pausePad=new ComboBox(),calibratePad=new ComboBox();
    readonly Settings settings; readonly int holdIndex;
    public KeybindWindow(Settings source,int holdPauseIndex) {
        settings=source;holdIndex=holdPauseIndex;Text="Keybinds";ClientSize=new Size(590,365);FormBorderStyle=FormBorderStyle.FixedDialog;
        MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        LabelAt("Click a keyboard box, then press a key or Ctrl / Alt / Shift combination.",18,15,555,26);
        LabelAt("Delete clears a binding. Escape cancels. Tab moves between fields.",18,42,555,26);
        LabelAt("Action",18,85,150,25);LabelAt("Keyboard shortcut",172,85,180,25);LabelAt("Controller button",370,85,200,25);
        Row("Center view",116,center,centerPad,source.CenterKey,source.CenterPad);
        Row("Toggle pause",163,pause,pausePad,source.PauseKey,source.TogglePausePad);
        Row("Recalibrate",210,calibrate,calibratePad,source.CalibrateKey,source.CalibratePad);
        LabelAt("Hold-to-pause stays on the main window. Buttons also keep their normal\ngame/Steam actions; use unused buttons to avoid conflicts.",18,253,555,46);
        var defaults=new Button {Text="Defaults"};defaults.SetBounds(18,319,110,30);defaults.Click+=(s,e)=>{center.Binding=(int)Keys.F8;pause.Binding=(int)Keys.F9;calibrate.Binding=(int)Keys.F10;centerPad.SelectedIndex=pausePad.SelectedIndex=calibratePad.SelectedIndex=0;};Controls.Add(defaults);
        var save=new Button {Text="Save"};save.SetBounds(341,319,110,30);save.Click+=(s,e)=>Save();Controls.Add(save);
        var cancel=new Button {Text="Cancel",DialogResult=DialogResult.Cancel};cancel.SetBounds(462,319,110,30);Controls.Add(cancel);CancelButton=cancel;
    }
    void LabelAt(string text,int x,int y,int w,int h) {var label=new Label {Text=text};label.SetBounds(x,y,w,h);Controls.Add(label);}
    void Row(string name,int y,KeyCapture key,ComboBox pad,int keyboard,int gamepad) {
        LabelAt(name,18,y+3,150,27);key.SetBounds(172,y,180,29);key.Binding=keyboard;Controls.Add(key);
        pad.SetBounds(370,y,202,29);pad.DropDownStyle=ComboBoxStyle.DropDownList;pad.Items.AddRange(Names);pad.SelectedIndex=Math.Max(0,Math.Min(5,gamepad));Controls.Add(pad);
    }
    void Save() {
        if(!Hotkeys.Distinct(new int[]{center.Binding,pause.Binding,calibrate.Binding})) {MessageBox.Show(this,"Choose a different shortcut for each action, or clear it.","Duplicate keyboard binding");return;}
        if(!Hotkeys.Distinct(new int[]{centerPad.SelectedIndex,pausePad.SelectedIndex,calibratePad.SelectedIndex,holdIndex})) {MessageBox.Show(this,"A controller button cannot have multiple bridge actions, including hold-to-pause.","Duplicate controller binding");return;}
        settings.CenterKey=center.Binding;settings.PauseKey=pause.Binding;settings.CalibrateKey=calibrate.Binding;
        settings.CenterPad=centerPad.SelectedIndex;settings.TogglePausePad=pausePad.SelectedIndex;settings.CalibratePad=calibratePad.SelectedIndex;
        DialogResult=DialogResult.OK;Close();
    }
}
}
