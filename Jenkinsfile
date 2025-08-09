pipeline {
    agent any

      environment {
    DOTNET_ROOT = '/root/.dotnet'
    PATH = "/root/.dotnet:/root/.dotnet/tools:${env.PATH}"
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
   // SONAR_HOST_URL = 'http://sonarqube:9000'  // Correct for container network
       SONAR_HOST_URL = 'http://localhost:9000'
    SONAR_TOKEN = credentials('SONAR_TOKEN')
   
}

    stages {
        stage('Checkout') {
            steps {
                git branch: 'master', url: 'https://github.com/chahe-dridi/prelevements-backend.git'
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
        withCredentials([string(credentialsId: 'SONAR_TOKEN', variable: 'SONAR_TOKEN')]) {
            sh """
                bash -c '
                echo "Using SonarQube token: \${SONAR_TOKEN:0:4}****"
                dotnet sonarscanner --version
                dotnet sonarscanner begin \\
                    /k:"Prelevements_par_caisse" \\
                    /d:sonar.host.url="http://localhost:9000" \\
                    /d:sonar.login=\$SONAR_TOKEN
                dotnet build
                dotnet sonarscanner end /d:sonar.login=\$SONAR_TOKEN
                '
            """
        }
    }
}









        stage('Test') {
            steps {
                // Restore only the test projects explicitly (optional but recommended)
                sh 'dotnet restore ./Prelevements_par_caisse.Tests/Prelevements_par_caisse.Tests.csproj'
                
                // Build the test project (and dependencies)
                sh 'dotnet build ./Prelevements_par_caisse.Tests/Prelevements_par_caisse.Tests.csproj'
                
                // Run tests with detailed verbosity, fail pipeline if tests fail
                sh 'dotnet test ./Prelevements_par_caisse.Tests/Prelevements_par_caisse.Tests.csproj --verbosity normal --no-build --results-directory ./TestResults --logger trx'
            }
        }






    }
}
