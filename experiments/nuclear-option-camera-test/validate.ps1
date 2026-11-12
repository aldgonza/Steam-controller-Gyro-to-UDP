param([string]$GamePath='F:\Games\Steam\steamapps\common\Nuclear Option')
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core\Mono.Cecil.dll')
$assemblyPath=Join-Path $GamePath 'NuclearOption_Data\Managed\Assembly-CSharp.dll'
$module=[Mono.Cecil.ModuleDefinition]::ReadModule($assemblyPath)
try {
 $camera=$module.Types | Where-Object Name -eq 'CameraCockpitState'
 if(-not $camera){throw 'Cockpit camera class missing.'}
 foreach($field in @('panView','tiltView','padLock')){if(-not($camera.Fields | Where-Object Name -eq $field)){throw "Camera field missing: $field"}}
 $method=$camera.Methods | Where-Object Name -eq 'UpdateState'
 if($method.Parameters.Count -ne 1 -or $method.Parameters[0].ParameterType.Name -ne 'CameraStateManager'){throw 'Unexpected camera method signature.'}
 if($method.Body.ExceptionHandlers.Count -ne 0){throw 'Unexpected exception regions need review.'}
 $instructions=$method.Body.Instructions
 $anchors=@()
 for($i=0;$i+7 -lt $instructions.Count;$i++) {
  $op=$instructions[$i..($i+6)]
  if($op[0].OpCode.Name -eq 'ldarg.0' -and $op[1].OpCode.Name -eq 'ldarg.0' -and
     $op[2].OpCode.Name -eq 'ldfld' -and $op[2].Operand.Name -eq 'panView' -and
     $op[3].OpCode.Name -eq 'ldc.r4' -and $op[3].Operand -eq -165 -and
     $op[4].OpCode.Name -eq 'ldc.r4' -and $op[4].Operand -eq 165 -and
     $op[5].OpCode.Name -eq 'call' -and $op[5].Operand.Name -eq 'Clamp' -and
     $op[6].OpCode.Name -eq 'stfld' -and $op[6].Operand.Name -eq 'panView'){$anchors+=,$instructions[$i]}
 }
 if($anchors.Count -ne 2){throw "Expected 2 pan clamps, found $($anchors.Count)"}
 $smoothing=@($instructions | Where-Object {$_.OpCode.Name -eq 'ldsfld' -and $_.Operand.FullName -eq 'System.Single PlayerSettings::viewSmoothing'})
 if($smoothing.Count -ne 2){throw 'Expected exactly two cockpit view-smoothing reads.'}
 if($anchors[1].Offset -ge $smoothing[0].Offset){throw 'Test injection must precede view smoothing.'}
 foreach($expect in @('System.Boolean GameManager::flightControlsEnabled','System.Boolean PlayerSettings::useTrackIR')) {
  if(-not($instructions | Where-Object {$_.Operand -and $_.Operand.ToString() -eq $expect})){throw "Missing context gate: $expect"}
 }
 $manager=$module.Types | Where-Object Name -eq 'CameraStateManager'
 $lateUpdate=$manager.Methods | Where-Object Name -eq 'LateUpdate'
 if(-not($lateUpdate.Body.Instructions | Where-Object {$_.OpCode.Name -eq 'callvirt' -and $_.Operand.Name -eq 'UpdateState'})){throw 'LateUpdate no longer calls the camera state update.'}
 $plugin=[Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $PSScriptRoot 'build\NuclearOption.CameraStepTest.dll'))
 try {
  $pluginType=$plugin.Types | Where-Object Name -eq 'CameraStepTest'
  foreach($name in @('Awake','ApplyStep','Smoothing','Transpile','BeforeUpdate','AfterUpdate','ResetCameraTest')) {
   if(-not($pluginType.Methods | Where-Object Name -eq $name)){throw "Plugin method missing: $name"}
  }
  if($plugin.AssemblyReferences.Name -contains 'Assembly-CSharp'){throw 'Plugin unexpectedly links proprietary game code.'}
 } finally {$plugin.Dispose()}
 Write-Output ('PASS: installed camera signature and fields; two pan clamp anchors; test injection at IL_'+$anchors[1].Offset.ToString('x4')+' before both smoothing reads; menu/TrackIR gates; LateUpdate caller; compiled plugin entry points. Live hook behavior remains untested.')
 Write-Output ('Game assembly SHA256: '+(Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash)
} finally {$module.Dispose()}
