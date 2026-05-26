def PROJECT_NAME = 'WarlineCapture'
def UNITY_VERSION = '6000.4.0f1'
def CUSTOM_WORKSPACE = "D:\\Projects\\Jenkins\\${PROJECT_NAME}"
def UNITY_EDITOR = "D:\\Program Files\\Unity\\${UNITY_VERSION}\\Editor\\Unity.exe"

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
        UNITY_EXE = "${UNITY_EDITOR}"
        BUILD_LOG = "${CUSTOM_WORKSPACE}\\build.log"
        CODEX_TASK_DIR = "\\\\192.168.2.175\\farhad\\Projects\\Jenkins_Builds\\WarlineCapture\\CodexTasks"
        MAC_PASSWORD = credentials('MAC_PASSWORD')
    }

    stages {
        stage('Checkout Unity Project') {
            steps {
                deleteDir()
                checkout([
                    $class: 'GitSCM',
                    branches: [[name: '*/main']],
                    userRemoteConfigs: [[
                        url: 'https://github.com/farhad2na2/WarlineCapture.git',
                        credentialsId: 'github-pat-2'
                    ]],
                    extensions: [[
                        $class: 'SparseCheckoutPaths',
                        sparseCheckoutPaths: [
                            [path: '.gitattributes'],
                            [path: '.gitignore'],
                            [path: 'Assets'],
                            [path: 'Packages'],
                            [path: 'ProjectSettings'],
                            [path: 'Tools'],
                            [path: 'build.bat'],
                            [path: 'Jenkinsfile.groovy'],
                            [path: 'LICENSE.md'],
                            [path: 'README.md']
                        ]
                    ]]
                ])
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
                bat '''
                @echo off
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                if not exist "%PROJECT_PATH%\\TestResults" mkdir "%PROJECT_PATH%\\TestResults"
                if exist "%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt" del "%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt"

                "%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT_PATH%" -runTests -testPlatform EditMode -testResults "%PROJECT_PATH%\\TestResults\\EditMode.xml" -logFile "%PROJECT_PATH%\\TestResults\\EditMode.log"
                set EDITMODE_EXIT=%ERRORLEVEL%
                powershell -NoProfile -ExecutionPolicy Bypass -File "%PROJECT_PATH%\\Tools\\CI\\PrintUnityTestFailures.ps1" -ResultsPath "%PROJECT_PATH%\\TestResults\\EditMode.xml" -PlatformName "EditMode"

                if not "%EDITMODE_EXIT%"=="0" (
                    echo [BuildGate] EditMode tests failed with exit code %EDITMODE_EXIT%. Continuing build and deployment.
                    echo [BuildGate][FINAL] EditMode tests FAILED with exit code %EDITMODE_EXIT%; build was allowed to continue. See archived TestResults/EditMode.xml and TestResults/EditMode.log.>"%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt"
                    exit /b 0
                )

                echo [BuildGate] EditMode tests passed. Continuing build.
                echo [BuildGate][FINAL] EditMode tests PASSED; build was allowed to continue.>"%PROJECT_PATH%\\TestResults\\BuildGateStatus.txt"
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
                bat '''
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                "%UNITY_EXE%" -executeMethod BuildScript.BuildWindows -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%BUILD_LOG%"
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
                bat '''
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                "%UNITY_EXE%" -executeMethod BuildScript.BuildWebGL -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%BUILD_LOG%"
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
                bat '''
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                "%UNITY_EXE%" -executeMethod BuildScript.BuildIOS -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%BUILD_LOG%"
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
                bat '''
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                "%UNITY_EXE%" -executeMethod BuildScript.BuildAndroid -buildType APK -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%BUILD_LOG%"
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
                bat '''
                if not exist "%LOCALAPPDATA%\\Unity\\Caches" mkdir "%LOCALAPPDATA%\\Unity\\Caches"
                "%UNITY_EXE%" -executeMethod BuildScript.BuildAndroid -buildType AAB -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%BUILD_LOG%"
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
