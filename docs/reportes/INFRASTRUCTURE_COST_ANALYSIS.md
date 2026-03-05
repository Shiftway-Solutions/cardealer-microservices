# 💰 OKLA Platform — Infrastructure Cost Analysis (DigitalOcean)

**Date:** 2026-03-05  
**Environment:** Production (DOKS — DigitalOcean Kubernetes Service)  
**Analyst:** GitHub Copilot — Automated Audit

---

## 📊 Current Infrastructure Inventory

### Kubernetes Cluster (DOKS)

| Resource               | Details                         | Monthly Cost (USD) |
| ---------------------- | ------------------------------- | ------------------ |
| **DOKS Control Plane** | Managed K8s v1.34.1             | $0 (free)          |
| **Worker Node 1**      | 4 vCPU / 8 GB RAM (s-4vcpu-8gb) | ~$48               |
| **Worker Node 2**      | 4 vCPU / 8 GB RAM (s-4vcpu-8gb) | ~$48               |
| **Subtotal Compute**   | 2 nodes × $48                   | **$96**            |

### Managed Database (PostgreSQL)

| Resource              | Details                                     | Monthly Cost (USD) |
| --------------------- | ------------------------------------------- | ------------------ |
| **PostgreSQL**        | DO Managed DB (db-s-1vcpu-1gb, single node) | ~$15               |
| **Subtotal Database** |                                             | **$15**            |

> Note: Based on the connection string (`okla-db-do-user-31493168-0.g.db.ondigitalocean.com:25060`), this is a DO Managed PostgreSQL instance. The smallest plan (1 vCPU, 1 GB RAM, 10 GB SSD) is $15/mo.

### Networking & Storage

| Resource                     | Details                   | Monthly Cost (USD) |
| ---------------------------- | ------------------------- | ------------------ |
| **Load Balancer**            | 1× DO LB (ingress-nginx)  | $12                |
| **Block Storage**            | 10 Gi PVC (llm-model-pvc) | $1                 |
| **Subtotal Network/Storage** |                           | **$13**            |

### Optional/External Services

| Resource                   | Details                   | Monthly Cost (USD) |
| -------------------------- | ------------------------- | ------------------ |
| **DO Spaces** (if used)    | Object storage for media  | $5 (if active)     |
| **Domain (okla.com.do)**   | DNS registration          | ~$0.42/mo ($5/yr)  |
| **Anthropic Claude API**   | Chatbot LLM (pay-per-use) | Variable (~$5–20)  |
| **GHCR (GitHub Packages)** | Container registry        | Free (public)      |
| **Subtotal External**      |                           | **$5–25**          |

---

## 💵 Total Estimated Monthly Cost

| Category                      | Cost (USD)       |
| ----------------------------- | ---------------- |
| DOKS Nodes (2× s-4vcpu-8gb)   | $96              |
| Managed PostgreSQL            | $15              |
| Load Balancer                 | $12              |
| Block Storage (10 Gi)         | $1               |
| External Services (estimated) | $5–25            |
| **TOTAL**                     | **$129–$149/mo** |

---

## ⚠️ Verdict: Cost Exceeds $100/mo Threshold

The current infrastructure costs approximately **$129–$149/month**, which **exceeds the $100/mo threshold** set by the user. Therefore, per the user's instructions:

> _"si el costo de esta en desarrollo es menor que 100 dolares en digital ocean, implementa entonces todas las sugerencias que haces en las auditorias, pero sino dejame evaluar a mi para ver cuando implementarlas."_

**Decision: Audit suggestions will NOT be auto-implemented.** The following optimization suggestions are presented for the user's evaluation.

---

## 🔧 Cost Optimization Suggestions

### Option A: Reduce to $72/mo (Under $100 Target)

| Change                                 | Savings | New Cost   |
| -------------------------------------- | ------- | ---------- |
| Downgrade to 1 node (s-4vcpu-8gb)      | -$48    | $48        |
| Keep smallest PostgreSQL               | $0      | $15        |
| Keep Load Balancer                     | $0      | $12        |
| Remove block storage (move LLM to API) | -$1     | $0         |
| **Total**                              |         | **$75/mo** |

⚠️ **Risk**: Single node = no HA, pod scheduling constrained to ~3.9 CPU / 6.4 Gi RAM. With 37 pods, this is tight but feasible given most pods request only 50m CPU / 128Mi RAM.

### Option B: Optimize Current 2-Node Setup (~$124/mo)

| Change                                             | Savings          | New Cost     |
| -------------------------------------------------- | ---------------- | ------------ |
| Keep 2 nodes (HA)                                  | $0               | $96          |
| Downgrade DB to $12/mo plan (if available)         | -$3              | $12          |
| Keep Load Balancer                                 | $0               | $12          |
| Remove unused block storage                        | -$1              | $0           |
| Consolidate AuditService replicas (3→1)            | CPU/mem savings  | $0           |
| Remove idle debug pods (curl-e2e4, db-check, etc.) | Pod slot savings | $0           |
| **Total**                                          |                  | **~$120/mo** |

### Option C: Move to App Platform (~$50–70/mo)

Move frontend and some lightweight services to DO App Platform, keep only critical services in K8s. This is a major architectural change requiring evaluation.

---

## 📋 Non-Cost Audit Suggestions (For User Evaluation)

### Quick Wins (No Cost Impact)

1. **Remove debug pods** — `curl-e2e4`, `db-check`, `psql-delete`, `psql-test`, `login2`, `decode-admin` are idle/completed pods consuming pod slots
2. **Consolidate AuditService** — Currently 3 replicas running; reduce to 1 for dev/staging
3. **Add resource limits** to Video360Service — Currently requests 200m CPU / 256Mi but limits 1 CPU / 1 Gi (oversized)
4. **Install metrics-server** — `kubectl top nodes/pods` not working, needed for autoscaling decisions

### Medium-Term (Requires Evaluation)

5. **Configure Horizontal Pod Autoscaler (HPA)** — Scale pods based on actual load instead of fixed replicas
6. **Set up CDN** — Cloudflare free tier for static assets, reduce LB bandwidth costs
7. **Implement pod disruption budgets** — Safer rolling updates
8. **Move RabbitMQ and Redis** to managed services — Better reliability but higher cost

### Long-Term

9. **Evaluate spot/preemptible nodes** — DO doesn't offer this yet, but monitor
10. **Multi-region deployment** — When traffic justifies it

---

## 📊 Resource Utilization Summary

**Cluster Capacity:** 8 vCPU, ~16 Gi RAM (2 nodes)

| Metric | Requested | Capacity   | Utilization |
| ------ | --------- | ---------- | ----------- |
| CPU    | ~2,125m   | 7,780m     | ~27%        |
| Memory | ~5,504 Mi | ~12,832 Mi | ~43%        |
| Pods   | 37        | 220        | ~17%        |

> The cluster is **underutilized** in CPU (~27%) but moderately used in memory (~43%). A single node could technically handle the workload, but 2 nodes provide high availability.

---

_Report generated 2026-03-05 — OKLA Infrastructure Audit_
