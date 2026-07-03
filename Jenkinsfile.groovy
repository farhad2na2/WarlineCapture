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
    }

    environment {
        PROJECT_PATH = "${CUSTOM_WORKSPACE}"
        UNITY_VERSION = "${UNITY_EDITOR_VERSION}"
        UNITY_EXE = ''
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
                    def resolvedUnityExe = powershell(
                        returnStdout: true,
                        script: '''
                        $resolved = & "$env:PROJECT_PATH\\Tools\\CI\\ResolveUnityEditor.ps1" -UnityVersion "$env:UNITY_VERSION"
                        if ($LASTEXITCODE -ne 0) {
                            exit $LASTEXITCODE
                        }
                        $resolved |
                            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                            Select-Object -Last 1
                        '''
                    ).trim()

                    if (!resolvedUnityExe || resolvedUnityExe.equalsIgnoreCase('null')) {
                        error "Unity editor resolver returned no executable for version ${env.UNITY_VERSION}. Check Jenkins console output and set UNITY_EXE_OVERRIDE to the installed Unity.exe path."
                    }

                    env.UNITY_EXE = resolvedUnityExe
                    echo "Using Unity editor: ${env.UNITY_EXE}"
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

                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" `
                    -UnityExe "$env:UNITY_EXE" `
                    -ProjectPath "$env:PROJECT_PATH" `
                    -LogFile "$env:PROJECT_PATH\\TestResults\\EditMode.log" `
                    -UnityArguments @("-nographics", "-runTests", "-testPlatform", "EditMode", "-testResults", "$env:PROJECT_PATH\\TestResults\\EditMode.xml")
                $editModeExit = $LASTEXITCODE

                powershell -NoProfile -ExecutionPolicy Bypass -File "$env:PROJECT_PATH\\Tools\\CI\\PrintUnityTestFailures.ps1" -ResultsPath "$env:PROJECT_PATH\\TestResults\\EditMode.xml" -PlatformName "EditMode"

                if ($editModeExit -ne 0) {
                    Write-Host "[BuildGate] EditMode tests failed with exit code $editModeExit. Continuing build and deployment."
                    "[BuildGate][FINAL] EditMode tests FAILED with exit code $editModeExit; build was allowed to continue. See archived TestResults/EditMode.xml and TestResults/EditMode.log." | Set-Content -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt"
                    exit 0
                }

                Write-Host "[BuildGate] EditMode tests passed. Continuing build."
                "[BuildGate][FINAL] EditMode tests PASSED; build was allowed to continue." | Set-Content -LiteralPath "$env:PROJECT_PATH\\TestResults\\BuildGateStatus.txt"
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
                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" -UnityExe "$env:UNITY_EXE" -ProjectPath "$env:PROJECT_PATH" -LogFile "$env:BUILD_LOG" -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildWindows")
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
                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" -UnityExe "$env:UNITY_EXE" -ProjectPath "$env:PROJECT_PATH" -LogFile "$env:BUILD_LOG" -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildWebGL")
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
                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" -UnityExe "$env:UNITY_EXE" -ProjectPath "$env:PROJECT_PATH" -LogFile "$env:BUILD_LOG" -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildIOS")
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
                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" -UnityExe "$env:UNITY_EXE" -ProjectPath "$env:PROJECT_PATH" -LogFile "$env:BUILD_LOG" -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildAndroid", "-buildType", "APK")
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
                & "$env:PROJECT_PATH\\Tools\\CI\\InvokeUnity.ps1" -UnityExe "$env:UNITY_EXE" -ProjectPath "$env:PROJECT_PATH" -LogFile "$env:BUILD_LOG" -UnityArguments @("-quit", "-executeMethod", "BuildScript.BuildAndroid", "-buildType", "AAB")
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
