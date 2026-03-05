# 📁 08-DGII-COMPLIANCE - DGII y Cumplimiento Legal

> **Descripción:** Facturación electrónica DGII y cumplimiento legal RD  
> **Total:** 8 documentos  
> **Prioridad:** 🟠 P1 - Requerimiento legal

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                                                    | Descripción                         | Prioridad |
| --- | -------------------------------------------------------------------------- | ----------------------------------- | --------- |
| 1   | [01-facturacion-dgii.md](01-facturacion-dgii.md)                           | Facturación electrónica DGII        | P0        |
| 2   | [02-auditoria-compliance-legal.md](02-auditoria-compliance-legal.md)       | Auditoría de compliance legal       | P1        |
| 3   | [03-obligaciones-fiscales.md](03-obligaciones-fiscales.md)                 | Obligaciones fiscales DGII          | P1        |
| 4   | [04-registro-gastos.md](04-registro-gastos.md)                             | Registro de gastos operativos       | P2        |
| 5   | [05-automatizacion-reportes.md](05-automatizacion-reportes.md)             | Automatización de reportes DGII     | P2        |
| 6   | [06-preparacion-auditoria.md](06-preparacion-auditoria.md)                 | Preparación para auditoría DGII     | P2        |
| 7   | [07-consentimiento-comunicaciones.md](07-consentimiento-comunicaciones.md) | Ley 172-13 (Protección de datos RD) | P1        |
| 8   | [08-legal-common-pages.md](08-legal-common-pages.md)                       | Páginas legales comunes             | P1        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-facturacion-dgii.md      → e-CF (Comprobantes Fiscales)
2. 08-legal-common-pages.md    → Términos, privacidad, etc.
3. 07-consentimiento-comunicaciones.md → Ley 172-13
4. 03-obligaciones-fiscales.md → Obligaciones fiscales
5. 02-auditoria-compliance-legal.md → Auditoría
6. 04-registro-gastos.md       → Gastos
7. 05-automatizacion-reportes.md → Reportes automáticos
8. 06-preparacion-auditoria.md → Prep auditoría
```

---

## 🔗 Dependencias Externas

- **06-ADMIN/**: Panel de compliance
- **07-PAGOS/**: Facturación
- **Backend**: DGIIService, ComplianceService

---

## 📊 APIs Utilizadas

| Servicio          | Endpoints Principales                           |
| ----------------- | ----------------------------------------------- |
| DGIIService       | POST /dgii/ecf, GET /dgii/ncf-sequence          |
| ComplianceService | GET /compliance/status, POST /compliance/report |
| BillingService    | GET /invoices, POST /invoices/dgii              |

---

## ⚖️ Normativas Aplicables

| Normativa         | Descripción                                 |
| ----------------- | ------------------------------------------- |
| **Ley 172-13**    | Protección de datos personales RD           |
| **DGII e-CF**     | Comprobantes fiscales electrónicos          |
| **Norma 06-2018** | Facturación electrónica                     |
| **ITBIS**         | Impuesto a la transferencia de bienes (18%) |
