pipeline {
    agent any

    environment {
        DOCKERHUB_CREDENTIALS = credentials('dockerhub-credentials')
        IMAGE_NAME = "${DOCKERHUB_CREDENTIALS_USR}/todoapp"
        IMAGE_TAG = "${env.BUILD_NUMBER}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test tests/TodoApp.Tests.csproj --logger "trx;LogFileName=test-results.trx"'
            }
            post {
                always {
                    junit allowEmptyResults: true, testResults: '**/*.trx'
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                sh 'docker build -t ${IMAGE_NAME}:${IMAGE_TAG} -t ${IMAGE_NAME}:latest .'
            }
        }

        stage('Push to DockerHub') {
            steps {
                sh 'echo ${DOCKERHUB_CREDENTIALS_PSW} | docker login -u ${DOCKERHUB_CREDENTIALS_USR} --password-stdin'
                sh 'docker push ${IMAGE_NAME}:${IMAGE_TAG}'
                sh 'docker push ${IMAGE_NAME}:latest'
            }
        }

        stage('Deploy to Kubernetes') {
            steps {
                sh 'kubectl apply -f k8s/mssql-secret.yaml'
                sh 'kubectl apply -f k8s/mssql-pvc.yaml'
                sh 'kubectl apply -f k8s/mssql-deployment.yaml'
                sh 'kubectl set image deployment/todoapp todoapp=${IMAGE_NAME}:${IMAGE_TAG} --record || kubectl apply -f k8s/app-deployment.yaml'
                sh 'kubectl apply -f k8s/app-service.yaml'
                sh 'kubectl rollout status deployment/todoapp'
            }
        }
    }

    post {
        always {
            sh 'docker logout || true'
        }
    }
}
