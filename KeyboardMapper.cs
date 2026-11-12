using System;
using System.Drawing;
using System.Windows.Forms;

namespace SteamGyroOpenTrack {
public enum CaptureResult { Ignored, Accepted, Cancelled }

public static class KeyboardCapture {
    public static CaptureResult Interpret(Keys data,out int binding) {
        binding=0;
        Keys key=data&Keys.KeyCode;
        if(key==Keys.Escape)return CaptureResult.Cancelled;
        if(key==Keys.ControlKey||key==Keys.LControlKey||key==Keys.RControlKey||
           key==Keys.ShiftKey||key==Keys.LShiftKey||key==Keys.RShiftKey||
           key==Keys.Menu||key==Keys.LMenu||key==Keys.RMenu||key==Keys.LWin||key==Keys.RWin)
            return CaptureResult.Ignored;
        int parsed;
        if(!Hotkeys.TryParse(((int)data).ToString(System.Globalization.CultureInfo.InvariantCulture),out parsed)||parsed==0)
            return CaptureResult.Ignored;
        binding=parsed;
        return CaptureResult.Accepted;
    }
    public static bool Distinct(int[] bindings) {
        for(int i=0;i<bindings.Length;i++)for(int j=i+1;j<bindings.Length;j++)
            if(bindings[i]!=0&&bindings[i]==bindings[j])return false;
        return true;
    }
}

public sealed class CaptureShortcutDialog : Form {
    public int CapturedKey {get;private set;}
    public CaptureShortcutDialog(string action) {
        Text="Bind "+action;
        ClientSize=new Size(495,155);
        FormBorderStyle=FormBorderStyle.FixedDialog;
        MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;
        StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10);
        KeyPreview=true;
        var prompt=new Label {Text="Press the keybind or Escape to cancel",Font=new Font("Segoe UI",13,FontStyle.Bold)};
        prompt.SetBounds(20,20,455,32);Controls.Add(prompt);
        var help=new Label {Text="Ctrl, Alt and Shift combinations are supported."};
        help.SetBounds(20,61,455,25);Controls.Add(help);
        var cancel=new Button {Text="Cancel",DialogResult=DialogResult.Cancel};
        cancel.SetBounds(360,105,115,32);Controls.Add(cancel);CancelButton=cancel;
    }
    // Only keyboard messages delivered to this focused window are captured.
    // No keyboard hook or reflection is used by the mapper.
    internal bool HandleShortcut(Keys data) {
        int binding;
        CaptureResult result=KeyboardCapture.Interpret(data,out binding);
        if(result==CaptureResult.Accepted){CapturedKey=binding;DialogResult=DialogResult.OK;Close();}
        else if(result==CaptureResult.Cancelled){DialogResult=DialogResult.Cancel;Close();}
        return true;
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {return HandleShortcut(keyData);}
    protected override void OnKeyDown(KeyEventArgs e) {HandleShortcut(e.KeyData);e.Handled=true;e.SuppressKeyPress=true;}
}

public sealed class KeyboardMapperDialog : Form {
    readonly int[] bindings;
    readonly Button[] keyButtons=new Button[4];
    readonly string[] actions={"Center view","Toggle pause","Recalibrate","Hold to pause"};
    public int[] Bindings {get{return (int[])bindings.Clone();}}
    public KeyboardMapperDialog(int[] current) {
        if(current==null||current.Length!=4)throw new ArgumentException("Four shortcut bindings required.");
        bindings=(int[])current.Clone();
        Text="Keyboard shortcuts";ClientSize=new Size(540,320);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;
        StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        var intro=new Label {Text="Click a shortcut to choose a key. Clear disables that action."};
        intro.SetBounds(20,18,500,28);Controls.Add(intro);
        for(int i=0;i<4;i++) {
            int index=i;int y=62+i*49;
            var label=new Label {Text=actions[i]};label.SetBounds(20,y+7,170,28);Controls.Add(label);
            var key=new Button {Text=Hotkeys.Label(bindings[i])};key.SetBounds(195,y,215,34);
            key.Click+=(s,e)=>{
                using(var capture=new CaptureShortcutDialog(actions[index])) {
                    if(capture.ShowDialog(this)==DialogResult.OK){bindings[index]=capture.CapturedKey;RefreshLabels();}
                }
            };
            keyButtons[i]=key;Controls.Add(key);
            var clear=new Button {Text="Clear"};clear.SetBounds(425,y,95,34);
            clear.Click+=(s,e)=>{bindings[index]=0;RefreshLabels();};Controls.Add(clear);
        }
        var defaults=new Button {Text="Defaults"};defaults.SetBounds(20,268,115,32);
        defaults.Click+=(s,e)=>{bindings[0]=(int)Keys.F8;bindings[1]=(int)Keys.F9;bindings[2]=(int)Keys.F10;bindings[3]=0;RefreshLabels();};Controls.Add(defaults);
        var save=new Button {Text="Save"};save.SetBounds(285,268,110,32);
        save.Click+=(s,e)=>{
            if(!KeyboardCapture.Distinct(bindings)){MessageBox.Show(this,"Choose different shortcuts for each action, or clear unused ones.","Duplicate shortcut");return;}
            DialogResult=DialogResult.OK;Close();
        };Controls.Add(save);
        var cancel=new Button {Text="Cancel",DialogResult=DialogResult.Cancel};cancel.SetBounds(410,268,110,32);Controls.Add(cancel);CancelButton=cancel;
    }
    void RefreshLabels(){for(int i=0;i<4;i++)keyButtons[i].Text=Hotkeys.Label(bindings[i]);}
}
}
