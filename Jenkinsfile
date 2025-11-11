pipeline {
    agent any

    environment {
        IMAGE_NAME = "karthiknat/integratedapi"
        IMAGE_TAG = "${BUILD_NUMBER}"
        DOCKER_CREDENTIALS_ID = "dockerhub-credentials"
    }

    stages {
        stage('Checkout') {
            steps {
                echo "Checking out code..."
                git branch: 'master', url: 'https://github.com/karthikeyan-1234/IntegratedAPI.git'
            }
        }

        stage('Clean Build Artifacts') {
            steps {
                echo "Cleaning previous build artifacts..."
                bat '''
                    if exist obj rmdir /s /q obj
                    if exist bin rmdir /s /q bin
                    if exist publish rmdir /s /q publish
                '''
            }
        }

        stage('Build .NET Project') {
            steps {
                echo "Building project..."
                bat 'dotnet restore'
                bat 'dotnet build --configuration Release'
            }
        }

        stage('Publish .NET Project') {
            steps {
                echo "Publishing project..."
                bat 'dotnet publish --configuration Release -o ./publish'
            }
        }

        stage('Build Docker Image') {
            steps {
                echo "Building Docker image..."
                script {
                    bat 'docker buildx create --use || echo "buildx already exists"'
                    // Add --load to export image to local docker images
                    bat "docker buildx build --platform linux/amd64 -t ${IMAGE_NAME}:${IMAGE_TAG} -t ${IMAGE_NAME}:latest --load ."
                }
            }
        }

        stage('Push Docker Image') {
            steps {
                echo "Pushing image to Docker Hub..."
                withCredentials([usernamePassword(credentialsId: "${DOCKER_CREDENTIALS_ID}", usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                    bat "docker login -u %DOCKER_USER% -p %DOCKER_PASS%"
                    bat "docker push ${IMAGE_NAME}:${IMAGE_TAG}"
                    bat "docker push ${IMAGE_NAME}:latest"
                }
            }
        }

        stage('Deploy to Kubernetes') {
            steps {
                echo "Deploying to Kubernetes..."
                bat 'kubectl apply -f kong-deploy.yml || echo "Kubernetes deployment skipped or not configured."'
            }
        }
    }

    post {
        always {
            echo "Pipeline completed."
        }
    }
}
