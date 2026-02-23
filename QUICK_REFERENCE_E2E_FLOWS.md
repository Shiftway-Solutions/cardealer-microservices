# 🎯 GUÍA RÁPIDA: Documentos de Auditoría E2E Production Flows

**Fecha:** 2026-02-23  
**Estado:** ✅ Listos para usar

---

## 📌 ¿QUÉ NECESITO?

### ❓ \"Quiero ejecutar el flujo de seller y dealer en producción\"
→ **Archivo:** `PROMPT_E2E_GUEST_FLOWS.md`  
→ **Acción:** Copia/pega en terminal o GitHub Issues y sigue paso a paso

---

### ❓ \"Quiero entender qué endpoints se usan y si hay errores\"
→ **Archivo:** `AUDIT_PRODUCTION_GUEST_FLOWS.md`  
→ **Acción:** Lee secciones "MAPEO COMPLETO" y "BUGS ENCONTRADOS"

---

### ❓ \"Necesito un resumen ejecutivo de la auditoría\"
→ **Archivo:** `SUMMARY_AUDIT_E2E_FLOWS.md`  
→ **Acción:** Lee la sección \"HALLAZGOS PRINCIPALES\" (2 min lectura)

---

### ❓ \"¿Qué bugs debo corregir primero?\"
→ **Archivo:** `SUMMARY_AUDIT_E2E_FLOWS.md`  
→ **Sección:** \"FIXES PENDIENTES (Prioridad)\"  
→ **Orden:** Priority 1 (ninguno), Priority 2 (5 bugs listados)

---

### ❓ \"Quiero crear un test Playwright basado en la auditoría\"
→ **Archivo:** `PROMPT_E2E_GUEST_FLOWS.md`  
→ **Secciones:** PASO S5–S8 (seller), PASO D5–D10 (dealer)  
→ **Convierte a:** `frontend/web-next/tests/e2e/guest-flows.spec.ts`

---

## 📂 ESTRUCTURA DE ARCHIVOS

```
cardealer-microservices/
├── PROMPT_E2E_GUEST_FLOWS.md              ← ⭐ USA ESTE para ejecutar flujos
├── AUDIT_PRODUCTION_GUEST_FLOWS.md        ← Auditoría detallada (referencia)
├── SUMMARY_AUDIT_E2E_FLOWS.md             ← Resumen ejecutivo (lectura rápida)
├── REPORT_DEALER_FLOW_PROD.md             ← Reporte histórico (referencia)
├── REPORT_SELLER_FLOW.md                  ← Reporte histórico (referencia)
└── QUICK_REFERENCE_E2E_FLOWS.md           ← Este archivo
```

---

## 🚀 INICIO RÁPIDO (5 MINUTOS)

### Paso 1: Leer el resumen
```bash
cat SUMMARY_AUDIT_E2E_FLOWS.md | head -80
```

### Paso 2: Obtener el prompt
```bash
cat PROMPT_E2E_GUEST_FLOWS.md
```

### Paso 3: Ejecutar en terminal o copiar a GitHub Issues
```bash
# Opción A: Terminal
# Sigue paso a paso: PASO S1, S2, ..., S9 para seller
#                   PASO D1, D2, ..., D10 para dealer

# Opción B: GitHub Issues
# 1. Abre: https://github.com/gregorymorenoiem/cardealer-microservices/issues/new
# 2. Copia/pega contenido de PROMPT_E2E_GUEST_FLOWS.md
# 3. Asigna a QA team
```

---

## 📋 CHECKLIST — ¿QUÉ TIENE CADA DOCUMENTO?

| Documento | Introducción | Pasos | Troubleshooting | Checklist | Bugs |
|-----------|--------------|-------|-----------------|-----------|------|
| PROMPT_E2E_GUEST_FLOWS.md | ✅ Completa | ✅ S1–S9, D1–D10 | ✅ Sí | ✅ 25 items | ✅ Listados |
| AUDIT_PRODUCTION_GUEST_FLOWS.md | ✅ Sí | ❌ No | ✅ Detallado | ✅ Tablas | ✅ Análisis |
| SUMMARY_AUDIT_E2E_FLOWS.md | ✅ Ejecutivo | ⚠️ Resumen | ❌ Mínimo | ✅ Métricas | ✅ Priorizado |
| REPORT_DEALER_FLOW_PROD.md | ✅ Histórico | ✅ 7 pasos | ✅ Sí | ⚠️ Dealer only | ✅ 5 bugs |
| REPORT_SELLER_FLOW.md | ✅ Histórico | ✅ 10 pasos | ⚠️ Mínimo | ⚠️ Seller only | ✅ 2 bugs |

---

## 🎯 CASOS DE USO

### Caso 1: QA Manual
```
1. Abre: PROMPT_E2E_GUEST_FLOWS.md
2. Navega a https://okla.com.do
3. Sigue PASO S1 → S2 → ... → S9 (seller)
4. Luego PASO D1 → D2 → ... → D10 (dealer)
5. Completa checklist al final
6. Documenta en GitHub Issues
```

### Caso 2: Dev Debugging
```
1. Bug encontrado: POST /api/dealers retorna 401
2. Abre: AUDIT_PRODUCTION_GUEST_FLOWS.md
3. Sección: \"BUGS ENCONTRADOS → BUG-D001\"
4. Lee: Causa raíz, fix aplicado, commit hash
5. Aplica fix similar al repo
```

### Caso 3: Automatización Playwright
```
1. Abre: PROMPT_E2E_GUEST_FLOWS.md
2. Extrae: Pasos PASO S2–S8, D2–D10
3. Crea: tests/e2e/guest-flows.spec.ts
4. Convierte: describe/it/expect basado en pasos
5. Ejecuta: pnpm test:e2e:guest-flows
```

### Caso 4: CI/CD Integration
```
1. Post-deploy a producción:
   - Ejecuta PROMPT_E2E_GUEST_FLOWS.md automáticamente
   - O: Corre Playwright spec
2. Si falla: Bloquea release
3. Si pasa: Continúa con smoke tests
```

---

## 🐛 BUGS QUICK REFERENCE

| Bug | Severidad | Estado | Fix |
|----|-----------|--------|-----|
| BUG-D001 (JWT SigningKey) | 🔴 Crítica | ✅ FIXED | ✅ commit 7fd97d55 |
| BUG-D005 (BillingService migrations) | 🔴 Alta | ❌ OPEN | Enable auto-migration |
| BUG-D004 (Ocelot routing) | 🟡 Media | ❌ OPEN | Check ocelot.prod.json |
| BUG-S001 + D002 (Notificaciones) | 🟡 Media | ❌ OPEN | Add KYC event handler |
| BUG-D003 (Image upload 500) | 🟡 Media | ❌ OPEN | Debug AddImagesHandler |

→ Detalles completos en: `AUDIT_PRODUCTION_GUEST_FLOWS.md` → Sección \"BUGS ENCONTRADOS\"

---

## 📞 REFERENCIAS INTERNAS

| Recurso | Ubicación | Propósito |
|---------|-----------|----------|
| Arquitectura | docs/ARCHITECTURE.md | Entender servicios y eventos |
| Kubernetes | docs/KUBERNETES.md | Kubectl commands y troubleshooting |
| Seguridad | docs/SECURITY.md | Validadores, CSRF, JWT |
| Copilot Instructions | .github/copilot-instructions.md | Reglas de proyecto |

---

## ⏱️ TIMELINE

| Actividad | Tiempo | Dependencias |
|-----------|--------|--------------|
| Leer resumen | 5 min | Ninguna |
| Ejecutar flujo seller | 5–8 min | Cluster health OK |
| Ejecutar flujo dealer | 8–12 min | Email verificado |
| Total end-to-end | 50–70 min | Incluye waiting times |

---

## ✅ VALIDACIÓN

**Todos los documentos están:**
- ✅ Generados: 2026-02-23
- ✅ Validados: 25+ endpoints testeados
- ✅ Actualizados: Incluyen fixes hasta commit 7fd97d55
- ✅ Listos: Para QA manual o automatización
- ✅ Documentados: Con ejemplos y comandos

**Recomendación:** Usar `PROMPT_E2E_GUEST_FLOWS.md` como source of truth para todas las validaciones futuras.

---

## 🎓 TIPS PARA USAR PROMPT_E2E_GUEST_FLOWS.md

1. **Copiar en bloques:** Copia cada PASO completo (PASO S1, S2, etc.) antes de ejecutar
2. **Verificar con DB:** Siempre confirma cambios con comandos `kubectl exec ... psql`
3. **Documentar errores:** Si algo falla, anota: HTTP status, error message, logs
4. **Usar workarounds:** Algunos bugs tienen workarounds (e.g., BUG-D003)
5. **Reutilizar datos:** Una vez creado seller/dealer, puedes reutilizar email en nuevos tests

---

## 🚀 EMPEZAR AHORA

```bash
# Opción 1: Leer en terminal
cat SUMMARY_AUDIT_E2E_FLOWS.md

# Opción 2: Abrir en editor
code PROMPT_E2E_GUEST_FLOWS.md  # ← Start here

# Opción 3: Copiar a GitHub Issues
# Abre: https://github.com/gregorymorenoiem/cardealer-microservices/issues/new
# Pega: (contenido de PROMPT_E2E_GUEST_FLOWS.md)
# Asigna: QA Team
```

---

**¿Preguntas?** Revisa el documento correspondiente o abre una issue referenciando este archivo.

**¿Listo?** → Abre `PROMPT_E2E_GUEST_FLOWS.md` y comienza con PASO S1 o PASO D1.

🎯 **Tu siguiente paso:** Copia `PROMPT_E2E_GUEST_FLOWS.md` completo y úsalo para validar los flujos en producción.
