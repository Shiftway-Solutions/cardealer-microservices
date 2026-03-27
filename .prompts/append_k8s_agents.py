#!/usr/bin/env python3
"""Append RecoAgent and PricingAgent deployments to k8s/deployments.yaml"""

import os

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEPLOYMENTS_FILE = os.path.join(REPO_ROOT, "k8s", "deployments.yaml")

RECO_AGENT_DEPLOYMENT = """
---
# =============================================================================
# RECO AGENT (AI Vehicle Recommendations with Claude Sonnet 4.5)
# =============================================================================
apiVersion: apps/v1
kind: Deployment
metadata:
  name: recoagent
  namespace: okla
  labels:
    app: recoagent
    tier: backend
spec:
  replicas: 1
  selector:
    matchLabels:
      app: recoagent
  template:
    metadata:
      labels:
        app: recoagent
    spec:
      serviceAccountName: okla-backend
      automountServiceAccountToken: false
      topologySpreadConstraints:
        - maxSkew: 1
          topologyKey: kubernetes.io/hostname
          whenUnsatisfiable: ScheduleAnyway
          labelSelector:
            matchLabels:
              app: recoagent
      imagePullSecrets:
        - name: registry-credentials
      containers:
        - name: recoagent
          image: ghcr.io/gregorymorenoiem/recoagent:latest
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_URLS
              value: "http://+:8080"
            - name: Claude__ApiKey
              valueFrom:
                secretKeyRef:
                  name: claude-api-secret
                  key: CLAUDE_API_KEY
          envFrom:
            - configMapRef:
                name: global-config
            - secretRef:
                name: jwt-secrets
            - secretRef:
                name: redis-secrets
            - secretRef:
                name: rabbitmq-secrets
          resources:
            requests:
              memory: "128Mi"
              cpu: "50m"
            limits:
              memory: "256Mi"
              cpu: "200m"
          securityContext:
            runAsNonRoot: true
            runAsUser: 1000
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop:
                - ALL
          startupProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
            failureThreshold: 24
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            periodSeconds: 30
            timeoutSeconds: 5
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
          volumeMounts:
            - name: tmp
              mountPath: /tmp
      volumes:
        - name: tmp
          emptyDir: {}
"""

PRICING_AGENT_DEPLOYMENT = """---
# =============================================================================
# PRICING AGENT (AI Vehicle Price Estimation with Claude)
# =============================================================================
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pricingagent
  namespace: okla
  labels:
    app: pricingagent
    tier: backend
spec:
  replicas: 1
  selector:
    matchLabels:
      app: pricingagent
  template:
    metadata:
      labels:
        app: pricingagent
    spec:
      serviceAccountName: okla-backend
      automountServiceAccountToken: false
      topologySpreadConstraints:
        - maxSkew: 1
          topologyKey: kubernetes.io/hostname
          whenUnsatisfiable: ScheduleAnyway
          labelSelector:
            matchLabels:
              app: pricingagent
      imagePullSecrets:
        - name: registry-credentials
      containers:
        - name: pricingagent
          image: ghcr.io/gregorymorenoiem/pricingagent:latest
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_URLS
              value: "http://+:8080"
            - name: LlmGateway__Claude__ApiKey
              valueFrom:
                secretKeyRef:
                  name: claude-api-secret
                  key: CLAUDE_API_KEY
            - name: LlmGateway__Claude__Enabled
              value: "true"
          envFrom:
            - configMapRef:
                name: global-config
            - secretRef:
                name: jwt-secrets
            - secretRef:
                name: redis-secrets
          resources:
            requests:
              memory: "128Mi"
              cpu: "50m"
            limits:
              memory: "256Mi"
              cpu: "200m"
          securityContext:
            runAsNonRoot: true
            runAsUser: 1000
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop:
                - ALL
          startupProbe:
            httpGet:
              path: /api/pricing-agent/health
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
            failureThreshold: 24
          livenessProbe:
            httpGet:
              path: /api/pricing-agent/health
              port: 8080
            periodSeconds: 30
            timeoutSeconds: 5
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /api/pricing-agent/health
              port: 8080
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
          volumeMounts:
            - name: tmp
              mountPath: /tmp
      volumes:
        - name: tmp
          emptyDir: {}
"""

# Check if deployments already added (idempotent)
with open(DEPLOYMENTS_FILE, "r") as f:
    existing = f.read()

if "name: recoagent" in existing:
    print("RecoAgent deployment already exists — skipping")
else:
    with open(DEPLOYMENTS_FILE, "a") as f:
        f.write(RECO_AGENT_DEPLOYMENT)
    print("RecoAgent deployment appended")

if "name: pricingagent" in existing:
    print("PricingAgent deployment already exists — skipping")
else:
    with open(DEPLOYMENTS_FILE, "a") as f:
        f.write(PRICING_AGENT_DEPLOYMENT)
    print("PricingAgent deployment appended")

import subprocess
result = subprocess.run(["wc", "-l", DEPLOYMENTS_FILE], capture_output=True, text=True)
print(f"Total lines: {result.stdout.strip()}")
