pipeline {
    agent any

    environment {
        DOTNET_ROOT = '/root/.dotnet'
        PATH = "/root/.dotnet:/root/.dotnet/tools:${env.PATH}"
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        SONAR_HOST_URL = 'http://localhost:9000'  // Service name from docker-compose
        SONAR_TOKEN = credentials('SONAR_TOKEN') // Jenkins credentials ID
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

        stage('Wait for SonarQube') {
            steps {
                sh '''
                    echo "Checking if SonarQube is up at $SONAR_HOST_URL..."
                    for i in {1..30}; do
                        if curl -s $SONAR_HOST_URL/api/system/status | grep -q '"status":"UP"'; then
                            echo "SonarQube is UP!"
                            break
                        fi
                        echo "Waiting for SonarQube... ($i/30)"
                        sleep 5
                    done
                '''
            }
        }

        stage('SonarQube Analysis') {
            steps {
                sh '''
                    echo "Using SonarQube token: ${SONAR_TOKEN:0:4}****"
                    echo "SonarQube URL: $SONAR_HOST_URL"

                    dotnet sonarscanner --version

                    dotnet sonarscanner begin \
                        /k:"Prelevements_par_caisse" \
                        /d:sonar.host.url="$SONAR_HOST_URL" \
                        /d:sonar.login=$SONAR_TOKEN

                    dotnet build
                    dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN
                '''
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --verbosity normal'
            }
        }
    }
}
