# 🚀 OKLA Chatbot LLM — Production Deployment Runbook

> **Version:** 2.0  
> **Last Updated:** February 17, 2026  
> **Owner:** MLOps / Cloud AI Team  
> **Target:** Digital Ocean Kubernetes (DOKS) — `okla-cluster`, namespace `okla`

---

## 📋 Pre-Deployment Checklist

### 1. Model Artifacts ✅

| Item                                        | Status | Command to Verify                                               |
| ------------------------------------------- | ------ | --------------------------------------------------------------- |
| GGUF file trained                           | ☐      | `ls -la models/okla-llama3-8b-q4_k_m.gguf`                      |
| SHA256 recorded in model-registry.json      | ☐      | `sha256sum models/*.gguf`                                       |
| Eval gate PASSED (all thresholds met)       | ☐      | `python evaluate_before_deploy.py`                              |
| model-registry.json `go_nogo_result` = `GO` | ☐      | `jq '.models[0].evaluation.go_nogo_result' model-registry.json` |
| GGUF uploaded to HuggingFace                | ☐      | Check `gregorymorenoiem/okla-chatbot-llama3-8b`                 |

### 2. Infrastructure ✅

| Item                        | Status | Command to Verify                                       |
| --------------------------- | ------ | ------------------------------------------------------- |
| DOKS cluster accessible     | ☐      | `doctl kubernetes cluster kubeconfig save okla-cluster` |
| Namespace `okla` exists     | ☐      | `kubectl get namespace okla`                            |
| PostgreSQL running          | ☐      | `kubectl get pods -n okla -l app=postgres`              |
| Redis running               | ☐      | `kubectl get pods -n okla -l app=redis`                 |
| RabbitMQ running            | ☐      | `kubectl get pods -n okla -l app=rabbitmq`              |
| Secrets configured          | ☐      | `kubectl get secret okla-secrets -n okla`               |
| PVC storage class available | ☐      | `kubectl get sc do-block-storage`                       |

### 3. Docker Images ✅

| Item                                          | Status | Command to Verify                                               |
| --------------------------------------------- | ------ | --------------------------------------------------------------- |
| `ghcr.io/okla-rd/chatbotservice:latest` built | ☐      | `docker manifest inspect ghcr.io/okla-rd/chatbotservice:latest` |
| `ghcr.io/okla-rd/llm-server:latest` built     | ☐      | `docker manifest inspect ghcr.io/okla-rd/llm-server:latest`     |
| Images scanned with Trivy (0 critical)        | ☐      | Check CI/CD output                                              |

---

## 🔧 Deployment Steps

### Step 1: Upload GGUF Model to PVC (One-Time)

The GGUF model file must be available in the PVC before the LLM server can start.

```bash
# Option A: Use a temporary pod to download from HuggingFace
kubectl run model-downloader \
  --image=python:3.11-slim \
  --restart=Never \
  --namespace=okla \
  --overrides='{
    "spec": {
      "containers": [{
        "name": "downloader",
        "image": "python:3.11-slim",
        "command": ["/bin/bash", "-c",
          "pip install huggingface-hub && python -c \"from huggingface_hub import hf_hub_download; hf_hub_download(repo_id='\''gregorymorenoiem/okla-chatbot-llama3-8b'\'', filename='\''okla-llama3-8b-q4_k_m.gguf'\'', local_dir='\''/models'\'')\" && ls -la /models/"
        ],
        "volumeMounts": [{"name": "model-vol", "mountPath": "/models"}]
      }],
      "volumes": [{"name": "model-vol", "persistentVolumeClaim": {"claimName": "llm-model-pvc"}}]
    }
  }' \
  -- /bin/bash -c "sleep 3600"

# Wait for download to complete (check logs)
kubectl logs -f model-downloader -n okla

# Clean up
kubectl delete pod model-downloader -n okla
```

```bash
# Option B: Copy from local machine
kubectl cp ./models/okla-llama3-8b-q4_k_m.gguf okla/model-downloader:/models/
```

### Step 2: Apply K8s Manifests

```bash
# 2.1 — Update Gateway ConfigMap (includes chatbot routes)
kubectl delete configmap gateway-config -n okla 2>/dev/null || true
kubectl apply -f k8s/configmaps.yaml -n okla

# 2.2 — Restart Gateway to pick up new routes
kubectl rollout restart deployment/gateway -n okla
kubectl rollout status deployment/gateway -n okla --timeout=120s

# 2.3 — Deploy ChatbotService + LLM Server
kubectl apply -f k8s/chatbotservice.yaml -n okla

# 2.4 — Wait for LLM Server to load model (up to 5 min)
echo "Waiting for LLM server to load model..."
kubectl rollout status deployment/llm-server -n okla --timeout=600s

# 2.5 — Wait for ChatbotService
kubectl rollout status deployment/chatbotservice -n okla --timeout=120s
```

### Step 3: Apply Monitoring & MLOps

```bash
# 3.1 — Prometheus alert rules
kubectl apply -f k8s/prometheus-rules-chatbot.yaml -n okla

# 3.2 — HPA (includes chatbot + LLM entries)
kubectl apply -f k8s/hpa.yaml -n okla

# 3.3 — MLOps CronJobs (drift detection + retrain collector)
kubectl apply -f k8s/mlops-cronjobs.yaml -n okla
```

### Step 4: Smoke Test

```bash
# 4.1 — LLM Server health
kubectl port-forward svc/llm-server 8000:8000 -n okla &
sleep 3
curl -s http://localhost:8000/health | python -m json.tool

# Expected output:
# {
#   "status": "healthy",
#   "model_loaded": true,
#   "model_name": "okla-llama3-8b-q4_k_m.gguf",
#   ...
# }

# 4.2 — ChatbotService health
kubectl port-forward svc/chatbotservice 8081:8080 -n okla &
sleep 3
curl -s http://localhost:8081/health

# 4.3 — End-to-end inference test
curl -s -X POST http://localhost:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "okla-llama3-8b",
    "messages": [
      {"role": "system", "content": "Eres OKLA Bot. Responde en JSON con campos: response, intent, confidence, isFallback, parameters, leadSignals, suggestedAction, quickReplies."},
      {"role": "user", "content": "Hola, cuánto cuesta este carro?"}
    ],
    "temperature": 0.3,
    "max_tokens": 400
  }' | python -m json.tool

# 4.4 — Gateway routing test (from outside cluster)
curl -s https://okla.com.do/api/chat/health

# Clean up port-forwards
kill %1 %2 2>/dev/null
```

### Step 5: Verify Monitoring

```bash
# 5.1 — Check HPA status
kubectl get hpa -n okla | grep -E "chatbot|llm"

# 5.2 — Check Prometheus rules loaded
kubectl get prometheusrule -n okla

# 5.3 — Check CronJobs scheduled
kubectl get cronjob -n okla | grep mlops

# 5.4 — Check LLM metrics endpoint
kubectl port-forward svc/llm-server 8000:8000 -n okla &
sleep 2
curl -s http://localhost:8000/metrics | head -30
kill %1
```

---

## 🔄 Rollback Procedure

**Max rollback time: ~10 minutes**

```bash
# 1. Rollback LLM Server to previous version
kubectl rollout undo deployment/llm-server -n okla

# 2. Rollback ChatbotService
kubectl rollout undo deployment/chatbotservice -n okla

# 3. Wait for rollback
kubectl rollout status deployment/llm-server -n okla --timeout=600s
kubectl rollout status deployment/chatbotservice -n okla --timeout=120s

# 4. Verify health
kubectl port-forward svc/llm-server 8000:8000 -n okla &
sleep 3
curl -s http://localhost:8000/health
kill %1

# 5. Update model-registry.json
# Set current version status to "rolled-back"
# Set previous version status to "production"
```

---

## 📊 Post-Deployment Monitoring (First 24h)

### Watch Commands

```bash
# Real-time pod status
watch -n 5 'kubectl get pods -n okla -l "app in (chatbotservice,llm-server)" -o wide'

# LLM Server logs (live)
kubectl logs -f deployment/llm-server -n okla --tail=50

# ChatbotService logs (live)
kubectl logs -f deployment/chatbotservice -n okla --tail=50

# HPA scaling events
kubectl get events -n okla --field-selector reason=SuccessfulRescale --sort-by='.lastTimestamp'
```

### Key Metrics to Monitor (First 24h)

| Metric          | Healthy | Warning   | Critical         |
| --------------- | ------- | --------- | ---------------- |
| LLM P95 latency | < 30s   | 30-60s    | > 60s            |
| LLM error rate  | < 5%    | 5-10%     | > 10%            |
| Memory usage    | < 75%   | 75-85%    | > 85%            |
| Fallback rate   | < 10%   | 10-15%    | > 15%            |
| Avg confidence  | > 0.75  | 0.65-0.75 | < 0.65           |
| Requests/day    | < 200   | 200-300   | > 300 (plan GPU) |

---

## 🏗️ Architecture Reference

```
                    ┌─────────────┐
                    │   Ingress   │
                    │ okla.com.do │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ frontend-web│
                    │  (Next.js)  │
                    └──────┬──────┘
                           │ /api/* rewrite
                    ┌──────▼──────┐
                    │   Gateway   │
                    │  (Ocelot)   │
                    └──────┬──────┘
                           │ /api/chat/*
                    ┌──────▼──────┐          ┌──────────────┐
                    │ ChatbotSvc  │───HTTP───▶│  LLM Server  │
                    │  (.NET 8)   │          │ (llama.cpp)  │
                    │  port:8080  │          │  port:8000   │
                    └──────┬──────┘          └──────┬───────┘
                           │                        │
                    ┌──────▼──────┐          ┌──────▼───────┐
                    │  PostgreSQL │          │   PVC 10Gi   │
                    │  (chatbot)  │          │  GGUF model  │
                    └─────────────┘          └──────────────┘
```

---

## 📞 Escalation Matrix

| Severity                    | Response Time     | Who           | Contact                  |
| --------------------------- | ----------------- | ------------- | ------------------------ |
| 🔴 Critical (service down)  | < 15 min          | On-call MLOps | Slack #okla-mlops-alerts |
| 🟠 Warning (degradation)    | < 1h              | MLOps team    | Slack #okla-chatbot      |
| 🔵 Info (capacity planning) | Next business day | MLOps + Cloud | Slack #okla-infra        |

---

_Runbook maintained by MLOps / Cloud AI — February 2026_
