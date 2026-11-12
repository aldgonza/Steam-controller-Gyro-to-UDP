using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SteamGyroOpenTrack {
public sealed class HidDevice {
    public string Path; public ushort Pid, UsagePage, Usage; public int InputLength, FeatureLength;
    public override string ToString() { return String.Format("Valve {0:X4} usage {1:X4}:{2:X4} input={3} feature={4}", Pid, UsagePage, Usage, InputLength, FeatureLength); }
}
public static class NativeHid {
    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid ClassGuid; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort Vendor, Product, Version; }
    [StructLayout(LayoutKind.Sequential)] struct Caps {
        public ushort Usage, UsagePage, InputLength, OutputLength, FeatureLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=17)] public ushort[] Reserved;
        public ushort Links, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
    }
    [StructLayout(LayoutKind.Sequential)] struct Overlap { public IntPtr Internal, InternalHigh; public uint Offset, OffsetHigh; public IntPtr Event; }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle h, ref Attributes a);
    [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
    [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, out Caps caps);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_SetFeature(SafeFileHandle h, byte[] b, int size);
    [DllImport("hid.dll")] static extern bool HidD_SetNumInputBuffers(SafeFileHandle h, int count);
    [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, string e, IntPtr parent, uint flags);
    [DllImport("setupapi.dll",SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid g, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint required, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadFile(SafeFileHandle h, IntPtr b, int count, out int read, ref Overlap overlap);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetOverlappedResult(SafeFileHandle h, ref Overlap overlap, out int read, bool wait);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool CancelIoEx(SafeFileHandle h, ref Overlap overlap);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateEvent(IntPtr attr, bool manual, bool initial, string name);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint timeout);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    public static List<HidDevice> Enumerate() {
        Guid guid; HidD_GetHidGuid(out guid); var devices=new List<HidDevice>();
        IntPtr set=SetupDiGetClassDevs(ref guid,null,IntPtr.Zero,0x12);
        if(set==new IntPtr(-1)) throw new Win32Exception();
        try { for(uint i=0;;i++) {
            var d=new InterfaceData(); d.Size=Marshal.SizeOf(typeof(InterfaceData));
            if(!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref d)) { if(Marshal.GetLastWin32Error()==259) break; throw new Win32Exception(); }
            uint needed; SetupDiGetDeviceInterfaceDetail(set,ref d,IntPtr.Zero,0,out needed,IntPtr.Zero);
            IntPtr detail=Marshal.AllocHGlobal((int)needed);
            try {
                Marshal.WriteInt32(detail,IntPtr.Size==8?8:6);
                if(!SetupDiGetDeviceInterfaceDetail(set,ref d,detail,needed,out needed,IntPtr.Zero)) continue;
                string path=Marshal.PtrToStringUni(IntPtr.Add(detail,4));
                if(path==null || path.IndexOf("vid_28de",StringComparison.OrdinalIgnoreCase)<0) continue;
                using(var h=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
                    if(h.IsInvalid) continue;
                    var a=new Attributes(); a.Size=Marshal.SizeOf(typeof(Attributes));
                    if(!HidD_GetAttributes(h,ref a) || a.Vendor!=0x28de) continue;
                    IntPtr pp; if(!HidD_GetPreparsedData(h,out pp)) continue;
                    try { Caps caps; if(HidP_GetCaps(pp,out caps)!=0x110000) continue;
                        devices.Add(new HidDevice { Path=path,Pid=a.Product,Usage=caps.Usage,UsagePage=caps.UsagePage,InputLength=caps.InputLength,FeatureLength=caps.FeatureLength });
                    } finally { HidD_FreePreparsedData(pp); }
                }
            } finally { Marshal.FreeHGlobal(detail); }
        }} finally { SetupDiDestroyDeviceInfoList(set); }
        return devices;
    }
    public static bool Supported(HidDevice d) { return d.Pid>=0x1302 && d.Pid<=0x1305 && d.UsagePage>=0xff00 && d.InputLength>=46 && d.FeatureLength>=6; }
    public static SafeFileHandle Open(HidDevice d) {
        var h=CreateFile(d.Path,0xc0000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero);
        if(h.IsInvalid) { h.Dispose(); throw new Win32Exception(); }
        HidD_SetNumInputBuffers(h,64); return h;
    }
    // Transient IMU setting only. Never changes lizard mode, mappings, haptics or firmware.
    public static bool EnableMotion(SafeFileHandle h, int length) {
        var b=new byte[length]; b[0]=1; b[1]=0x87; b[2]=3; b[3]=48; b[4]=0x18;
        return HidD_SetFeature(h,b,b.Length);
    }
    public static int Read(SafeFileHandle h, byte[] b, int timeoutMs) {
        var o=new Overlap(); o.Event=CreateEvent(IntPtr.Zero,true,false,null);
        if(o.Event==IntPtr.Zero) throw new Win32Exception();
        var pinned=GCHandle.Alloc(b,GCHandleType.Pinned);
        try { int n; bool ok=ReadFile(h,pinned.AddrOfPinnedObject(),b.Length,out n,ref o);
            if(ok) return n;
            if(Marshal.GetLastWin32Error()!=997) throw new Win32Exception();
            uint result=WaitForSingleObject(o.Event,(uint)timeoutMs);
            if(result==258) { CancelIoEx(h,ref o); GetOverlappedResult(h,ref o,out n,true); return 0; }
            if(result!=0) throw new Win32Exception();
            if(!GetOverlappedResult(h,ref o,out n,false)) throw new Win32Exception();
            return n;
        } finally { pinned.Free(); CloseHandle(o.Event); }
    }
}
}
