def PROJECT_NAME = 'WarlineCapture'
def UNITY_EDITOR_VERSION = '6000.5.2f1'
def CUSTOM_WORKSPACE = "D:\\Projects\\Jenkins\\${PROJECT_NAME}"

pipeline {
    agent {
        node {
            label ''
            customWorkspace "${CUSTOM_WORKSPACE}"
        }
    }

    options {
        skipDefaultCheckout(true)
        disableConcurrentBuilds(abortPrevious: true)
    }

    environment {
        PROJECT_PATH = "${CUSTOM_WORKSPACE}"
        UNITY_VERSION = "${UNITY_EDITOR_VERSION}"
        BUILD_LOG = "${CUSTOM_WORKSPACE}\\build.log"
        CODEX_TASK_DIR = "\\\\192.168.2.175\\farhad\\Projects\\Jenkins_Builds\\WarlineCapture\\CodexTasks"
        MAC_PASSWORD = credentials('MAC_PASSWORD')
    }

    stages {
        stage('Checkout Unity Project') {
            steps {
                deleteDir()
                withCredentials([usernamePassword(credentialsId: 'github-pat-2', usernameVariable: 'GIT_USER', passwordVariable: 'GIT_TOKEN')]) {
                    bat '''
                    @echo off
                    git --version
                    git clone --filter=blob:none --sparse --depth 1 --branch main "https://%GIT_USER%:%GIT_TOKEN%@github.com/farhad2na2/WarlineCapture.git" "%PROJECT_PATH%"
                    cd /d "%PROJECT_PATH%"
                    git remote set-url origin https://github.com/farhad2na2/WarlineCapture.git
                    git sparse-checkout set Assets Packages ProjectSettings Tools
                    git rev-parse HEAD
                    git status --short
                    '''
                }
                script {
                    env.GIT_COMMIT = bat(
                        returnStdout: true,
                        script: '@git -C "%PROJECT_PATH%" rev-parse HEAD'
                    ).trim()
                    echo "Checked out WarlineCapture commit ${env.GIT_COMMIT}"
                }
            }
        }

        stage('Resolve Unity Editor') {
            when {
                expression {
                    return params.BUILD_WINDOWS == true || params.BUILD_WINDOWS?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_WEBGL == true || params.BUILD_WEBGL?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_IOS == true || params.BUILD_IOS?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_ANDROID_APK == true || params.BUILD_ANDROID_APK?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_ANDROID_AAB == true || params.BUILD_ANDROID_AAB?.toString()?.equalsIgnoreCase('true')
                }
            }
            steps {
                script {
                    bat '''
                    @echo off
                    setlocal EnableExtensions
                    set "UNITY_RESOLVE_FILE=%PROJECT_PATH%\\unity-editor-path.txt"
                    set "UNITY_RESOLVE_LOG=%PROJECT_PATH%\\TestResults\\UnityEditorResolution.log"
                    if not exist "%PROJECT_PATH%\\TestResults" mkdir "%PROJECT_PATH%\\TestResults"
                    if exist "%UNITY_RESOLVE_FILE%" del /f /q "%UNITY_RESOLVE_FILE%"
                    if exist "%UNITY_RESOLVE_LOG%" del /f /q "%UNITY_RESOLVE_LOG%"

                    powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%PROJECT_PATH%\\Tools\\CI\\ResolveUnityEditor.ps1" -UnityVersion "%UNITY_VERSION%" -OutputPath "%UNITY_RESOLVE_FILE%" -LogFile "%UNITY_RESOLVE_LOG%"
                    if errorlevel 1 (
                        echo Unity editor resolver failed for version %UNITY_VERSION%.
                        exit /b 1
                    )

                    if not exist "%UNITY_RESOLVE_FILE%" (
                        echo Unity editor resolver did not write "%UNITY_RESOLVE_FILE%".
                        exit /b 1
                    )

                    set /p RESOLVED_UNITY_EXE=<"%UNITY_RESOLVE_FILE%"
                    if "%RESOLVED_UNITY_EXE%"=="" (
                        echo Unity editor resolver wrote an empty path.
                        exit /b 1
                    )

                    if /i "%RESOLVED_UNITY_EXE%"=="null" (
                        echo Unity editor resolver wrote null instead of a Unity.exe path.
                        exit /b 1
                    )

                    if not exist "%RESOLVED_UNITY_EXE%" (
                        echo Unity editor resolver wrote a missing executable: "%RESOLVED_UNITY_EXE%"
                        exit /b 1
                    )

                    echo Resolved Unity editor: "%RESOLVED_UNITY_EXE%"
                    endlocal
                    '''

                    echo "Unity editor path resolved and validated."
                }
            }
        }

        stage('Run Unity EditMode Tests') {
            when {
                expression {
                    return params.BUILD_WINDOWS == true || params.BUILD_WINDOWS?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_WEBGL == true || params.BUILD_WEBGL?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_IOS == true || params.BUILD_IOS?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_ANDROID_APK == true || params.BUILD_ANDROID_APK?.toString()?.equalsIgnoreCase('true') ||
                        params.BUILD_ANDROID_AAB == true || params.BUILD_ANDROID_AAB?.toString()?.equalsIgnoreCase('true')
                }
            }
            steps {
                powershell '''
                $ErrorActionPreference = "Continue"
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                New-Item -ItemType Directory -Path "$env:PROJECT_PATH\\TestResults" -Force | Out-Null
                Remove-Item -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt" -Force -ErrorAction Ignore
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:PROJECT_PATH\\TestResults\\EditMode.log" `
                    -NoProcessExit `
                    -UnityArguments @("-nographics", "-quit", "-runTests", "-testPlatform", "EditMode", "-testResults", "$env:PROJECT_PATH\\TestResults\\EditMode.xml")
                $editModeExit = $LASTEXITCODE

                powershell -NoProfile -ExecutionPolicy Bypass -File "$env:PROJECT_PATH\\Tools\\CI\\PrintUnityTestFailures.ps1" -ResultsPath "$env:PROJECT_PATH\\TestResults\\EditMode.xml" -PlatformName "EditMode"

                if (-not (Test-Path -LiteralPath "$env:PROJECT_PATH\\TestResults\\EditMode.xml" -PathType Leaf)) {
                    Write-Host "[BuildGate] EditMode test results were not created. Continuing build and deployment."
                    "[BuildGate][FINAL] EditMode tests FAILED because TestResults/EditMode.xml was not created; build was allowed to continue. See archived TestResults/EditMode.log." | Set-Content -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt"
                    exit 0
                }

                if ($editModeExit -ne 0) {
                    Write-Host "[BuildGate] EditMode tests failed with exit code $editModeExit. Continuing build and deployment."
                    "[BuildGate][FINAL] EditMode tests FAILED with exit code $editModeExit; build was allowed to continue. See archived TestResults/EditMode.xml and TestResults/EditMode.log." | Set-Content -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt"
                    exit 0
                }

                Write-Host "[BuildGate] EditMode tests passed. Continuing build."
                "[BuildGate][FINAL] EditMode tests PASSED; build was allowed to continue." | Set-Content -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt"
                exit 0
                '''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'TestResults/*.xml,TestResults/*.log', allowEmptyArchive: true
                }
            }
        }

        stage('Build Windows') {
            when { expression { return params.BUILD_WINDOWS == true || params.BUILD_WINDOWS?.toString()?.equalsIgnoreCase('true') } }
            steps {
                powershell '''
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:BUILD_LOG" `
                    -NoProcessExit `
                    -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildWindows")
                exit $LASTEXITCODE
                '''
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Build/Windows.zip', fingerprint: true
                }
            }
        }

        stage('Deploy Windows') {
            when { expression { return params.DEPLOY_WINDOWS == true || params.DEPLOY_WINDOWS?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "Windows_Build_${buildDate}.zip"
                    withCredentials([usernamePassword(credentialsId: 'nexus-admin', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASSWORD')]) {
                        bat '''
                        if not exist "%PROJECT_PATH%\\Build\\Windows.zip" (
                            echo Build artifact not found: "%PROJECT_PATH%\\Build\\Windows.zip"
                            exit /b 1
                        )
                        curl.exe --fail --show-error --location --user "%NEXUS_USER%:%NEXUS_PASSWORD%" --upload-file "%PROJECT_PATH%\\Build\\Windows.zip" "http://localhost:8081/repository/jenkins-unity/Windows_Build/%ARTIFACT_NAME%"
                        '''
                    }
                }
            }
        }

        stage('Build WebGL') {
            when { expression { return params.BUILD_WEBGL == true || params.BUILD_WEBGL?.toString()?.equalsIgnoreCase('true') } }
            steps {
                powershell '''
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:BUILD_LOG" `
                    -NoProcessExit `
                    -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildWebGL")
                exit $LASTEXITCODE
                '''
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Build/WebGL.zip', fingerprint: true
                }
            }
        }

        stage('Deploy WebGL') {
            when { expression { return params.DEPLOY_WEBGL == true || params.DEPLOY_WEBGL?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "WebGL_Build_${buildDate}.zip"
                    withCredentials([usernamePassword(credentialsId: 'nexus-admin', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASSWORD')]) {
                        bat '''
                        if not exist "%PROJECT_PATH%\\Build\\WebGL.zip" (
                            echo Build artifact not found: "%PROJECT_PATH%\\Build\\WebGL.zip"
                            exit /b 1
                        )
                        curl.exe --fail --show-error --location --user "%NEXUS_USER%:%NEXUS_PASSWORD%" --upload-file "%PROJECT_PATH%\\Build\\WebGL.zip" "http://localhost:8081/repository/jenkins-unity/WebGL_Build/%ARTIFACT_NAME%"
                        '''
                    }
                }
            }
        }

        stage('Build iOS') {
            when { expression { return params.BUILD_IOS == true || params.BUILD_IOS?.toString()?.equalsIgnoreCase('true') } }
            steps {
                powershell '''
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:BUILD_LOG" `
                    -NoProcessExit `
                    -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildIOS")
                exit $LASTEXITCODE
                '''
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Build/iOS.zip', fingerprint: true
                }
            }
        }

        stage('Deploy iOS Nexus') {
            when { expression { return params.DEPLOY_IOS_NEXUS == true || params.DEPLOY_IOS_NEXUS?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "iOS_Build_${buildDate}.zip"
                    withCredentials([usernamePassword(credentialsId: 'nexus-admin', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASSWORD')]) {
                        bat '''
                        if not exist "%PROJECT_PATH%\\Build\\iOS.zip" (
                            echo Build artifact not found: "%PROJECT_PATH%\\Build\\iOS.zip"
                            exit /b 1
                        )
                        curl.exe --fail --show-error --location --user "%NEXUS_USER%:%NEXUS_PASSWORD%" --upload-file "%PROJECT_PATH%\\Build\\iOS.zip" "http://localhost:8081/repository/jenkins-unity/iOS_Build/%ARTIFACT_NAME%"
                        '''
                    }
                }
            }
        }

        stage('Deploy iOS Mac') {
            when { expression { return params.DEPLOY_IOS_MAC == true || params.DEPLOY_IOS_MAC?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    env.PROJECT_NAME = PROJECT_NAME
                    powershell '''
                    net use \\\\192.168.2.175 /user:farhad $env:MAC_PASSWORD
                    Remove-Item -Path \\\\192.168.2.175\\farhad\\Projects\\Jenkins_Builds\\$env:PROJECT_NAME -Recurse -Force -ErrorAction Ignore
                    New-Item -ItemType directory -Path \\\\192.168.2.175\\farhad\\Projects\\Jenkins_Builds\\$env:PROJECT_NAME -Force
                    Copy-Item -Path "$env:PROJECT_PATH\\Build\\iOS" -Destination \\\\192.168.2.175\\farhad\\Projects\\Jenkins_Builds\\$env:PROJECT_NAME -Recurse -Force
                    net use \\\\192.168.2.175 /delete
                    '''
                }
            }
        }

        stage('Build Android APK') {
            when { expression { return params.BUILD_ANDROID_APK == true || params.BUILD_ANDROID_APK?.toString()?.equalsIgnoreCase('true') } }
            steps {
                powershell '''
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:BUILD_LOG" `
                    -NoProcessExit `
                    -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildAndroid", "-buildType", "APK")
                exit $LASTEXITCODE
                '''
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Build/AndroidAPK/WarlineCapture.apk', fingerprint: true
                }
            }
        }

        stage('Deploy Android APK') {
            when { expression { return params.DEPLOY_ANDROID_APK == true || params.DEPLOY_ANDROID_APK?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "Android_Build_${buildDate}.apk"
                    withCredentials([usernamePassword(credentialsId: 'nexus-admin', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASSWORD')]) {
                        bat '''
                        if not exist "%PROJECT_PATH%\\Build\\AndroidAPK\\WarlineCapture.apk" (
                            echo Build artifact not found: "%PROJECT_PATH%\\Build\\AndroidAPK\\WarlineCapture.apk"
                            exit /b 1
                        )
                        curl.exe --fail --show-error --location --user "%NEXUS_USER%:%NEXUS_PASSWORD%" --upload-file "%PROJECT_PATH%\\Build\\AndroidAPK\\WarlineCapture.apk" "http://localhost:8081/repository/jenkins-unity/AndroidAPK_Build/%ARTIFACT_NAME%"
                        '''
                    }
                }
            }
        }

        stage('Build Android AAB') {
            when { expression { return params.BUILD_ANDROID_AAB == true || params.BUILD_ANDROID_AAB?.toString()?.equalsIgnoreCase('true') } }
            steps {
                powershell '''
                New-Item -ItemType Directory -Path "$env:LOCALAPPDATA\\Unity\\Caches" -Force | Out-Null
                $unityPathFile = "$env:PROJECT_PATH\\unity-editor-path.txt"
                if (-not (Test-Path -LiteralPath $unityPathFile -PathType Leaf)) {
                    throw "Unity editor path file not found: $unityPathFile"
                }

                $unityExe = (Get-Content -LiteralPath $unityPathFile -Raw).Trim()
                if ([string]::IsNullOrWhiteSpace($unityExe) -or $unityExe -ieq "null") {
                    throw "Unity editor path file is empty or invalid: $unityPathFile"
                }

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe $unityExe `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:BUILD_LOG" `
                    -NoProcessExit `
                    -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildAndroid", "-buildType", "AAB")
                exit $LASTEXITCODE
                '''
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Build/AndroidAAB/WarlineCapture.aab', fingerprint: true
                }
            }
        }

        stage('Deploy Android AAB') {
            when { expression { return params.DEPLOY_ANDROID_AAB == true || params.DEPLOY_ANDROID_AAB?.toString()?.equalsIgnoreCase('true') } }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "Android_Build_${buildDate}.aab"
                    withCredentials([usernamePassword(credentialsId: 'nexus-admin', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASSWORD')]) {
                        bat '''
                        if not exist "%PROJECT_PATH%\\Build\\AndroidAAB\\WarlineCapture.aab" (
                            echo Build artifact not found: "%PROJECT_PATH%\\Build\\AndroidAAB\\WarlineCapture.aab"
                            exit /b 1
                        )
                        curl.exe --fail --show-error --location --user "%NEXUS_USER%:%NEXUS_PASSWORD%" --upload-file "%PROJECT_PATH%\\Build\\AndroidAAB\\WarlineCapture.aab" "http://localhost:8081/repository/jenkins-unity/AndroidAAB_Build/%ARTIFACT_NAME%"
                        '''
                    }
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'build.log,TestResults/*.xml,TestResults/*.log', allowEmptyArchive: true
        }
        failure {
            powershell '''
            try {
                $taskDir = $env:CODEX_TASK_DIR
                if ([string]::IsNullOrWhiteSpace($taskDir)) {
                    $taskDir = Join-Path $env:PROJECT_PATH "CodexTasks"
                }
                if ($taskDir.StartsWith("\\\\")) {
                    net use \\\\192.168.2.175 /user:farhad $env:MAC_PASSWORD
                }
                powershell -NoProfile -ExecutionPolicy Bypass -File "$env:PROJECT_PATH\\Tools\\CI\\QueueCodexJenkinsFailure.ps1" -TaskDir "$taskDir" -ProjectPath "$env:PROJECT_PATH" -BuildLog "$env:BUILD_LOG"
            } catch {
                Write-Host "[CodexQueue] Could not queue Codex failure task: $($_.Exception.Message)"
            } finally {
                if ($taskDir -and $taskDir.StartsWith("\\\\")) {
                    net use \\\\192.168.2.175 /delete
                }
            }
            '''
            archiveArtifacts artifacts: 'CodexTasks/**', allowEmptyArchive: true
        }
        cleanup {
            bat '''
            @echo off
            if exist "%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt" (
                type "%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt"
            ) else (
                echo [BuildGate][FINAL] EditMode tests were not run for this build.
            )
            '''
        }
    }
}
