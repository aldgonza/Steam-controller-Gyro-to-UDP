using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace SteamGyroOpenTrack {
public sealed class Sample {
    public uint Tick, Buttons; public int TickBits; public double TickScale;
    public double X,Y,Z, Ax,Ay,Az;
    public static Sample Parse(byte[] b, int n) {
        if(n<46) return null;
        int timestampOffset, accelOffset; uint tick; int bits; double scale;
        // SDL's packed TritonMTUNoQuat and TritonMTUNoQuat32TS wire structures.
        if(b[0]==0x42 || b[0]==0x45) { timestampOffset=30; accelOffset=34; tick=BitConverter.ToUInt32(b,timestampOffset); bits=32; scale=0.000001; }
        else if(b[0]==0x47) { timestampOffset=32; accelOffset=34; tick=BitConverter.ToUInt16(b,timestampOffset); bits=16; scale=0.000032; }
        else return null;
        var s=new Sample(); s.Tick=tick; s.TickBits=bits; s.TickScale=scale; s.Buttons=BitConverter.ToUInt32(b,2);
        s.Ax=BitConverter.ToInt16(b,accelOffset)/16384.0;
        s.Ay=BitConverter.ToInt16(b,accelOffset+4)/16384.0;
        s.Az=-BitConverter.ToInt16(b,accelOffset+2)/16384.0;
        s.X=BitConverter.ToInt16(b,accelOffset+6)*(2000.0/32768);
        s.Y=BitConverter.ToInt16(b,accelOffset+10)*(2000.0/32768);
        s.Z=-BitConverter.ToInt16(b,accelOffset+8)*(2000.0/32768);
        return s;
    }
}
public struct Quaternion {
    public double W,X,Y,Z;
    public static Quaternion Identity { get { return new Quaternion {W=1}; } }
    public static Quaternion operator *(Quaternion a, Quaternion b) { return new Quaternion {
        W=a.W*b.W-a.X*b.X-a.Y*b.Y-a.Z*b.Z,
        X=a.W*b.X+a.X*b.W+a.Y*b.Z-a.Z*b.Y,
        Y=a.W*b.Y-a.X*b.Z+a.Y*b.W+a.Z*b.X,
        Z=a.W*b.Z+a.X*b.Y-a.Y*b.X+a.Z*b.W }; }
    public Quaternion Normalized() { double n=Math.Sqrt(W*W+X*X+Y*Y+Z*Z); if(n<1e-12) return Identity; return new Quaternion {W=W/n,X=X/n,Y=Y/n,Z=Z/n}; }
    public static Quaternion Increment(double x,double y,double z,double dt) {
        double speed=Math.Sqrt(x*x+y*y+z*z), angle=speed*dt*Math.PI/180;
        if(speed<1e-10) return Identity;
        double k=Math.Sin(angle/2)/speed; return new Quaternion {W=Math.Cos(angle/2),X=x*k,Y=y*k,Z=z*k};
    }
    public double[] Angles() {
        double yaw=Math.Atan2(2*(W*Y+X*Z),1-2*(X*X+Y*Y));
        double pitch=Math.Asin(Math.Max(-1,Math.Min(1,2*(W*X-Y*Z))));
        double roll=Math.Atan2(2*(W*Z+X*Y),1-2*(X*X+Z*Z));
        return new double[] {yaw*180/Math.PI,pitch*180/Math.PI,roll*180/Math.PI};
    }
}
public sealed class MotionEngine {
    public readonly object Gate=new object();
    public bool Paused, HoldPaused, Calibrating=true, Ready;
    public string CalibrationStatus="Waiting for motion; then keep controller still for 2 seconds.";
    public double BiasX,BiasY,BiasZ; public long SampleCount; public uint Buttons;
    public double LastSampleSeconds; public double Rate;
    Quaternion orientation=Quaternion.Identity; bool haveTick; uint tick; int tickBits;
    List<Sample> calibration=new List<Sample>(); double calibrationTime;
    Stopwatch clock=Stopwatch.StartNew(); long rateCount; double rateTime;
    public void Recenter() { lock(Gate) { orientation=Quaternion.Identity; } }
    public void Calibrate() { lock(Gate) { Calibrating=true; Ready=false; calibration.Clear(); calibrationTime=0; orientation=Quaternion.Identity; CalibrationStatus="Keep controller still for 2 seconds."; } }
    public void Disconnected() { lock(Gate) { haveTick=false; Buttons=0;Rate=0; Ready=false; Calibrating=true; calibration.Clear(); calibrationTime=0; orientation=Quaternion.Identity; } }
    public void Update(Sample s) { lock(Gate) {
        Buttons=s.Buttons;
        if(haveTick && s.Tick==tick && s.TickBits==tickBits) return;
        double now=clock.Elapsed.TotalSeconds; LastSampleSeconds=now; SampleCount++; rateCount++;
        if(now-rateTime>=1) { Rate=rateCount/(now-rateTime); rateCount=0; rateTime=now; }
        double dt=0;
        if(haveTick && s.TickBits==tickBits) {
            uint delta=unchecked(s.Tick-tick); if(s.TickBits==16) delta &= 0xffff;
            dt=delta*s.TickScale;
        }
        tick=s.Tick; tickBits=s.TickBits; haveTick=true;
        if(dt<=0 || dt>0.1) { if(Calibrating) {calibration.Clear(); calibrationTime=0;} return; }
        if(Calibrating) {
            double accel=Math.Sqrt(s.Ax*s.Ax+s.Ay*s.Ay+s.Az*s.Az);
            if(accel<0.75 || accel>1.25 || Math.Max(Math.Abs(s.X),Math.Max(Math.Abs(s.Y),Math.Abs(s.Z)))>8) {
                calibration.Clear(); calibrationTime=0; CalibrationStatus="Movement detected. Put controller on a still surface."; return;
            }
            calibration.Add(s); calibrationTime+=dt;
            CalibrationStatus=String.Format("Calibrating: {0:0}% (keep still)",Math.Min(100,calibrationTime*50));
            if(calibrationTime<2 || calibration.Count<100) return;
            double x=0,y=0,z=0; foreach(var a in calibration) {x+=a.X;y+=a.Y;z+=a.Z;} x/=calibration.Count;y/=calibration.Count;z/=calibration.Count;
            double variance=0; foreach(var a in calibration) variance+=(a.X-x)*(a.X-x)+(a.Y-y)*(a.Y-y)+(a.Z-z)*(a.Z-z);
            if(variance/calibration.Count>1.5) {calibration.Clear();calibrationTime=0;CalibrationStatus="Too much movement. Keep still and retrying.";return;}
            BiasX=x;BiasY=y;BiasZ=z;Calibrating=false;Ready=true;calibration.Clear();orientation=Quaternion.Identity;
            CalibrationStatus="Calibrated. Use the configured shortcuts or on-screen buttons."; return;
        }
        if(Paused || HoldPaused) return;
        orientation=(orientation*Quaternion.Increment(s.X-BiasX,s.Y-BiasY,s.Z-BiasZ,dt)).Normalized();
    }}
    public double Age { get { return clock.Elapsed.TotalSeconds-LastSampleSeconds; } }
    public double[] Pose(double yawGain,double pitchGain,bool roll,bool invertYaw,bool invertPitch) { lock(Gate) {
        var a=orientation.Angles(); return new double[] {0,0,0,a[0]*yawGain*(invertYaw?-1:1),a[1]*pitchGain*(invertPitch?-1:1),roll?a[2]:0};
    }}
    public static byte[] Packet(double[] pose) {
        if(pose.Length!=6) throw new ArgumentException("Six pose values required.");
        var packet=new byte[48]; for(int i=0;i<6;i++) {
            if(Double.IsNaN(pose[i])||Double.IsInfinity(pose[i])) throw new ArgumentException("Finite pose values required.");
            var bytes=BitConverter.GetBytes(pose[i]); if(!BitConverter.IsLittleEndian) Array.Reverse(bytes); Buffer.BlockCopy(bytes,0,packet,i*8,8);
        } return packet;
    }
}
public sealed class HidWorker : IDisposable {
    readonly MotionEngine engine; Thread thread; volatile bool stopping;
    public volatile bool EnableSensor=true; public string Status="Not started"; public string Device="";
    public HidWorker(MotionEngine e) {engine=e;}
    public void Start() {thread=new Thread(Run);thread.IsBackground=true;thread.Start();}
    void Run() {
        while(!stopping) {
            bool connected=false;
            try {
                var candidates=NativeHid.Enumerate().FindAll(NativeHid.Supported);
                if(candidates.Count==0) Status="No Steam Controller (2026) HID device found. Connect USB or receiver.";
                foreach(var d in candidates) {
                    if(stopping) break;
                    try { using(var h=NativeHid.Open(d)) {
                        Status="Checking controller interface..."; Device=d.ToString();
                        var b=new byte[Math.Max(64,d.InputLength)]; var elapsed=Stopwatch.StartNew(); double lastFeature=-1,lastValid=-1;
                        bool found=false; uint firstTick=0; bool first=false; double lastAdvance=0;
                        while(!stopping) {
                            double now=elapsed.Elapsed.TotalSeconds;
                            if(EnableSensor && now-lastFeature>=0.25) {
                                bool enabled=NativeHid.EnableMotion(h,d.FeatureLength); lastFeature=now;
                                if(!enabled) Status="IMU enable failed; listening for Steam's motion reports.";
                            }
                            int n=NativeHid.Read(h,b,100); var s=n>0?Sample.Parse(b,n):null;
                            if(s!=null) {
                                if(!first) { first=true;firstTick=s.Tick; }
                                else if(s.Tick!=firstTick) { found=true;firstTick=s.Tick;lastAdvance=now; }
                                lastValid=now;
                                if(found) { connected=true; engine.Update(s); Status="Reading controller motion"; }
                            }
                            if(!found && now>1.2) break;
                            if(found && (now-lastValid>2 || now-lastAdvance>2)) {Status="Motion stopped. Reconnecting...";break;}
                        }
                    }} catch(Exception ex) {Status="HID: "+ex.Message;}
                    if(connected) {engine.Disconnected();break;}
                }
            } catch(Exception ex) {Status="HID: "+ex.Message;}
            for(int i=0;i<10&&!stopping;i++) Thread.Sleep(100);
        }
    }
    public void Dispose() {stopping=true;if(thread!=null) thread.Join(1800);}
}
}
