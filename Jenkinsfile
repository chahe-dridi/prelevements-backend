pipeline {
    agent any

    environment {
        DOTNET_ROOT = '/root/.dotnet'
        PATH = "/root/.dotnet:/root/.dotnet/tools:${env.PATH}"
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        SONAR_TOKEN = credentials('SONAR_TOKEN') // Add your SonarQube token in Jenkins Credentials and use this ID
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
                sh 'dotnet build --no-restore'
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('My SonarQube Server') {
                    sh "dotnet sonarscanner begin /k:\"Prelevements_par_caisse\" /d:sonar.login=$SONAR_TOKEN"
                    sh 'dotnet build --no-restore'
                    sh "dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN"
                }
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --verbosity normal'
            }
        }
    }
}
