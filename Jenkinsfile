pipeline {
    agent any

    environment {
        SONAR_TOKEN = credentials('sonar-token') // Add this in Jenkins Credentials
    }

    tools {
        dotnet 'dotnet8' // Must be configured in Jenkins (Manage Jenkins > Global Tool Configuration)
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'https://github.com/chahe-dridi/prelevements-backend.git'
            }
        }

        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --verbosity normal'
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('My SonarQube Server') {
                    sh """
                        dotnet sonarscanner begin /k:"prelevements" /d:sonar.login=$SONAR_TOKEN
                        dotnet build
                        dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN
                    """
                }
            }
        }

        stage('Quality Gate') {
            steps {
                timeout(time: 1, unit: 'MINUTES') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }
    }
}
