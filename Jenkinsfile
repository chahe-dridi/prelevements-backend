pipeline {
    agent any

    environment {
        SONAR_TOKEN = credentials('SONAR_TOKEN') // You add this secret in Jenkins Credentials
    }

    stages {
        stage('Checkout') {
            steps {
                git 'https://github.com/chahe-dridi/prelevements-backend.git'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build'
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('My SonarQube Server') {
                    sh 'dotnet sonarscanner begin /k:"Prelevements_par_caisse" /d:sonar.login=$SONAR_TOKEN'
                    sh 'dotnet build'
                    sh 'dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN'
                }
            }
        }
    }
}
