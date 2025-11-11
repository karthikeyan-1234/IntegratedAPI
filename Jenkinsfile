pipeline {
    agent any
    environment {
        DOCKER_IMAGE = 'karthiknat/integratedapi'
        IMAGE_TAG = "${BUILD_NUMBER}"
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
                script {
                    bat '''
                        if exist obj rmdir /s /q obj
                        if exist bin rmdir /s /q bin
                        if exist publish rmdir /s /q publish
                    '''
                }
            }
        }
        stage('Build .NET Project') {
            steps {
                echo 'Building IntegratedAPI project...'
                script {
                    bat "dotnet restore"
                    bat "dotnet build --configuration Release"
                }
            }
        }
        stage('Publish .NET Project') {
            steps {
                echo 'Publishing project...'
                script {
                    bat "dotnet publish --configuration Release -o ./publish"
                }
            }
        }
        stage('Build Docker Image') {
            steps {
                echo 'Building Docker image...'
                script {
                    bat "docker build -t ${DOCKER_IMAGE}:${IMAGE_TAG} -t ${DOCKER_IMAGE}:latest ."
                }
            }
        }
        stage('Push to Docker Hub') {
            steps {
                echo 'Pushing Docker image to Docker Hub...'
                script {
                    withCredentials([usernamePassword(credentialsId: 'dockerhub-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                        bat "docker login -u %DOCKER_USER% -p %DOCKER_PASS%"
                        bat "docker push ${DOCKER_IMAGE}:${IMAGE_TAG}"
                        bat "docker push ${DOCKER_IMAGE}:latest"
                    }
                }
            }
        }
        stage('Deploy to Kubernetes') {
            steps {
                echo 'Deploying SQL Server, IntegratedAPI, and Kong to Kubernetes...'
                script {
                    withCredentials([file(credentialsId: 'kube-config', variable: 'KUBECONFIG')]) {

                        // Deploy SQL Server if not already present
                        bat "kubectl apply -f sqlserver-deploy.yml --kubeconfig=%KUBECONFIG% || echo 'SQL Server already deployed.'"

                        // Deploy Integrated API
                        bat "kubectl apply -f integratedapi-deploy.yml --kubeconfig=%KUBECONFIG%"

                        // 🦍 Deploy Kong (database + migrations + gateway)
                        echo 'Deploying Kong API Gateway and its database...'
                        bat "kubectl apply -f kong-deploy.yml --kubeconfig=%KUBECONFIG%"

                        // Wait for Postgres DB to be ready before running migrations
                        echo 'Waiting for Kong database to be ready...'
                        bat "kubectl rollout status deployment/kong-database --timeout=120s --kubeconfig=%KUBECONFIG%"

                        // Wait for Kong migration job to finish
                        echo 'Waiting for Kong migrations job to complete...'
                        bat "kubectl wait --for=condition=complete job/kong-migration --timeout=120s --kubeconfig=%KUBECONFIG% || echo '⚠️ Kong migration job may still be running'"

                        // Wait for Kong Gateway to become ready
                        echo 'Waiting for Kong deployment rollout...'
                        bat "kubectl rollout status deployment/kong --timeout=180s --kubeconfig=%KUBECONFIG%"

                        // Restart & verify rollout for IntegratedAPI
                        bat "kubectl rollout restart deployment/integratedapi-deployment --kubeconfig=%KUBECONFIG%"
                        bat "kubectl rollout status deployment/integratedapi-deployment --timeout=120s --kubeconfig=%KUBECONFIG%"
                    }
                }
            }
        }
    }
    post {
        success {
            echo '✅ Build, Docker image creation, push, and deployment successful!'
            echo '🌐 IntegratedAPI (direct) accessible at http://localhost:30080'
            echo '🦍 Kong API Gateway accessible at http://localhost:30800'
            echo '📡 Access your API via Kong at http://localhost:30800/api'
            echo '⚙️ Kong Admin API (internal) at http://kong-admin:8001'
            echo '🗄️ SQL Server available via sqlserver-service:1433 inside the cluster.'
        }
        failure {
            echo '❌ Pipeline failed. Check logs for details.'
        }
    }
}
