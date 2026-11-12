using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SteamGyroOpenTrack {
public static class Hotkeys {
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    public static string ConfigName(int value) {return value==0?"None":new KeysConverter().ConvertToInvariantString((Keys)value);}
    public static string Label(int value) {
        string symbol;
        switch((Keys)(value&0xffff)) {
            case Keys.OemMinus:symbol="-";break;
            case Keys.Oemplus:symbol="=";break;
            case Keys.Oemcomma:symbol=",";break;
            case Keys.OemPeriod:symbol=".";break;
            case Keys.OemQuestion:symbol="/";break;
            case Keys.OemSemicolon:symbol=";";break;
            case Keys.OemQuotes:symbol="'";break;
            case Keys.OemOpenBrackets:symbol="[";break;
            case Keys.OemCloseBrackets:symbol="]";break;
            case Keys.OemPipe:case Keys.OemBackslash:symbol="\\";break;
            case Keys.Oemtilde:symbol="`";break;
            default:return ConfigName(value);
        }
        return ((value&(int)Keys.Control)!=0?"Ctrl+":"")+
            ((value&(int)Keys.Alt)!=0?"Alt+":"")+
            ((value&(int)Keys.Shift)!=0?"Shift+":"")+symbol;
    }
    public static bool TryParse(string text,out int value) {
        value=0;
        if(String.IsNullOrWhiteSpace(text)||String.Equals(text.Trim(),"None",StringComparison.OrdinalIgnoreCase))return true;
        int parsed;
        if(!Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsed)) {
            try {parsed=(int)(Keys)new KeysConverter().ConvertFromInvariantString(text.Trim());}
            catch {return false;}
        }
        if(parsed==0)return true;
        int key=parsed&0xffff;
        if((parsed&~0x700ff)!=0||key==0||key==0x10||key==0x11||key==0x12||key==0x5b||key==0x5c)return false;
        value=parsed;return true;
    }
    public static bool Matches(int value,int key,bool ctrl,bool alt,bool shift) {
        return value!=0 && (value&0xffff)==key && (((value&(int)Keys.Control)!=0)==ctrl)
            && (((value&(int)Keys.Alt)!=0)==alt) && (((value&(int)Keys.Shift)!=0)==shift);
    }
    public static bool Down(int value) {
        // Modifier states are needed only while a configured key is held.
        if(value==0||(GetAsyncKeyState(value&0xffff)&0x8000)==0)return false;
        return Matches(value,value&0xffff,(GetAsyncKeyState(0x11)&0x8000)!=0,(GetAsyncKeyState(0x12)&0x8000)!=0,(GetAsyncKeyState(0x10)&0x8000)!=0);
    }
    public static bool Pressed(int value,ref bool previous) {bool down=Down(value);bool edge=down&&!previous;previous=down;return edge;}
}
}
