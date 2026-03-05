# 🛡️ Registro de Especialistas de Auditoría — OKLA

**Proyecto:** OKLA (CarDealer Microservices)  
**Última actualización:** Febrero 17, 2026  
**Total de auditorías realizadas:** 13  
**Total de especialistas futuros planificados:** 4

---

## 📊 Resumen de Estado

| Categoría                              | Auditorías | Estado                |
| -------------------------------------- | ---------- | --------------------- |
| ✅ Completadas y remediadas            | 10         | Hallazgos corregidos  |
| ⚠️ Completadas con pendientes          | 3          | Requieren seguimiento |
| 🔜 Planificadas (nuevos especialistas) | 4          | Próximos a ejecutar   |

---

## ✅ ESPECIALISTAS QUE HAN AUDITADO EL SISTEMA

### 1. 🏗️ Model Architect (Arquitecto de Modelos)

| Detalle        | Valor                                                       |
| -------------- | ----------------------------------------------------------- |
| **Auditor**    | GitHub Copilot — Model Architect                            |
| **Reporte**    | `docs/chatbot-llm/CHATBOT_ARCHITECTURE_AND_MODELS_AUDIT.md` |
| **Versión**    | 3.0 (Post-Remediación completa)                             |
| **Fecha**      | Febrero 17, 2026                                            |
| **Puntuación** | 6.8 → 8.1 → **9.2/10**                                      |

**Áreas evaluadas:**

- Clean Architecture del ChatbotService
- Modelo LLM (GGUF + Fine-tuning QLoRA)
- Integración Backend ↔ LLM
- Esquema de Base de Datos
- Deuda Técnica (migración Dialogflow)
- Seguridad (JWT, sanitización)
- Resiliencia (Polly circuit breaker)
- Testing
- Observabilidad (Prometheus, health checks)
- Production Readiness (DOKS)

**Hallazgos clave remediados:**

- ✅ Migración completa de Dialogflow legacy (22 archivos renombrados)
- ✅ SQL renames y Docker rebuild verificados
- ✅ 11 ítems de remediación completados

---

### 2. 🔬 AI Researcher (Investigador Senior IA)

| Detalle        | Valor                                                   |
| -------------- | ------------------------------------------------------- |
| **Auditor**    | Investigador Senior — Ingeniería de Modelos de Lenguaje |
| **Reporte**    | `docs/chatbot-llm/AUDIT_AI_RESEARCHER_REPORT.md`        |
| **Versión**    | 2.0 (Post-Remediación)                                  |
| **Fecha**      | Febrero 17, 2026                                        |
| **Puntuación** | 7.4 → **9.3/10**                                        |

**Áreas evaluadas:**

- Diseño del Dataset (37 intents, 1,376 templates)
- Prompt Engineering (system prompt, JSON schema)
- Training Pipeline (QLoRA + GGUF Q4_K_M)
- Alineamiento Entrenamiento ↔ Inferencia
- Inference Server (llama-cpp-python + FastAPI)
- Backend Integration (.NET 8)
- Evaluación y Mejora Continua
- Seguridad del Modelo (PII, prompt injection)

**Hallazgos clave remediados:**

- ✅ CRIT-1: Context window overflow (N_CTX 2048→4096)
- ✅ CRIT-2: JSON schema mismatch (5→8 campos, LlmLeadSignals reescrito)
- ✅ CRIT-3: MAX_TOKENS insuficiente (400→600)
- ✅ CRIT-4: Gate pre-deploy creado (9 métricas GO/NO-GO)
- ✅ PiiDetector.cs y PromptInjectionDetector.cs creados
- ✅ GBNF grammar + explicit Llama 3 template

---

### 3. 🖥️ Frontend Auditor

| Detalle        | Valor                           |
| -------------- | ------------------------------- |
| **Auditor**    | GitHub Copilot                  |
| **Reporte**    | `docs/FRONTEND_AUDIT_REPORT.md` |
| **Fecha**      | Enero 29, 2026                  |
| **Puntuación** | ⚠️ Requiere atención            |

**Áreas evaluadas:**

- Consistencia de tipado de Props (310 archivos TSX)
- Bundle size (3.16 MB)
- Cobertura de tests (5.5%)
- Patrones React y code-splitting
- Componentes sin tipado estricto

**Estado:** Hallazgos identificados, remediación parcial.

---

### 4. 🔐 Auditor de Roles y Seguridad

| Detalle        | Valor                                    |
| -------------- | ---------------------------------------- |
| **Auditor**    | Auditoría automatizada                   |
| **Reporte**    | `docs/AUDIT_GESTION_ROLES_COMPLETADA.md` |
| **Fecha**      | Febrero 6, 2026                          |
| **Puntuación** | ✅ Completado                            |

**Áreas evaluadas:**

- Flujo de gestión de roles
- Mapping AccountType ↔ Role
- Asignación de roles en JWT
- Reglas de visibilidad por tipo de cuenta

**Estado:** ✅ Todos los hallazgos remediados.

---

### 5. 🌐 Gateway Auditor

| Detalle        | Valor                                                               |
| -------------- | ------------------------------------------------------------------- |
| **Auditor**    | GitHub Copilot                                                      |
| **Reportes**   | `docs/GATEWAY_AUDIT_SUMMARY.md` + `docs/GATEWAY_ENDPOINTS_AUDIT.md` |
| **Fecha**      | Enero 29, 2026                                                      |
| **Puntuación** | 85% cobertura de rutas (22/30 servicios)                            |

**Áreas evaluadas:**

- Cobertura de rutas del Gateway (~145 rutas)
- Servicios faltantes (MaintenanceService, AlertService)
- Errores de path mapping (ComparisonService, AzulPayment)
- Comparación endpoint-by-endpoint de 30+ microservicios

**Estado:** ⚠️ 2 servicios críticos sin integrar al Gateway.

---

### 6. 📐 Auditor de Estándares y Observabilidad

| Detalle        | Valor                                                     |
| -------------- | --------------------------------------------------------- |
| **Auditor**    | Auditoría basada en estándares (ISO 25010, OpenTelemetry) |
| **Reporte**    | `docs/OBSERVABILITY_TESTING_DATA_AUDIT.md`                |
| **Fecha**      | Febrero 13, 2026                                          |
| **Puntuación** | **70/100 (C+)**                                           |

**Áreas evaluadas y sub-puntuaciones:**
| Sub-área | Puntuación |
|----------|-----------|
| Observabilidad (Serilog, OpenTelemetry) | 82/100 (B+) |
| Testing (cobertura, pirámide) | 52/100 (D) |
| Arquitectura de Datos | 71/100 (C+) |
| Diseño de API (REST Maturity) | 66/100 (C) |
| Estándares de Logging | 78/100 (B) |

**Hallazgos:** 9 Críticos, 14 Major, 8 Minor.  
**Estado:** ⚠️ Requiere remediación significativa (especialmente Testing 52/100).

---

### 7. 📋 Auditor de Cobertura de Procesos de Negocio

| Detalle      | Valor                               |
| ------------ | ----------------------------------- |
| **Auditor**  | GitHub Copilot + automatizado       |
| **Reportes** | 3 auditorías de cobertura funcional |
| **Fecha**    | Enero 9–29, 2026                    |

**Sub-auditorías:**

| Auditoría                  | Reporte                                      | Puntuación                            |
| -------------------------- | -------------------------------------------- | ------------------------------------- |
| Process Matrix vs Frontend | `docs/PROCESS_MATRIX_VS_FRONTEND_REBUILD.md` | ⚠️ Parcial (DealerAnalytics faltante) |
| Test Drives (Agendamiento) | `docs/05-AGENDAMIENTO_TEST_DRIVES_AUDIT.md`  | 79% → ✅ Completado                   |
| Reviews & Reputación       | `docs/07-REVIEWS_REPUTACION_AUDIT.md`        | 90% → **100%**                        |
| Búsqueda & Recomendaciones | `docs/SEARCH_RECOMMENDATIONS_AUDIT.md`       | 97% → **100%**                        |

---

### 8. 📝 Auditor de Documentación API

| Detalle        | Valor                                                 |
| -------------- | ----------------------------------------------------- |
| **Auditor**    | Script automatizado                                   |
| **Reporte**    | `docs/API_DOCUMENTATION_AUDIT.md`                     |
| **Fecha**      | Enero 30, 2026                                        |
| **Puntuación** | 🔴 **9.3% cobertura** (12/129 endpoints documentados) |

**Estado:** 🔴 Crítico — requiere documentación masiva de endpoints.

---

## 🔜 ESPECIALISTAS PLANIFICADOS (Próximas Auditorías)

### 9. 🗣️ Conversational AI / Dialogue Systems Specialist

| Detalle           | Valor                                                                           |
| ----------------- | ------------------------------------------------------------------------------- |
| **Prioridad**     | 🔴 P1 — Mayor impacto                                                           |
| **Estado**        | Planificado                                                                     |
| **Justificación** | Ninguna auditoría ha evaluado la calidad real de las conversaciones del chatbot |

**Áreas a evaluar:**
| Área | Descripción |
|------|-------------|
| Flujos multi-turno | ¿El bot mantiene coherencia en 6+ turnos de conversación? |
| Cambios de tema | ¿Maneja transiciones precio → financiamiento → test drive? |
| Calidad del español dominicano | ¿Suena natural? ¿Usa modismos correctos? ¿No suena robótico? |
| Recuperación de errores | ¿Qué pasa cuando el bot no entiende? ¿Escalamiento gracioso? |
| Personalidad "Ana" | ¿Consistente? ¿Empática pero profesional? ¿Tono apropiado? |
| Task completion rate | ¿Cuántos turnos necesita para resolver una consulta? |
| Edge cases conversacionales | Usuarios enojados, preguntas ambiguas, múltiples intents simultáneos |
| Calidad de respuestas | ¿Respuestas informativas sin ser excesivamente largas? |

**Gap que llena:** Las auditorías anteriores optimizaron el _motor_ (pipeline, inferencia, integración) pero nadie ha evaluado si el chatbot _conversa bien_ con usuarios reales.

**Métricas esperadas:**

- Task Completion Rate (TCR)
- Turns-to-Resolution (TTR)
- User Satisfaction Score (simulado)
- Coherence Score multi-turno
- Naturalness Rating del español dominicano

---

### 10. ⚙️ MLOps Engineer

| Detalle           | Valor                                                     |
| ----------------- | --------------------------------------------------------- |
| **Prioridad**     | 🟡 P2                                                     |
| **Estado**        | Planificado                                               |
| **Justificación** | Falta el ciclo de vida operativo del modelo en producción |

**Áreas a evaluar:**
| Área | Descripción |
|------|-------------|
| Model versioning & registry | ¿Cómo se versionan los GGUF? ¿Rollback en <5 min? |
| Monitoring & drift detection | ¿Alertas cuando la calidad degrada en producción? |
| CI/CD para modelos | Pipeline: train → eval → deploy → canary → promote |
| A/B testing framework | Comparar modelo v1 vs v2 con tráfico real |
| Feature store & data lineage | Trazabilidad de datos seed → dataset → modelo |
| Cost optimization | GPU/CPU budget, batching, caching de respuestas frecuentes |
| Observabilidad end-to-end | Latencia desglosada por componente (RAG + LLM + post-process) |
| Reproducibilidad | ¿Se puede recrear exactamente el modelo actual desde código? |

**Gap que llena:** `evaluate_before_deploy.py` es un buen inicio, pero falta el ecosistema operativo completo (model registry, drift detection, automated retraining).

---

### 11. 🔴 AI Red Team / Adversarial Testing Specialist

| Detalle           | Valor                                                                                 |
| ----------------- | ------------------------------------------------------------------------------------- |
| **Prioridad**     | 🟡 P2                                                                                 |
| **Estado**        | Planificado                                                                           |
| **Justificación** | `PromptInjectionDetector.cs` usa 28 patrones estáticos — un adversario real los evade |

**Áreas a evaluar:**
| Área | Descripción |
|------|-------------|
| Prompt injection avanzado | Unicode obfuscation, token splitting, indirect injection |
| Jailbreaking del fine-tune | ¿Se puede hacer que "Ana" deje de ser Ana? |
| Data extraction attacks | ¿Se puede extraer el system prompt? ¿Datos de training? |
| Hallucination exploitation | Hacer que invente vehículos, precios, garantías falsas |
| Social engineering via chat | Manipular al bot para dar "descuentos" o "promesas" |
| PII leakage bajo presión | Bypasses del PiiDetector con encoding/typos intencionales |
| Compliance boundary testing | Hacer que dé asesoría legal/financiera indirectamente |
| Control token injection | Inyectar `<|eot_id|>`, `<|start_header_id|>` en mensajes |

**Gap que llena:** La puntuación de seguridad (9.3/10) se basa en regex estáticos. Un adversario real no sigue patrones predefinidos.

**Entregables esperados:**

- Catálogo de vulnerabilidades con PoC
- Bypass rate del PiiDetector y PromptInjectionDetector
- Recomendaciones de hardening
- Test suite adversarial reutilizable

---

### 12. 🗣️ Computational Linguist / NLP Specialist

| Detalle           | Valor                                                        |
| ----------------- | ------------------------------------------------------------ |
| **Prioridad**     | 🟢 P3                                                        |
| **Estado**        | Planificado                                                  |
| **Justificación** | Análisis profundo del español dominicano y calidad semántica |

**Áreas a evaluar:**
| Área | Descripción |
|------|-------------|
| Autenticidad dialectal | ¿El modelo usa correctamente modismos dominicanos? |
| Registro lingüístico | ¿Adapta formalidad según contexto? (tú vs usted) |
| Errores semánticos | ¿Las respuestas son factualmente correctas sobre vehículos? |
| Cobertura léxica | ¿Maneja vocabulario automotriz completo en español RD? |
| Sesgos lingüísticos | ¿Discrimina por dialecto o forma de escribir del usuario? |
| Calidad de traducciones | Si el usuario escribe en inglés, ¿responde correctamente? |
| Coherencia terminológica | ¿Usa consistentemente los mismos términos? (financiamiento vs préstamo) |

**Gap que llena:** El dataset tiene 1,376 templates en español dominicano, pero nadie ha validado la _calidad lingüística_ con criterios de un lingüista profesional.

---

## 📈 Mapa de Cobertura de Auditorías

```
                    COMPLETADAS                           PLANIFICADAS
                    ───────────                           ───────────
   ┌─────────────────────────────────────┐    ┌──────────────────────────┐
   │                                     │    │                          │
   │  🏗️ Model Architect (9.2/10)       │    │  🗣️ Conversational AI   │
   │  🔬 AI Researcher (9.3/10)         │    │     Specialist (P1)      │
   │  🖥️ Frontend Auditor (⚠️)          │    │                          │
   │  🔐 Roles & Security (✅)          │    │  ⚙️ MLOps Engineer (P2)  │
   │  🌐 Gateway Auditor (85%)          │    │                          │
   │  📐 Standards & Observability (C+) │    │  🔴 AI Red Team (P2)    │
   │  📋 Business Coverage (4 audits)   │    │                          │
   │  📝 API Documentation (9.3% 🔴)    │    │  🗣️ Computational       │
   │                                     │    │     Linguist (P3)        │
   └─────────────────────────────────────┘    └──────────────────────────┘

   Pipeline LLM:  [Dataset] → [Training] → [Inference] → [Backend] → [UX]
                      ✅           ✅           ✅            ✅        🔜
                   Researcher   Researcher   Researcher    Architect  Conv.AI
```

---

## 🔄 Orden de Ejecución Recomendado

| Fase        | Especialista                    | Área Principal         | Dependencia                                |
| ----------- | ------------------------------- | ---------------------- | ------------------------------------------ |
| **Fase 1**  | 🗣️ Conversational AI Specialist | Calidad de diálogo     | Ninguna — puede empezar ahora              |
| **Fase 2a** | ⚙️ MLOps Engineer               | Operaciones del modelo | Tras Fase 1 (usa métricas de calidad)      |
| **Fase 2b** | 🔴 AI Red Team                  | Seguridad adversarial  | Tras Fase 1 (el modelo debe estar estable) |
| **Fase 3**  | 🗣️ Computational Linguist       | Calidad lingüística    | Tras Fase 1 + 2 (modelo final)             |

---

## 📊 Resumen de Puntuaciones Actuales

| Área                   | Auditor                                | Puntuación      | Estado                    |
| ---------------------- | -------------------------------------- | --------------- | ------------------------- |
| Chatbot — Arquitectura | Model Architect                        | **9.2/10**      | ✅ Excelente              |
| Chatbot — Pipeline IA  | AI Researcher                          | **9.3/10**      | ✅ Excelente              |
| Chatbot — Conversación | Conversational AI _(planificado)_      | —               | 🔜 Pendiente              |
| Chatbot — MLOps        | MLOps Engineer _(planificado)_         | —               | 🔜 Pendiente              |
| Chatbot — Adversarial  | AI Red Team _(planificado)_            | —               | 🔜 Pendiente              |
| Chatbot — Lingüística  | Computational Linguist _(planificado)_ | —               | 🔜 Pendiente              |
| Frontend               | Frontend Auditor                       | ⚠️              | Requiere seguimiento      |
| Seguridad/Roles        | Auditor de Roles                       | ✅              | Completado                |
| Gateway                | Gateway Auditor                        | 85%             | ⚠️ 2 servicios pendientes |
| Estándares             | Observability Auditor                  | **70/100 (C+)** | ⚠️ Testing 52/100         |
| Procesos de Negocio    | Business Coverage                      | 79–100%         | Mayormente completado     |
| Documentación API      | Script automatizado                    | 🔴 **9.3%**     | Requiere acción urgente   |

---

_Documento de seguimiento — Registro de Especialistas de Auditoría_  
_Proyecto OKLA — Febrero 2026_
