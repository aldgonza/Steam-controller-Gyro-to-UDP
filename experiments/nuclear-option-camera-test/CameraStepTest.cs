using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("aldgonza.nuclearoption.camera-step-test", "Nuclear Option Camera Step Test", "0.1.0")]
public sealed class CameraStepTest : BaseUnityPlugin {
    internal static CameraStepTest Instance;
    Harmony harmony;
    ConfigEntry<bool> bypassSmoothing;
    FieldInfo pan,tilt,padlock,flightControls,useTrackIR,smoothing;
    MethodInfo radialInUse;
    object baselineState;
    float baselinePan,baselineTilt,deadline;
    int pending;
    bool active,safeThisFrame;
    string status="Starting camera hook...";
    string result="";
    bool failed;

    void Awake() {
        Instance=this;
        bypassSmoothing=Config.Bind("Test","BypassViewSmoothing",true,"Bypass only cockpit view-angle smoothing while the fixed-angle test is active. No global game setting is changed.");
        try {
            Type camera=AccessTools.TypeByName("CameraCockpitState");
            Type settings=AccessTools.TypeByName("PlayerSettings");
            Type game=AccessTools.TypeByName("GameManager");
            pan=RequiredField(camera,"panView");tilt=RequiredField(camera,"tiltView");padlock=RequiredField(camera,"padLock");
            flightControls=RequiredField(game,"flightControlsEnabled");useTrackIR=RequiredField(settings,"useTrackIR");smoothing=RequiredField(settings,"viewSmoothing");
            radialInUse=AccessTools.Method(AccessTools.TypeByName("RadialMenuMain"),"IsInUse");
            if(radialInUse==null)throw new MissingMethodException("RadialMenuMain.IsInUse");
            MethodInfo update=AccessTools.Method(camera,"UpdateState");
            if(update==null)throw new MissingMethodException("CameraCockpitState.UpdateState");
            harmony=new Harmony("aldgonza.nuclearoption.camera-step-test");
            harmony.Patch(update,prefix:new HarmonyMethod(typeof(CameraStepTest),"BeforeUpdate"),postfix:new HarmonyMethod(typeof(CameraStepTest),"AfterUpdate"),transpiler:new HarmonyMethod(typeof(CameraStepTest),"Transpile"));
            foreach(string method in new[]{"EnterState","LeaveState"}) {
                var target=AccessTools.Method(camera,method);
                if(target==null)throw new MissingMethodException(method);
                harmony.Patch(target,postfix:new HarmonyMethod(typeof(CameraStepTest),"ResetCameraTest"));
            }
            status="READY - enter a cockpit. F6: +10 degrees; F7: undo test.";
            Logger.LogInfo("Camera hook installed. No gyro input or OpenTrack output. F6 adds 10 degrees to cockpit pan; F7 restores the saved pan/tilt. Smoothing bypass="+bypassSmoothing.Value);
        } catch(Exception ex) {
            failed=true;
            if(harmony!=null)harmony.UnpatchSelf();
            status="HOOK FAILED - see BepInEx/LogOutput.log";
            Logger.LogError(ex);
        }
    }
    static FieldInfo RequiredField(Type owner,string name) {
        if(owner==null)throw new TypeLoadException("Missing owner for "+name);
        var field=AccessTools.Field(owner,name);
        if(field==null)throw new MissingFieldException(owner.FullName,name);
        return field;
    }
    void Update() {
        if(failed||!Application.isFocused)return;
        if(Input.GetKeyDown(KeyCode.F6)){pending=1;deadline=Time.realtimeSinceStartup+2;status="Waiting for a usable cockpit update...";}
        if(Input.GetKeyDown(KeyCode.F7)){pending=-1;deadline=Time.realtimeSinceStartup+2;status="Waiting to restore the test baseline...";}
        if(pending!=0&&Time.realtimeSinceStartup>deadline){pending=0;status="No usable cockpit update. Enter a cockpit with menus closed.";Logger.LogWarning(status);}
    }
    static void BeforeUpdate(){if(Instance!=null)Instance.safeThisFrame=false;}
    static void AfterUpdate(object __instance) {
        var self=Instance;
        if(self==null||self.result.Length==0)return;
        self.Logger.LogInfo(self.result+"; final pan="+self.pan.GetValue(__instance)+" tilt="+self.tilt.GetValue(__instance));
        self.result="";
    }
    static void ResetCameraTest() {
        if(Instance==null)return;
        Instance.active=false;Instance.safeThisFrame=false;Instance.baselineState=null;Instance.pending=0;
        Instance.status="READY - F6: +10 degrees; F7: undo test.";
    }
    static void ApplyStep(object state) {
        var self=Instance;if(self==null||self.failed)return;
        bool usable=Application.isFocused&&(bool)self.flightControls.GetValue(null)&&!Cursor.visible&&!(bool)self.radialInUse.Invoke(null,null)&&!(bool)self.useTrackIR.GetValue(null)&&!(bool)self.padlock.GetValue(state);
        self.safeThisFrame=usable;
        if(!usable) {
            if(self.pending!=0){self.pending=0;self.status="Test blocked: close menus, disable TrackIR and release target padlock.";self.Logger.LogWarning(self.status);}
            return;
        }
        if(self.pending==0)return;
        int request=self.pending;self.pending=0;
        if(request<0) {
            if(self.active&&System.Object.ReferenceEquals(self.baselineState,state)) {
                self.pan.SetValue(state,self.baselinePan);self.tilt.SetValue(state,self.baselineTilt);
                self.active=false;self.status="RESTORED - original look angles and normal smoothing.";self.result="UNDO: restored saved cockpit pan/tilt";
            } else self.status="No active test to undo.";
            return;
        }
        float before=(float)self.pan.GetValue(state);
        if(before>155f){self.status="Near right look limit. Turn toward center, then press F6.";return;}
        if(!self.active||!System.Object.ReferenceEquals(self.baselineState,state)) {
            self.baselineState=state;self.baselinePan=before;self.baselineTilt=(float)self.tilt.GetValue(state);
        }
        self.pan.SetValue(state,before+10f);self.active=true;
        self.status="APPLIED +10 degrees. F7: restore starting view. Smoothing bypass: "+self.bypassSmoothing.Value;
        self.result="STEP: pan "+before+" -> "+(before+10f)+"; smoothing bypass="+self.bypassSmoothing.Value;
    }
    static float Smoothing(float original) {
        var self=Instance;
        return self!=null&&self.active&&self.safeThisFrame&&self.bypassSmoothing.Value?0f:original;
    }
    static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> source) {
        var code=source.Select(c=>new CodeInstruction(c)).ToList();
        var self=Instance;
        var anchors=new List<int>();
        for(int i=0;i+7<code.Count;i++) {
            if(code[i].opcode==OpCodes.Ldarg_0&&code[i+1].opcode==OpCodes.Ldarg_0&&code[i+2].LoadsField(self.pan)&&
               code[i+3].opcode==OpCodes.Ldc_R4&&(float)code[i+3].operand==-165f&&
               code[i+4].opcode==OpCodes.Ldc_R4&&(float)code[i+4].operand==165f&&
               code[i+5].opcode==OpCodes.Call&&((MethodInfo)code[i+5].operand).Name=="Clamp"&&code[i+6].StoresField(self.pan))anchors.Add(i);
        }
        int reads=code.Count(c=>c.LoadsField(self.smoothing));
        if(anchors.Count!=2||reads!=2)throw new InvalidOperationException("Camera method layout changed; refusing patch. Pan clamp anchors="+anchors.Count+", smoothing reads="+reads);
        int anchor=anchors[1];
        for(int i=0;i<code.Count;i++) {
            if(i==anchor) {
                var entry=new CodeInstruction(OpCodes.Ldarg_0);
                entry.labels.AddRange(code[i].labels);code[i].labels.Clear();
                entry.blocks.AddRange(code[i].blocks);code[i].blocks.Clear();
                yield return entry;yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CameraStepTest),"ApplyStep"));
            }
            yield return code[i];
            if(code[i].LoadsField(self.smoothing))yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CameraStepTest),"Smoothing"));
        }
    }
    void OnGUI() {
        GUI.Box(new Rect(16,16,700,76),"Nuclear Option camera hook test - no gyro");
        GUI.Label(new Rect(28,40,676,42),status);
    }
    void OnDestroy(){if(harmony!=null)harmony.UnpatchSelf();if(Instance==this)Instance=null;}
}
