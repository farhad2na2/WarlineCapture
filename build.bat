if not exist "D:\Projects\WarlineCapture\TestResults" mkdir "D:\Projects\WarlineCapture\TestResults"

"D:\Program Files\Unity\6000.4.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Projects\WarlineCapture" -runTests -testPlatform EditMode -testResults "D:\Projects\WarlineCapture\TestResults\EditMode.xml" -logFile "D:\Projects\WarlineCapture\TestResults\EditMode.log"
set EDITMODE_EXIT=%ERRORLEVEL%
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Projects\WarlineCapture\Tools\CI\PrintUnityTestFailures.ps1" -ResultsPath "D:\Projects\WarlineCapture\TestResults\EditMode.xml" -PlatformName "EditMode"

"D:\Program Files\Unity\6000.4.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Projects\WarlineCapture" -runTests -testPlatform PlayMode -testResults "D:\Projects\WarlineCapture\TestResults\PlayMode.xml" -logFile "D:\Projects\WarlineCapture\TestResults\PlayMode.log"
set PLAYMODE_EXIT=%ERRORLEVEL%
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Projects\WarlineCapture\Tools\CI\PrintUnityTestFailures.ps1" -ResultsPath "D:\Projects\WarlineCapture\TestResults\PlayMode.xml" -PlatformName "PlayMode"

if not "%EDITMODE_EXIT%"=="0" (
    echo [BuildGate] EditMode tests failed. Build stopped.
    exit /b %EDITMODE_EXIT%
)
if not "%PLAYMODE_EXIT%"=="0" (
    echo [BuildGate] PlayMode tests failed. Build stopped.
    exit /b %PLAYMODE_EXIT%
)

echo [BuildGate] All EditMode and PlayMode tests passed. Continuing build.

"D:\Program Files\Unity\6000.4.0f1\Editor\Unity.exe" -executeMethod BuildScript.BuildWindows -batchmode -quit -projectPath "D:\Projects\WarlineCapture" -logFile D:\Projects\WarlineCapture\build.log
