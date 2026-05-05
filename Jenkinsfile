pipeline {
    agent {
        label 'uipath-agent'  // Jenkins agent running on your Remote Desktop
    }

    parameters {
        choice(
            name: 'WORKFLOW',
            choices: ['IssueManagement', 'TaskManagement'],
            description: 'Select which workflow to run'
        )
        string(
            name: 'ORCHESTRATOR_URL',
            defaultValue: 'https://cloud.uipath.com',
            description: 'UiPath Orchestrator URL'
        )
        string(
            name: 'ORCHESTRATOR_FOLDER',
            defaultValue: '205476',
            description: 'Orchestrator Folder ID'
        )
    }

    environment {
        PROJECT_NAME    = 'IssueManagement'
        GITHUB_REPO     = 'https://github.com/abbaskhan360/ui-path-automation.git'
        NUPKG_OUTPUT    = "${WORKSPACE}\\output"
    }

    stages {

        stage('Checkout Code') {
            steps {
                echo '=== Pulling latest code from GitHub ==='
                git branch: 'main',
                    url: "${GITHUB_REPO}"
            }
        }

        stage('Determine Entry Point') {
            steps {
                script {
                    if (params.WORKFLOW == 'IssueManagement') {
                        env.ENTRY_POINT = 'IssueManagementSingle\\Main.xaml'
                        env.PROCESS_NAME = 'IssueManagement_Main'
                    } else {
                        env.ENTRY_POINT = 'TaskManagement\\TaskMain.xaml'
                        env.PROCESS_NAME = 'IssueManagement_TaskMain'
                    }
                    echo "Running workflow: ${env.ENTRY_POINT}"
                }
            }
        }

        stage('Pack UiPath Project') {
            steps {
                echo '=== Packing UiPath Project ==='
                UiPathPack(
                    projectJsonPath: "${WORKSPACE}\\project.json",
                    outputPath: "${NUPKG_OUTPUT}",
                    version: AutoVersion()
                )
            }
        }

        stage('Deploy to Orchestrator') {
            steps {
                echo '=== Deploying to Orchestrator ==='
                UiPathDeploy(
                    packagePath: "${NUPKG_OUTPUT}",
                    orchestratorAddress: "${params.ORCHESTRATOR_URL}",
                    orchestratorTenant: "${ORCHESTRATOR_TENANT}",
                    folderName: "${ORCHESTRATOR_FOLDER_NAME}",
                    credentials: ExternalApp(
                        accountForApp: "${UIPATH_ACCOUNT}",
                        applicationId: "${UIPATH_APP_ID}",
                        applicationSecret: credentials('uipath-app-secret'),
                        applicationScope: "${UIPATH_APP_SCOPE}"
                    )
                )
            }
        }

        stage('Run Automation') {
            steps {
                echo "=== Running ${env.PROCESS_NAME} ==="
                UiPathRunJob(
                    orchestratorAddress: "${params.ORCHESTRATOR_URL}",
                    orchestratorTenant: "${ORCHESTRATOR_TENANT}",
                    folderName: "${ORCHESTRATOR_FOLDER_NAME}",
                    processName: "${env.PROCESS_NAME}",
                    credentials: ExternalApp(
                        accountForApp: "${UIPATH_ACCOUNT}",
                        applicationId: "${UIPATH_APP_ID}",
                        applicationSecret: credentials('uipath-app-secret'),
                        applicationScope: "${UIPATH_APP_SCOPE}"
                    ),
                    parametersFilePath: '',
                    strategy: Dynamically(jobsCount: 1),
                    timeout: 3600,
                    waitForJobCompletion: true,
                    failWhenJobFails: true
                )
            }
        }
    }

    post {
        success {
            echo "=== ${env.PROCESS_NAME} completed SUCCESSFULLY ==="
        }
        failure {
            echo "=== ${env.PROCESS_NAME} FAILED ==="
        }
        always {
            echo '=== Pipeline finished ==='
            // Clean up workspace
            cleanWs()
        }
    }
}
