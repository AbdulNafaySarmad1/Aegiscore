# AegisCore — Distribution & Deployment Strategy (U12)
## App Stores, Web, Kubernetes (Cloud & Bare-Metal)

## Role
You are a platform architect + DevOps engineer designing AegisCore's complete distribution and deployment strategy:

1. **iOS App Store** — Submit AegisCore mobile app
2. **Google Play Store** — Submit AegisCore mobile app
3. **Web Deployment** — University websites hosting
4. **Kubernetes (Cloud)** — Azure Singapore option
5. **Kubernetes (On-Prem)** — Bare-metal server option for universities that refuse cloud

---

## Part A — iOS App Store Submission

### Pre-Submission Checklist (4 weeks before launch)

#### 1. Developer Account Setup
```
Apple Developer Program ($99/year)
├─ Create Apple ID
├─ Enroll in Apple Developer Program
├─ Accept agreements
├─ Activate certificates
└─ Create app bundle identifiers
    ├─ com.aegiscore.student (student app)
    └─ com.aegiscore.faculty (faculty app)
```

#### 2. Code Signing Setup

```bash
# Generate Certificate Signing Request (CSR)
# On Mac: Keychain Access → Certificate Assistant → Request Certificate

# Upload to Apple Developer Portal
# → Certificates → Create new iOS App Development certificate
# → Download certificate
# → Import into Keychain

# Create provisioning profiles
# → Devices: Add all test devices (UDID)
# → Provisioning Profiles: Create development + distribution
# → Download both profiles
```

#### 3. App Configuration

```javascript
// app.json (Expo)
{
  "expo": {
    "name": "AegisCore Student",
    "slug": "aegiscore-student",
    "version": "1.0.0",
    "orientation": "portrait",
    "icon": "./assets/app-icon.png",
    "userInterfaceStyle": "light",
    "splash": {
      "image": "./assets/splash.png",
      "resizeMode": "contain",
      "backgroundColor": "#ffffff"
    },
    "plugins": [
      ["expo-notifications"],
      ["expo-camera"],
      ["expo-contacts"],
      ["expo-location"],
      ["expo-local-authentication"] // Biometric auth
    ],
    "ios": {
      "supportsTabletMode": true,
      "bundleIdentifier": "com.aegiscore.student",
      "buildNumber": "1",
      "infoPlist": {
        "NSCameraUsageDescription": "Allow AegisCore to use camera for biometric authentication",
        "NSLocationWhenInUseUsageDescription": "Allow AegisCore to access your location for campus services",
        "NSFaceIDUsageDescription": "Use Face ID to unlock AegisCore",
        "NSBiometricsUsageDescription": "Use Touch ID to unlock AegisCore"
      }
    },
    "android": {
      "bundleIdentifier": "com.aegiscore.student",
      "versionCode": 1
    }
  }
}
```

#### 4. App Screenshots & Metadata

**Screenshots required:**
- 5 screenshots (iPad 12.9" gen 5)
- 5 screenshots (iPhone 6.7")
- 1 app preview video (15-30 seconds)

**Content to show:**
- Login screen
- Student dashboard
- Attendance view
- Grades/marks
- Fee payment integration

**Metadata for App Store:**
```
App Name: AegisCore Student
Subtitle: University Management at Your Fingertips
Description:
  "AegisCore Student connects you to your university instantly.
   - Check attendance & grades
   - Pay fees via JazzCash/EasyPaisa
   - Access courses & materials
   - Get notifications
   - View timetable
   - Download transcripts"

Keywords: university, education, student, management, erp, pakistan

Category: Education

Age Rating: 4+

Content Rights: Original
```

#### 5. Privacy Policy & Terms

**Must include:**
```
Privacy Policy (on website: aegiscore.pk/privacy)
├─ Data we collect (name, email, student ID, grades, attendance)
├─ How we use data (display in app, analytics)
├─ Data storage (PostgreSQL, encrypted at rest)
├─ Data retention (kept until student graduates + 1 year)
├─ Third parties (JazzCash for payments, notification services)
├─ User rights (export, delete, access)
└─ Security measures (AES-256, HTTPS, TLS)

Terms of Service (on website: aegiscore.pk/terms)
├─ Acceptable use
├─ Account responsibility
├─ Content ownership
├─ Limitation of liability
└─ Dispute resolution
```

#### 6. Build for App Store

```bash
# Using Expo (easiest)
eas build --platform ios --auto-submit

# Or manual approach:
# 1. Build on Xcode
# 2. Create App Store Connect record
# 3. Upload via Transporter
```

#### 7. App Store Connect Submission

```
1. Create App Record
   └─ App Name: AegisCore Student
   └─ Bundle ID: com.aegiscore.student
   └─ SKU: AEGISCORE-STU-001
   └─ User Access: Students

2. Pricing
   └─ Free (funded by university licensing)

3. Version Release
   └─ Version: 1.0.0
   └─ Build: Select build from Xcode Cloud
   └─ Marketing URL: aegiscore.pk/ios
   └─ Support URL: support.aegiscore.pk

4. App Information
   └─ Privacy Policy: https://aegiscore.pk/privacy
   └─ Category: Education
   └─ Age Rating: 4+
   └─ Copyright: © 2024 AegisCore Systems

5. App Preview & Screenshots
   └─ 5 iPad screenshots (2732 x 2048)
   └─ 5 iPhone screenshots (1284 x 2778)
   └─ 1 preview video (max 30 sec, MP4)

6. General App Information
   └─ Subtitle: "University Management at Your Fingertips"
   └─ Description: [See above]
   └─ Keywords: university, student, grades, attendance
   └─ Support URL: support.aegiscore.pk
   └─ Marketing URL: aegiscore.pk
   └─ Privacy Policy: aegiscore.pk/privacy

7. Review Notes (for Apple reviewer)
   "AegisCore Student is a university management app for Pakistani
    universities. Students check grades, attendance, pay fees,
    and access course materials. Demo account:
    user@demo.aegiscore.pk / demo123456
    (accounts are university-specific)"

8. Submit for Review
   └─ Automatic review time: 24-48 hours
```

**Common rejection reasons (avoid):**
- ❌ Crashes on launch (test thoroughly!)
- ❌ Links to external payment (use in-app purchases)
- ❌ Empty or placeholder content
- ❌ Login required without way to test (provide demo account)
- ❌ Misleading screenshots/videos
- ❌ Battery drain issues

**After approval:**
- ✅ App goes live on App Store
- ✅ Students can download immediately
- ✅ You can push updates (review time applies)

---

## Part B — Google Play Store Submission

### Pre-Submission (Similar to iOS, but easier)

#### 1. Developer Account Setup
```
Google Play Developer Account ($25 one-time)
├─ Create Google Account
├─ Enroll in Google Play
├─ Set up billing
├─ Create app bundle ID: com.aegiscore.student
└─ Accept Play Developer policies
```

#### 2. App Signing (Google handles for you)

```bash
# Google Play signs your app automatically
# You just upload the AAB (Android App Bundle)

# Generate signing certificate (if doing manual)
# keytool -genkey -v -keystore aegiscore.keystore -alias aegiscore -keyalg RSA -keysize 2048 -validity 10000

# Build AAB (Android App Bundle)
# Using React Native + EAS:
eas build --platform android --auto-submit

# Result: .aab file ready for Play Store
```

#### 3. Google Play Console Setup

```
1. Create App Entry
   └─ App Name: AegisCore Student
   └─ Package Name: com.aegiscore.student
   └─ App Category: Education
   └─ Ads: No

2. App Access
   └─ Type: Default (students download from store)

3. Content Rating
   └─ Questionnaire:
      ├─ Violence: None
      ├─ Sexual content: None
      ├─ Profanity: None
      ├─ Alcohol/Drugs: None
      ├─ Downloads/Uploads User-Generated Content: Yes (assignments)
      └─ Personally Identifiable Information: Yes (student ID, grades)
   └─ Rating: Moderate Maturity

4. Target Audience
   └─ Audience: Age 13+ (young adults, college students)

5. Screenshots (min 2, max 8)
   └─ Landscape: 1280 x 720
   └─ Or portrait: 1080 x 1920
   └─ Show 4-5 key screens

6. Feature Graphic
   └─ 1024 x 500 PNG
   └─ Show app name + key feature

7. App Description
   "AegisCore Student: Your university in your pocket.
    • Check real-time attendance
    • View grades and transcripts
    • Pay fees via JazzCash/EasyPaisa
    • Download course materials
    • Get push notifications
    • Access timetable & announcements"

8. Short Description (80 chars)
   "University management app for students"

9. Privacy Policy
   └─ URL: aegiscore.pk/privacy (REQUIRED)

10. Release Notes
    "Initial release - complete student management"
```

#### 4. Prepare Build

```bash
# Using Expo + EAS
eas build --platform android

# Output: aegiscore-student.aab (Android App Bundle)
# Size: ~50-70 MB (will be ~30 MB after compression on device)
```

#### 5. Internal Testing Track

```
Before submitting to production:

1. Internal Testing
   └─ Upload AAB
   └─ Add test users
   └─ Test on multiple devices
   └─ Check: crashes, performance, network issues

2. Closed Testing (Optional)
   └─ Invite 10-20 university staff
   └─ Gather feedback before public launch

3. Open Testing (Optional)
   └─ Roll out to limited audience first
   └─ Fix issues before 100% rollout
```

#### 6. Production Submission

```
1. Upload Build
   └─ Upload aegiscore-student.aab
   └─ Build is automatically tested
   └─ Optimized for all screen sizes

2. Review Time
   └─ Usually: 2-4 hours
   └─ Rarely: Up to 48 hours
   └─ Google is faster than Apple

3. Launch
   └─ Set release date
   └─ Roll out gradually (5% → 25% → 100%)
   └─ Monitor crashes + ratings
```

**Common rejection reasons (avoid):**
- ❌ Permissions not justified (don't ask for camera if not using)
- ❌ Crashes/ANR (Application Not Responding)
- ❌ Misleading ads or description
- ❌ Requires payment without in-app billing
- ❌ Intellectual property violations

---

## Part C — Web Deployment

### Architecture

```
Domain: aegiscore.university.edu.pk
  ├─ DNS → Cloudflare (DDoS protection)
  ├─ Frontend: aegiscore.university.edu.pk/
  │   └─ Static React/Next.js (Cloudflare Pages or Vercel)
  │
  ├─ API: api.aegiscore.university.edu.pk
  │   └─ Kubernetes service (Azure or bare-metal)
  │
  └─ Admin: admin.aegiscore.university.edu.pk
      └─ Same Kubernetes backend
```

### Cloudflare Setup

```javascript
// cloudflare.json (configuration)
{
  "account_id": "YOUR_ACCOUNT_ID",
  "api_token": "YOUR_API_TOKEN",
  "zone": "university.edu.pk",
  
  "dns": [
    {
      "type": "A",
      "name": "aegiscore",
      "content": "YOUR_SERVER_IP"
    },
    {
      "type": "CNAME",
      "name": "api.aegiscore",
      "content": "aegiscore.university.edu.pk"
    }
  ],
  
  "ssl": {
    "ssl_default_certificate": true,
    "always_use_https": true,
    "min_tls_version": "1.3"
  },
  
  "rules": [
    {
      "rule": "api.*",
      "cache_level": "cache_everything",
      "cache_ttl": 600
    }
  ]
}
```

### Frontend Deployment (Vercel or Cloudflare Pages)

```bash
# Option 1: Vercel (recommended for React/Next.js)
npm i -g vercel
vercel --prod --name aegiscore-student

# Creates:
# aegiscore-student.vercel.app (staging)
# aegiscore.university.edu.pk (production via custom domain)

# Option 2: Cloudflare Pages (free, fast)
wrangler pages publish dist

# Automatically deploys on git push
# Creates preview + production deployments
```

**Vercel config:**
```javascript
// vercel.json
{
  "buildCommand": "npm run build",
  "outputDirectory": "out",
  "env": {
    "NEXT_PUBLIC_API_URL": "https://api.aegiscore.university.edu.pk",
    "NEXT_PUBLIC_UNIVERSITY": "SSUET"
  },
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://api.aegiscore.university.edu.pk/:path*"
    }
  ]
}
```

---

## Part D — Kubernetes Deployment (Azure Singapore)

### Scenario: University wants to host on Azure Singapore

#### 1. Azure Setup

```bash
# Login to Azure
az login

# Create resource group in Singapore
az group create \
  --name aegiscore-rg \
  --location southeastasia

# Create AKS cluster (Singapore region)
az aks create \
  --resource-group aegiscore-rg \
  --name aegiscore-cluster \
  --node-count 3 \
  --vm-set-type VirtualMachineScaleSets \
  --node-vm-size Standard_B4ms \
  --enable-managed-identity \
  --network-plugin azure \
  --location southeastasia

# Get credentials
az aks get-credentials \
  --resource-group aegiscore-rg \
  --name aegiscore-cluster
```

#### 2. Container Registry (Azure Container Registry)

```bash
# Create ACR
az acr create \
  --resource-group aegiscore-rg \
  --name aegiscorepk \
  --sku Basic

# Build and push image
az acr build \
  --registry aegiscorepk \
  --image aegiscore-api:latest .

# Result: aegiscorepk.azurecr.io/aegiscore-api:latest
```

#### 3. Kubernetes Manifests

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: aegiscore
---

# secret.yaml (database credentials)
apiVersion: v1
kind: Secret
metadata:
  name: aegiscore-secrets
  namespace: aegiscore
type: Opaque
data:
  DATABASE_URL: cG9zdGdyZXM6Ly9ub2N0dXJuZTpwYXNzd29yZEBkYi5ub2N0dXJuZS5lZHUucGs6NTQzMi9ub2N0dXJuZQ==
  REDIS_URL: cmVkaXM6Ly9yZWRpcy5ub2N0dXJuZS5lZHUucGs6NjM3OQ==
---

# configmap.yaml (configuration)
apiVersion: v1
kind: ConfigMap
metadata:
  name: aegiscore-config
  namespace: aegiscore
data:
  ASPNETCORE_ENVIRONMENT: Production
  AZURE_REGION: southeastasia
  REPLICATION_LAG_HOURS: "12"
---

# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aegiscore-api
  namespace: aegiscore
spec:
  replicas: 3 # High availability
  selector:
    matchLabels:
      app: aegiscore-api
  template:
    metadata:
      labels:
        app: aegiscore-api
    spec:
      containers:
      - name: aegiscore-api
        image: aegiscorepk.azurecr.io/aegiscore-api:latest
        imagePullPolicy: Always
        
        ports:
        - containerPort: 5000
          name: http
        
        env:
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: aegiscore-secrets
              key: DATABASE_URL
        - name: REDIS_URL
          valueFrom:
            secretKeyRef:
              name: aegiscore-secrets
              key: REDIS_URL
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: aegiscore-config
              key: ASPNETCORE_ENVIRONMENT
        
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        
        readinessProbe:
          httpGet:
            path: /ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
        
        resources:
          requests:
            cpu: 250m
            memory: 512Mi
          limits:
            cpu: 500m
            memory: 1Gi
      
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 100
            podAffinityTerm:
              labelSelector:
                matchExpressions:
                - key: app
                  operator: In
                  values:
                  - aegiscore-api
              topologyKey: kubernetes.io/hostname
---

# service.yaml
apiVersion: v1
kind: Service
metadata:
  name: aegiscore-api
  namespace: aegiscore
spec:
  type: LoadBalancer
  selector:
    app: aegiscore-api
  ports:
  - port: 80
    targetPort: 5000
    protocol: TCP
---

# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: aegiscore-ingress
  namespace: aegiscore
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.aegiscore.university.edu.pk
    secretName: aegiscore-tls
  rules:
  - host: api.aegiscore.university.edu.pk
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: aegiscore-api
            port:
              number: 80
---

# hpa.yaml (Auto-scaling)
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: aegiscore-api-hpa
  namespace: aegiscore
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: aegiscore-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

#### 4. Deploy to AKS

```bash
# Apply all manifests
kubectl apply -f namespace.yaml
kubectl apply -f secret.yaml
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml
kubectl apply -f hpa.yaml

# Verify deployment
kubectl get pods -n aegiscore
kubectl get svc -n aegiscore
kubectl get ingress -n aegiscore

# Check logs
kubectl logs -n aegiscore deployment/aegiscore-api

# Get load balancer IP
kubectl get svc -n aegiscore aegiscore-api
# → Copy EXTERNAL-IP
# → Point DNS A record to this IP
```

---

## Part E — Kubernetes Deployment (On-Premises Bare-Metal)

### Scenario: University says "No cloud, we need data on our servers"

#### 1. Infrastructure Requirements

```
Minimum Setup:
├─ 3 bare-metal servers (high availability)
│  ├─ CPU: 4+ cores per server
│  ├─ RAM: 16 GB+ per server
│  ├─ Storage: 500 GB+ SSD per server
│  └─ OS: Ubuntu 22.04 LTS
│
├─ Network:
│  ├─ Dedicated VLAN for AegisCore
│  ├─ Static IPs for all servers
│  ├─ Load balancer (hardware or Keepalived)
│  └─ Firewall rules (only HTTP/HTTPS/SSH allowed)
│
├─ Storage:
│  ├─ PostgreSQL: 1 TB+ NAS (for backups)
│  └─ Redis: Local SSD on master node
│
└─ Backup:
   ├─ Daily snapshots
   ├─ Off-site backup (university choice)
   └─ 30-day retention
```

#### 2. Install Kubernetes (kubeadm)

```bash
# On all 3 servers:

# 1. Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# 2. Install kubeadm, kubelet, kubectl
curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.28/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.28/deb /' | sudo tee /etc/apt/sources.list.d/kubernetes.list

sudo apt update
sudo apt install -y kubeadm kubelet kubectl

# 3. Disable swap (Kubernetes requirement)
sudo swapoff -a
sudo sed -i '/ swap / s/^/#/' /etc/fstab

# 4. Configure kernel networking
sudo modprobe overlay
sudo modprobe br_netfilter

sudo tee /etc/sysctl.d/kubernetes.conf > /dev/null <<EOF
net.bridge.bridge-nf-call-iptables = 1
net.bridge.bridge-nf-call-ip6tables = 1
net.ipv4.ip_forward = 1
EOF

sudo sysctl --system
```

#### 3. Initialize Master Node

```bash
# On first server (master):
sudo kubeadm init \
  --pod-network-cidr=10.244.0.0/16 \
  --service-cidr=10.96.0.0/12 \
  --control-plane-endpoint=aegiscore-control.university.edu.pk \
  --apiserver-advertise-address=192.168.1.10

# Save the output! You'll need the join command for worker nodes
# Example output (KEEP THIS):
# kubeadm join 192.168.1.10:6443 --token abc123... --discovery-token-ca-cert-hash sha256:...

# Configure kubectl
mkdir -p $HOME/.kube
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown $(id -u):$(id -g) $HOME/.kube/config

# Verify master is running
kubectl get nodes
```

#### 4. Install Pod Network (Flannel)

```bash
# On master node:
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml

# Wait for flannel to be ready
kubectl get pods -A
# coredns should change from Pending to Running
```

#### 5. Join Worker Nodes

```bash
# On each of the 2 worker nodes, run the join command from master init:
sudo kubeadm join 192.168.1.10:6443 \
  --token abc123xyz \
  --discovery-token-ca-cert-hash sha256:...

# Verify nodes joined
kubectl get nodes
# Should show: master, worker1, worker2 (all Ready)
```

#### 6. Install Ingress Controller

```bash
# Install Nginx Ingress
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.1/deploy/static/provider/cloud/deploy.yaml

# Wait for LoadBalancer to get an EXTERNAL-IP
kubectl get svc -n ingress-nginx ingress-nginx-controller

# Point university's firewall to this IP
# Forward port 80 + 443 to this service
```

#### 7. Install Cert-Manager (SSL Certificates)

```bash
# Install cert-manager
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
    email: admin@university.edu.pk
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

#### 8. Deploy AegisCore (Same manifests as Azure)

```bash
# Same YAML files as Azure deployment
kubectl apply -f namespace.yaml
kubectl apply -f secret.yaml
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml
kubectl apply -f hpa.yaml

# Verify
kubectl get all -n aegiscore
```

#### 9. Backup Strategy (On-Premises Critical)

```bash
# velero-values.yaml (for backup automation)
configuration:
  backupStorageLocation:
    provider: aws
    bucket: university-aegiscore-backups
    config:
      s3Url: https://minio.university.edu.pk # Self-hosted MinIO
  
  schedules:
    daily-backup:
      schedule: "0 2 * * *" # 2 AM daily
      template:
        ttl: "720h"
        includedNamespaces:
          - aegiscore
```

#### 10. Monitoring & Logging

```bash
# Install Prometheus + Grafana
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install prometheus prometheus-community/kube-prometheus-stack -n monitoring

# Install ELK Stack (Elasticsearch, Logstash, Kibana)
helm repo add elastic https://helm.elastic.co
helm install elasticsearch elastic/elasticsearch -n logging
helm install kibana elastic/kibana -n logging

# Monitor cluster health
kubectl top nodes
kubectl top pods -n aegiscore
```

---

## Part F — CI/CD Pipeline (GitHub Actions)

### Automated Testing + Deployment

```yaml
# .github/workflows/deploy.yml
name: Build, Test & Deploy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Install dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run tests
      run: dotnet test --no-build --verbosity normal
    
    - name: Security scan
      run: |
        dotnet tool install -g SharpLab.CLI
        dotnet sonarscanner begin /k:aegiscore /d:sonar.host.url=${{ secrets.SONAR_HOST }} /d:sonar.login=${{ secrets.SONAR_TOKEN }}
        dotnet build
        dotnet sonarscanner end /d:sonar.login=${{ secrets.SONAR_TOKEN }}

  build-docker:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Build Docker image
      run: |
        docker build -t aegiscore-api:${{ github.sha }} .
        docker tag aegiscore-api:${{ github.sha }} aegiscore-api:latest
    
    - name: Push to registry (Azure)
      run: |
        echo ${{ secrets.AZURE_REGISTRY_PASSWORD }} | docker login -u aegiscore --password-stdin aegiscorepk.azurecr.io
        docker push aegiscorepk.azurecr.io/aegiscore-api:${{ github.sha }}

  deploy-azure:
    needs: build-docker
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Deploy to AKS
      run: |
        az login --service-principal -u ${{ secrets.AZURE_CLIENT_ID }} -p ${{ secrets.AZURE_CLIENT_SECRET }} --tenant ${{ secrets.AZURE_TENANT_ID }}
        az aks get-credentials --resource-group aegiscore-rg --name aegiscore-cluster
        kubectl set image deployment/aegiscore-api aegiscore-api=aegiscorepk.azurecr.io/aegiscore-api:${{ github.sha }} -n aegiscore
        kubectl rollout status deployment/aegiscore-api -n aegiscore

  build-ios:
    needs: test
    runs-on: macos-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup EAS
      run: npm install -g eas-cli
    
    - name: Build for App Store
      run: eas build --platform ios --auto-submit
      env:
        EXPO_TOKEN: ${{ secrets.EXPO_TOKEN }}

  build-android:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup EAS
      run: npm install -g eas-cli
    
    - name: Build for Play Store
      run: eas build --platform android --auto-submit
      env:
        EXPO_TOKEN: ${{ secrets.EXPO_TOKEN }}

  notify:
    needs: [deploy-azure, build-ios, build-android]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Notify Slack
      run: |
        curl -X POST ${{ secrets.SLACK_WEBHOOK }} \
          -H 'Content-type: application/json' \
          -d '{
            "text": "AegisCore deployment successful!",
            "blocks": [
              {
                "type": "section",
                "text": {
                  "type": "mrkdwn",
                  "text": "✅ API deployed to AKS (Azure Singapore)\n✅ iOS app submitted to App Store\n✅ Android app submitted to Play Store"
                }
              }
            ]
          }'
```

---

## Deployment Checklist

### Pre-Launch (4 weeks)
- [ ] Create Apple Developer account + enroll
- [ ] Create Google Play Developer account
- [ ] Set up app signing certificates (iOS)
- [ ] Prepare screenshots + videos (5 per app store)
- [ ] Write privacy policy + terms
- [ ] Set up Azure account (or bare-metal servers)
- [ ] Configure DNS + SSL (Cloudflare)
- [ ] Build Kubernetes manifests

### 2 Weeks Before Launch
- [ ] Submit iOS app for review
- [ ] Build Android AAB
- [ ] Test on internal devices
- [ ] Deploy API to Kubernetes (staging)
- [ ] Test web app on staging domain

### 1 Week Before Launch
- [ ] iOS app approved (hopefully)
- [ ] Submit Android app for review
- [ ] Load test API (1000+ concurrent students)
- [ ] Prepare release notes + announcement

### Launch Day
- [ ] Release iOS app on App Store
- [ ] Release Android app on Play Store
- [ ] Go live on web: aegiscore.university.edu.pk
- [ ] Monitor for crashes + errors
- [ ] Respond to user feedback

### Post-Launch Monitoring
- [ ] App Store rating tracking
- [ ] Play Store rating tracking
- [ ] Kubernetes pod health
- [ ] Database replication lag
- [ ] Attendance offline queue status
- [ ] Payment gateway logs

---

**This is distribution at scale: 500 universities, millions of students, worldwide.**

iOS + Android + Web + Cloud + On-Premises. One codebase. One deployment pipeline. 🚀
