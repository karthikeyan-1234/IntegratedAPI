pipeline {
    agent any

    environment {
        DOCKERHUB_USER = 'karthiknat'
        IMAGE_NAME = 'integratedapi'
        VERSION = '28'
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checking out code...'
                git branch: 'master', url: 'https://github.com/karthikeyan-1234/IntegratedAPI.git'
            }
        }

        stage('Clean Build Artifacts') {
            steps {
                echo 'Cleaning previous build artifacts...'
                bat '''
                    if exist obj rmdir /s /q obj
                    if exist bin rmdir /s /q bin
                    if exist publish rmdir /s /q publish
                '''
            }
        }

        stage('Build .NET Project') {
            steps {
                echo 'Building project...'
                bat 'dotnet restore'
                bat 'dotnet build --configuration Release'
            }
        }

        stage('Publish .NET Project') {
            steps {
                echo 'Publishing project...'
                bat 'dotnet publish --configuration Release -o ./publish'
            }
        }

        stage('Build Docker Image') {
            steps {
                echo 'Building Docker image...'
                script {
                    bat '''
                        docker buildx create --use || echo "buildx already exists"
                        docker buildx build --platform linux/amd64 -t %DOCKERHUB_USER%/%IMAGE_NAME%:%VERSION% -t %DOCKERHUB_USER%/%IMAGE_NAME%:latest .
                    '''
                }
            }
        }

        stage('Push to Docker Hub') {
            steps {
                echo 'Pushing Docker image to Docker Hub...'
                withCredentials([usernamePassword(credentialsId: 'dockerhub-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                    script {
                        bat """
                            echo %DOCKER_PASS% | docker login -u %DOCKER_USER% --password-stdin
                            timeout /t 5
                            docker push %DOCKERHUB_USER%/%IMAGE_NAME%:%VERSION%
                            docker push %DOCKERHUB_USER%/%IMAGE_NAME%:latest
                            docker logout
                        """
                    }
                }
            }
        }

        stage('Cleanup Docker') {
            steps {
                echo 'Cleaning up local Docker images...'
                bat '''
                    docker image prune -f
                '''
            }
        }
    }
}
