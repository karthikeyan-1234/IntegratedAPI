pipeline {
    agent any

    environment {
        KUBECONFIG = credentials('KUBECONFIG')
    }

    stages {
        stage('Deploy SQL Server') {
            steps {
                bat '''
                kubectl apply -f sqlserver-deploy.yml --kubeconfig=%KUBECONFIG%
                '''
            }
        }

        stage('Deploy Integrated API') {
            steps {
                bat '''
                kubectl apply -f integratedapi-deploy.yml --kubeconfig=%KUBECONFIG%
                '''
            }
        }

        stage('Deploy Kong and Database') {
            steps {
                echo 'Cleaning up old Kong resources...'
                bat '''
                kubectl delete svc kong-proxy --ignore-not-found=true --kubeconfig=%KUBECONFIG%
                kubectl delete svc kong-admin --ignore-not-found=true --kubeconfig=%KUBECONFIG%
                kubectl delete deployment kong --ignore-not-found=true --kubeconfig=%KUBECONFIG%
                kubectl delete deployment kong-database --ignore-not-found=true --kubeconfig=%KUBECONFIG%
                kubectl delete job kong-bootstrap --ignore-not-found=true --kubeconfig=%KUBECONFIG%
                '''

                echo 'Deploying Kong Gateway and Database...'
                bat '''
                kubectl apply -f kong-deploy.yml --kubeconfig=%KUBECONFIG%
                echo Waiting for Kong Database to be ready...
                kubectl wait --for=condition=available deployment/kong-database --timeout=180s --kubeconfig=%KUBECONFIG%
                '''
            }
        }

        stage('Verify Kong Services') {
            steps {
                bat '''
                kubectl get svc kong-proxy kong-admin --kubeconfig=%KUBECONFIG%
                '''
            }
        }
    }

    post {
        failure {
            echo '❌ Pipeline failed. Check logs for details.'
        }
        success {
            echo '✅ Kong and services deployed successfully!'
        }
    }
}
