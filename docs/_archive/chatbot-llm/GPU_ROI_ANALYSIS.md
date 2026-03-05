# ============================================================================

# GPU vs CPU ROI Analysis — OKLA LLM Inference Server

# ============================================================================

# R21 (MLOps): Detailed cost-benefit analysis for GPU vs CPU deployment.

# ============================================================================

## 📊 Executive Summary

| Metric               | CPU-Only (Current)      | GPU Droplet (Proposed)   |
| -------------------- | ----------------------- | ------------------------ |
| **Infrastructure**   | DO $96/mo (8vCPU, 16GB) | DO $350/mo (GPU Droplet) |
| **Inference Time**   | 2-5 min/request         | 3-15 sec/request         |
| **Throughput**       | ~20 req/hour            | ~240 req/hour            |
| **Concurrent Users** | 1 (sequential)          | ~4-8 parallel            |
| **User Experience**  | Poor (long wait)        | Good (real-time)         |
| **Annual Cost**      | $1,152                  | $4,200                   |

---

## 🖥️ Current Setup: CPU-Only

### Hardware

- **Droplet:** DO Regular 8vCPU / 16GB RAM
- **Model:** Llama 3.1 8B Q4_K_M (~4.7 GB GGUF)
- **Context:** 2048 tokens (reduced from 4096 for speed)
- **Threads:** 4 (N_THREADS=4)

### Performance Characteristics

| Metric                      | Value                                 |
| --------------------------- | ------------------------------------- |
| Time to First Token         | 15-30 sec                             |
| Token Generation Rate       | ~2-5 tokens/sec                       |
| Full Response (~150 tokens) | 30-75 sec                             |
| Full Response (~300 tokens) | 60-150 sec                            |
| Max Concurrent Requests     | 1 (sequential via ThreadPoolExecutor) |
| Memory Usage                | ~6-7 GB (model + KV cache)            |
| CPU Usage During Inference  | 95-100% on all cores                  |

### Monthly Costs

| Item                      | Cost       |
| ------------------------- | ---------- |
| DO Droplet (s-8vcpu-16gb) | $96/mo     |
| PVC Storage (10GB)        | $1/mo      |
| **Total**                 | **$97/mo** |

---

## 🎮 Option A: Digital Ocean GPU Droplet

### Hardware

- **Droplet:** DO GPU Droplet (NVIDIA A10G or similar)
- **GPU Memory:** 24 GB VRAM
- **Model:** Llama 3.1 8B Q4_K_M → runs entirely in VRAM
- **GPU Layers:** -1 (ALL layers on GPU)

### Expected Performance

| Metric                      | Value                            |
| --------------------------- | -------------------------------- |
| Time to First Token         | 0.5-2 sec                        |
| Token Generation Rate       | 30-60 tokens/sec                 |
| Full Response (~150 tokens) | 3-5 sec                          |
| Full Response (~300 tokens) | 5-10 sec                         |
| Max Concurrent Requests     | 4-8 (GPU can batch)              |
| VRAM Usage                  | ~5 GB (model) + ~2 GB (KV cache) |

### Monthly Costs

| Item           | Cost         |
| -------------- | ------------ |
| DO GPU Droplet | ~$350/mo     |
| PVC Storage    | $1/mo        |
| **Total**      | **~$351/mo** |

### Speed Improvement: **10-20x faster**

---

## 🎮 Option B: Smaller GPU (NVIDIA T4)

### Hardware

- **Cloud:** AWS g4dn.xlarge or GCP n1-standard-4 + T4
- **GPU Memory:** 16 GB VRAM
- **Model:** Fits entirely in VRAM (4.7 GB)

### Expected Performance

| Metric                      | Value            |
| --------------------------- | ---------------- |
| Token Generation Rate       | 20-40 tokens/sec |
| Full Response (~150 tokens) | 4-8 sec          |
| Concurrent Requests         | 2-4              |

### Monthly Costs

| Item                           | Cost     |
| ------------------------------ | -------- |
| AWS g4dn.xlarge (on-demand)    | ~$380/mo |
| AWS g4dn.xlarge (reserved 1yr) | ~$230/mo |
| GCP n1-standard-4 + T4 (spot)  | ~$150/mo |

---

## 🎮 Option C: Hybrid (CPU default + GPU burst)

### Strategy

- Keep CPU droplet for low-traffic periods
- Spin up GPU droplet during peak hours (9AM-9PM)
- Use K8s HPA or scheduled scaling

### Monthly Costs (12h GPU / 12h CPU)

| Item                    | Cost         |
| ----------------------- | ------------ |
| CPU Droplet (always-on) | $96/mo       |
| GPU Droplet (12h/day)   | ~$175/mo     |
| **Total**               | **~$271/mo** |

---

## 📈 ROI Calculation

### Traffic Assumptions (Conservative)

| Period             | Daily Requests | Monthly Requests |
| ------------------ | -------------- | ---------------- |
| Launch (Month 1-3) | 50-100         | 1,500-3,000      |
| Growth (Month 4-6) | 200-500        | 6,000-15,000     |
| Scale (Month 7-12) | 500-2,000      | 15,000-60,000    |

### Break-Even Analysis

**CPU can handle:** ~20 req/hour × 24h = 480 req/day = ~14,400/month

- ✅ Sufficient for Launch + Growth phases
- ❌ Bottleneck at Scale phase (>500/day sequential processing)

**GPU unlocks:**

- 10-20x faster responses → better UX → higher conversion
- 4-8x concurrent capacity → handles traffic spikes
- Critical for real-time chat experience

### When to Switch to GPU

| Signal          | Threshold                   |
| --------------- | --------------------------- |
| P95 latency     | > 60 seconds consistently   |
| Daily requests  | > 300/day                   |
| Queue depth     | > 5 requests waiting        |
| User complaints | Chatbot "too slow" feedback |
| Conversion rate | Drops due to wait time      |

---

## 🎯 Recommendation

### Phase 1 — Now (CPU): ✅ KEEP

- Traffic is low (<100 req/day)
- $97/mo is cost-effective for MVP/launch
- Focus engineering effort on model quality, not infrastructure

### Phase 2 — Growth (3-6 months): EVALUATE

- Monitor P95 latency trends
- If P95 > 30s consistently, begin GPU evaluation
- Consider Option C (Hybrid) for cost optimization

### Phase 3 — Scale (6-12 months): MIGRATE TO GPU

- When daily requests > 300, GPU becomes necessary
- Option A (DO GPU Droplet) recommended for simplicity
- Expected cost: +$254/mo but 10-20x better UX

### Configuration Changes for GPU

```yaml
# In chatbotservice.yaml — LLM Server env vars
- name: N_GPU_LAYERS
  value: "-1" # ALL layers on GPU (currently "0")
- name: N_CTX
  value: "4096" # Can increase context with GPU
- name: N_THREADS
  value: "2" # Fewer CPU threads needed
- name: N_BATCH
  value: "1024" # Larger batch size with GPU
```

```dockerfile
# In LlmServer/Dockerfile — GPU variant
FROM nvidia/cuda:12.1-runtime-ubuntu22.04
# ... install llama-cpp-python with CUDA support
CMAKE_ARGS="-DLLAMA_CUBLAS=on" pip install llama-cpp-python
```

---

## 📊 Decision Matrix

| Factor (Weight)    | CPU (Current) | GPU (Option A) | Hybrid (Option C) |
| ------------------ | :-----------: | :------------: | :---------------: |
| Cost (30%)         |  ⭐⭐⭐⭐⭐   |      ⭐⭐      |      ⭐⭐⭐       |
| Performance (25%)  |     ⭐⭐      |   ⭐⭐⭐⭐⭐   |     ⭐⭐⭐⭐      |
| Scalability (20%)  |     ⭐⭐      |   ⭐⭐⭐⭐⭐   |     ⭐⭐⭐⭐      |
| Simplicity (15%)   |  ⭐⭐⭐⭐⭐   |    ⭐⭐⭐⭐    |       ⭐⭐        |
| UX Impact (10%)    |     ⭐⭐      |   ⭐⭐⭐⭐⭐   |     ⭐⭐⭐⭐      |
| **Weighted Score** |   **3.25**    |    **3.80**    |     **3.35**      |

**Verdict:** Stay on CPU now, plan GPU migration when traffic justifies it.

---

_Last updated: February 2026_
_Author: MLOps Pipeline_
