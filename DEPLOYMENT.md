# OrderFlow Order Management System - Deployment Guide

This document provides comprehensive instructions for deploying the OrderFlow Order Management System using Docker Compose or Kubernetes with Helm Chart.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Docker Compose Deployment](#docker-compose-deployment)
- [Kubernetes & Helm Chart Deployment](#kubernetes--helm-chart-deployment)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

## Architecture Overview

The OrderFlow system consists of the following components:

- **Frontend (Client)**: React/Vite application served on port 3000
- **Backend API (Server)**: .NET 8.0 API served on port 5063
- **Database**: SQL Server 2022 on port 1433
- **Message Queue**: RabbitMQ 3.13 on ports 5672 (AMQP) and 15672 (Management UI)

### Component Relationships

```
┌──────────────────────────────────────────────────┐
│          Client (React/Vite)                     │
│          Port: 3000                              │
└──────────────┬───────────────────────────────────┘
               │
               ├─ HTTP Requests to
               ▼
┌──────────────────────────────────────────────────┐
│          API (.NET 8.0)                          │
│          Port: 5063                              │
│   ┌──────────────────────────────────────────┐   │
│   │  Swagger UI: /swagger/v1/swagger.json    │   │
│   └──────────────────────────────────────────┘   │
└──────────────┬──────────────┬────────────────────┘
               │              │
      Queries & Commands   Events
               │              │
               ▼              ▼
        ┌─────────────┐  ┌──────────────┐
        │ SQL Server  │  │   RabbitMQ   │
        │ Port: 1433  │  │ Port: 5672   │
        └─────────────┘  │ Port: 15672  │
                         └──────────────┘
```

## Prerequisites

### For Docker Compose

- Docker 20.10+
- Docker Compose 1.29+
- Minimum 4GB RAM available
- Ports 3000, 5063, 1433, 5672, 15672 available

### For Kubernetes & Helm

- Kubernetes 1.24+
- Helm 3.12+
- kubectl configured to access your cluster
- StorageClass available for persistent volumes
- Ingress Controller installed (nginx recommended)
- Minimum 2 nodes with 2CPU and 2GB RAM each

---

## Docker Compose Deployment

### Quick Start

```bash
# Navigate to the project root
cd /Users/prophet/Documents/GitHub/amrod

# Build and start all services
docker-compose up -d --build

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Services Overview

#### 1. SQL Server Database

- **Image**: mcr.microsoft.com/mssql/server:2022-latest
- **Container Name**: orderflow-sqlserver
- **Port**: 1433
- **Credentials**:
  - Username: `sa`
  - Password: `YourStrong@Passw0rd`
- **Volumes**: `sqlserver_data:/var/opt/mssql`

**Access Database**:

```bash
# Using SQL Server Management Tools
sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'

# Or via Docker
docker exec -it orderflow-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'
```

#### 2. RabbitMQ Message Queue

- **Image**: rabbitmq:3.13-management
- **Container Name**: orderflow-rabbitmq
- **Ports**:
  - 5672 (AMQP protocol)
  - 15672 (Management UI)
- **Credentials**:
  - Username: `guest`
  - Password: `guest`
- **Management UI**: http://localhost:15672

#### 3. API Server

- **Image**: orderflow-api:latest
- **Container Name**: orderflow-api
- **Port**: 5063
- **Environment**:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__DefaultConnection`: Auto-configured to SQL Server
  - `RabbitMq__HostName`: Auto-configured to rabbitmq
- **Dependencies**: Waits for SQL Server and RabbitMQ health checks

**Access API**:

- Swagger UI: http://localhost:5063/swagger
- Health Endpoint: http://localhost:5063/health

**View API Logs**:

```bash
docker-compose logs -f api
```

#### 4. Client (UI)

- **Image**: orderflow-client:latest
- **Container Name**: orderflow-client
- **Port**: 3000
- **Environment**:
  - `VITE_API_URL=http://api:5063`

**Access UI**: http://localhost:3000

**View Client Logs**:

```bash
docker-compose logs -f client
```

### Useful Docker Compose Commands

```bash
# View running containers
docker-compose ps

# View logs for a specific service
docker-compose logs -f [service_name]

# Rebuild a specific service
docker-compose up -d --build [service_name]

# Stop all services
docker-compose stop

# Remove all containers and volumes
docker-compose down -v

# Execute command in container
docker-compose exec [service_name] [command]

# View resource usage
docker stats

# Inspect container
docker-compose exec [service_name] /bin/bash
```

### Data Persistence

- **SQL Server**: Data is persisted in `sqlserver_data` volume
- **RabbitMQ**: Data is persisted in `rabbitmq_data` volume

To backup and restore:

```bash
# Backup SQL Server database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "BACKUP DATABASE [OrderManagement] TO DISK = '/var/opt/mssql/backup/OrderManagement.bak'"

# Restore SQL Server database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "RESTORE DATABASE [OrderManagement] FROM DISK = '/var/opt/mssql/backup/OrderManagement.bak'"
```

---

## Kubernetes & Helm Chart Deployment

### Prerequisites Setup

```bash
# Install Helm (if not already installed)
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Add common Helm repositories
helm repo add stable https://charts.helm.sh/stable
helm repo update

# Verify Kubernetes access
kubectl cluster-info
kubectl get nodes
```

### Quick Start with Helm

```bash
# Navigate to helm chart directory
cd helm/

# Install the chart
helm install orderflow ./amrod -n orderflow --create-namespace

# View deployment status
helm status orderflow -n orderflow

# View resources created
kubectl get all -n orderflow

# View logs for API
kubectl logs -f deployment/orderflow-api -n orderflow

# View logs for Client
kubectl logs -f deployment/orderflow-client -n orderflow
```

### Helm Chart Structure

```
helm/amrod/
├── Chart.yaml              # Chart metadata
├── values.yaml             # Default configuration values
├── templates/
│   ├── _helpers.tpl        # Template helpers and functions
│   ├── namespace.yaml      # Kubernetes namespace
│   ├── deployment-api.yaml # API deployment
│   ├── deployment-client.yaml # Client deployment
│   ├── database.yaml       # SQL Server deployment & service
│   ├── rabbitmq.yaml       # RabbitMQ deployment & service
│   ├── service.yaml        # API and Client services
│   ├── ingress.yaml        # Ingress resources
│   ├── secrets.yaml        # Database and RabbitMQ secrets
│   └── hpa.yaml            # Horizontal Pod Autoscalers
└── README.md
```

### Chart Values Customization

#### Override Default Values

Create a `custom-values.yaml`:

```yaml
# Custom values for AMROD Helm deployment

api:
  replicaCount: 3
  resources:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 1000m
      memory: 1Gi
  autoscaling:
    maxReplicas: 10

client:
  replicaCount: 2

sqlserver:
  persistence:
    size: 20Gi
  resources:
    requests:
      cpu: 1000m
      memory: 2Gi

rabbitmq:
  persistence:
    size: 10Gi
```

Install with custom values:

```bash
helm install amrod ./amrod \
  -n amrod \
  --create-namespace \
  -f custom-values.yaml
```

#### Common Value Overrides

```bash
# Change API replicas
helm install amrod ./amrod -n amrod --set api.replicaCount=3

# Change client image tag
helm install amrod ./amrod -n amrod --set client.image.tag=v1.0.0

# Disable RabbitMQ
helm install amrod ./amrod -n amrod --set rabbitmq.enabled=false

# Configure ingress hosts
helm install amrod ./amrod -n amrod \
  --set api.ingress.hosts[0].host=api.mycompany.com \
  --set client.ingress.hosts[0].host=mycompany.com
```

### Helm Chart Useful Commands

```bash
# Dry run to see what will be deployed
helm install amrod ./amrod -n amrod --dry-run --debug

# Validate chart syntax
helm lint ./amrod

# Get all chart values
helm get values amrod -n amrod

# Update deployment with new values
helm upgrade amrod ./amrod -n amrod -f custom-values.yaml

# Rollback to previous release
helm rollback amrod -n amrod

# Delete deployment
helm uninstall amrod -n amrod

# Get chart information
helm show chart ./amrod
helm show values ./amrod
```

### Kubernetes Resources Created

After installation, the following resources are created:

```
Namespace
├── Deployments (3)
│   ├── amrod-api
│   ├── amrod-client
│   └── amrod-sqlserver
│   └── amrod-rabbitmq
├── Services (4)
│   ├── amrod-api
│   ├── amrod-client
│   ├── amrod-sqlserver
│   └── amrod-rabbitmq
├── Ingresses (3)
│   ├── amrod-api
│   ├── amrod-client
│   └── amrod-rabbitmq
├── Secrets (3)
│   ├── amrod-sqlserver-secret
│   ├── amrod-rabbitmq-secret
│   └── amrod-db-secret
├── PersistentVolumeClaims (2)
│   ├── amrod-sqlserver-pvc
│   └── amrod-rabbitmq-pvc
└── HorizontalPodAutoscalers (2)
    ├── amrod-api
    └── amrod-client
```

### Access Services in Kubernetes

```bash
# Port forward to API
kubectl port-forward svc/amrod-api 5063:5063 -n amrod
# Access: http://localhost:5063

# Port forward to Client
kubectl port-forward svc/amrod-client 3000:3000 -n amrod
# Access: http://localhost:3000

# Port forward to SQL Server
kubectl port-forward svc/amrod-sqlserver 1433:1433 -n amrod

# Port forward to RabbitMQ Management UI
kubectl port-forward svc/amrod-rabbitmq 15672:15672 -n amrod
# Access: http://localhost:15672 (guest/guest)

# Access pod shell
kubectl exec -it deployment/amrod-api -n amrod -- /bin/bash
```

### Ingress Configuration

The Helm chart includes ingress resources for HTTP routing. Configure ingress domains in values:

```yaml
api:
  ingress:
    hosts:
      - host: api.amrod.local
        paths:
          - path: /
            pathType: Prefix

client:
  ingress:
    hosts:
      - host: amrod.local
        paths:
          - path: /
            pathType: Prefix
```

For HTTPS, configure cert-manager:

```bash
# Install cert-manager (if not installed)
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer for Let's Encrypt
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@amrod.local
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

### Scaling Deployments

```bash
# Manual scaling
kubectl scale deployment amrod-api --replicas=5 -n amrod

# View HPA status
kubectl get hpa -n amrod
kubectl describe hpa amrod-api -n amrod

# View autoscaling in action
kubectl get hpa amrod-api -n amrod --watch
```

### Monitoring and Logging

```bash
# View pod logs
kubectl logs deployment/amrod-api -n amrod -f

# View multiple pod logs
kubectl logs -f deployment/amrod-api -n amrod --all-containers=true

# Describe deployment
kubectl describe deployment amrod-api -n amrod

# Get pod events
kubectl get events -n amrod --sort-by='.lastTimestamp'

# Resource usage
kubectl top nodes
kubectl top pods -n amrod
```

### Troubleshooting Kubernetes Deployment

```bash
# Check pod status
kubectl get pods -n amrod -o wide

# Get pod details
kubectl describe pod [pod-name] -n amrod

# View pod logs for errors
kubectl logs [pod-name] -n amrod
kubectl logs [pod-name] -n amrod --previous  # Previous crashed pod logs

# Check service endpoints
kubectl get endpoints -n amrod
kubectl get svc -n amrod -o wide

# Debug connectivity
kubectl run -it --image=busybox:1.28 debug --restart=Never -n amrod -- sh
# Inside pod: nslookup amrod-api, ping amrod-sqlserver, etc.
```

---

## Configuration

### Environment Variables

#### API (Server)

| Variable                               | Default       | Description                  |
| -------------------------------------- | ------------- | ---------------------------- |
| ASPNETCORE_ENVIRONMENT                 | Production    | Deployment environment       |
| ASPNETCORE_URLS                        | http://+:5063 | Server listen URL            |
| ConnectionStrings\_\_DefaultConnection | -             | SQL Server connection string |
| RabbitMq\_\_HostName                   | localhost     | RabbitMQ host                |
| RabbitMq\_\_UserName                   | guest         | RabbitMQ username            |
| RabbitMq\_\_Password                   | guest         | RabbitMQ password            |

#### Client (UI)

| Variable     | Default               | Description     |
| ------------ | --------------------- | --------------- |
| VITE_API_URL | http://localhost:5063 | API backend URL |

### Database Configuration

For custom database setup, modify the connection string:

```
Server=[hostname];Database=OrderManagement;User Id=sa;Password=[password];TrustServerCertificate=True;
```

### Security Recommendations

1. **Change Default Passwords**:
   - SQL Server SA password
   - RabbitMQ credentials
   - Update in Dockerfile and Helm values

2. **Use Secrets Management**:
   - Kubernetes Secrets for sensitive data
   - Docker secret mounting
   - External vault systems

3. **Enable HTTPS**:
   - Use cert-manager with Let's Encrypt
   - Configure ingress TLS

4. **Network Policies**:
   ```bash
   kubectl apply -f - <<EOF
   apiVersion: networking.k8s.io/v1
   kind: NetworkPolicy
   metadata:
     name: amrod-network-policy
     namespace: amrod
   spec:
     podSelector: {}
     policyTypes:
     - Ingress
     - Egress
     ingress:
     - from:
       - namespaceSelector:
           matchLabels:
             name: amrod
     egress:
     - to:
       - namespaceSelector:
           matchLabels:
             name: amrod
   EOF
   ```

---

## Troubleshooting

### Docker Compose Issues

**Service fails to start**:

```bash
# Check logs
docker-compose logs [service_name]

# Verify all services are healthy
docker-compose ps

# Check port conflicts
lsof -i :[port]

# Restart service
docker-compose restart [service_name]
```

**Database connection errors**:

```bash
# Verify SQL Server is running
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT @@VERSION'

# Check connection string
docker-compose logs api | grep -i connection
```

**RabbitMQ issues**:

```bash
# Access RabbitMQ management UI
# http://localhost:15672 (guest/guest)

# Check RabbitMQ status
docker-compose exec rabbitmq rabbitmq-diagnostics status
```

### Kubernetes Issues

**Pods not starting**:

```bash
kubectl describe pod [pod-name] -n amrod
kubectl logs [pod-name] -n amrod
kubectl get events -n amrod --sort-by='.lastTimestamp'
```

**Service not accessible**:

```bash
# Check service exists
kubectl get svc -n amrod

# Check endpoints
kubectl get endpoints -n amrod

# Test DNS resolution
kubectl run -it --image=busybox test --restart=Never -- nslookup amrod-api.amrod.svc.cluster.local
```

**Database connection issues**:

```bash
# Verify secrets
kubectl get secrets -n amrod
kubectl get secret amrod-db-secret -n amrod -o yaml

# Check pod environment variables
kubectl exec -it deployment/amrod-api -n amrod -- env | grep -i connection
```

### Performance Tuning

**Docker Compose**:

```bash
# Check resource usage
docker stats

# Increase resource limits
docker update --memory 2g --cpus 2 amrod-api
```

**Kubernetes**:

```bash
# Monitor node resources
kubectl top nodes

# Monitor pod resources
kubectl top pods -n amrod

# Adjust resource requests/limits in values.yaml
helm upgrade amrod ./amrod -n amrod --set api.resources.requests.memory=512Mi
```

---

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Helm Documentation](https://helm.sh/docs/)
- [AMROD README](../README.md)

## Support

For issues or questions:

1. Check logs: `docker-compose logs` or `kubectl logs`
2. Verify configuration in values.yaml or docker-compose.yml
3. Consult the troubleshooting section above
4. Contact the AMROD development team
