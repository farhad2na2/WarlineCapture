$env:SystemRoot='D:\Projects\WarlineCapture\Logs\SkirmishProduction\Offline\InvokeUnityCleanupStderr\owned-q65s8ej5\fake-system-root'
$env:WARLINE_TEST_TASKKILL_MODE='killthenstderr'
& 'D:\Projects\WarlineCapture\Tools\CI\InvokeUnity.ps1' -UnityExe 'D:\Projects\WarlineCapture\Logs\SkirmishProduction\Offline\InvokeUnityCleanupStderr\owned-q65s8ej5\OwnedExitProbe.exe' -ProjectPath 'D:\Projects\WarlineCapture\Logs\SkirmishProduction\Offline\InvokeUnityCleanupStderr\owned-q65s8ej5' -LogFile 'D:\Projects\WarlineCapture\Logs\SkirmishProduction\Offline\InvokeUnityCleanupStderr\owned-q65s8ej5\fatal-child-ended-stderr.log' -GuiLicensing -TimeoutSeconds 1 -UnityArguments @('--sleep','20000','--fatal','1')
exit $global:LASTEXITCODE
