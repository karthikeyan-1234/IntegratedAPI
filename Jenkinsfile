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
                    // Build for local use - no push needed for local Kubernetes
                    bat "docker build -t ${DOCKER_IMAGE}:${IMAGE_TAG} -t ${DOCKER_IMAGE}:latest ."
                }
            }
        }
        
        stage('Push to Docker Hub') {
            when {
                expression { return false } // Disabled for local development
            }
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
                echo 'Deploying IntegratedAPI and SQL Server to Kubernetes...'
                script {
                    // Copy current kubeconfig to workspace before deploying
                    bat """
                        copy /Y "%USERPROFILE%\\.kube\\config" "%WORKSPACE%\\kubeconfig"
                        set KUBECONFIG=%WORKSPACE%\\kubeconfig
                        kubectl apply -f sqlserver-deploy.yml
                    """
                    
                    // Deploy the Integrated API
                    bat """
                        set KUBECONFIG=%WORKSPACE%\\kubeconfig
                        kubectl apply -f integratedapi-deploy.yml
                    """
                    
                    // Restart & verify rollout for IntegratedAPI
                    bat """
                        set KUBECONFIG=%WORKSPACE%\\kubeconfig
                        kubectl rollout restart deployment/integratedapi-deployment
                    """
                    
                    bat """
                        set KUBECONFIG=%WORKSPACE%\\kubeconfig
                        kubectl rollout status deployment/integratedapi-deployment
                    """
                    
                    // Check SQL Server deployment status (non-blocking)
                    bat """
                        set KUBECONFIG=%WORKSPACE%\\kubeconfig
                        kubectl get pods -l app=sqlserver
                        echo Note: SQL Server may take 2-3 minutes to fully start. Check status with: kubectl get pods
                    """
                }
            }
        }
    }
    
    post {
        success {
            echo '✅ Build, Docker image creation, push, and deployment successful!'
            echo '🌐 IntegratedAPI accessible at http://localhost:30080'
            echo '📊 Swagger UI at http://localhost:30080/swagger/index.html'
            echo '🗄️ SQL Server available at 127.0.0.1,1433'
            echo '📦 3 IntegratedAPI pods load-balanced automatically by Kubernetes Service'
        }
        failure {
            echo '❌ Pipeline failed. Check logs for details.'
        }
        always {
            echo 'Pipeline execution completed.'
        }
    }
}