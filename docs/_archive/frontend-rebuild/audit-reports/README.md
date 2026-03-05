# 📊 Audit Reports - API Documentation Coverage

Este directorio contiene reportes automáticos de auditoría de documentación de API.

---

## 📁 Contenido

Los reportes se generan ejecutando el script:

```bash
python3 scripts/audit-api-documentation.py
```

Cada ejecución crea 3 archivos con el mismo timestamp:

- `audit-report-YYYYMMDD_HHMMSS.json` - Datos estructurados para análisis programático
- `audit-report-YYYYMMDD_HHMMSS.csv` - Para Excel/Google Sheets
- `audit-report-YYYYMMDD_HHMMSS.md` - Reporte en formato Markdown

---

## 📊 Último Reporte

**Fecha:** Enero 30, 2026

### Resumen

| Métrica                        | Valor |
| ------------------------------ | ----- |
| **Endpoints Documentados**     | 12    |
| **Rutas en Gateway**           | 129   |
| **Cobertura de Documentación** | 9.3%  |

### Desglose por Archivo

| Archivo                 | Endpoints |
| ----------------------- | --------- |
| `02-autenticacion.md`   | 8         |
| `04-subida-imagenes.md` | 1         |
| `05-vehicle-360-api.md` | 3         |

---

## 🔄 Frecuencia de Auditoría

**Recomendado:** Ejecutar cada semana o después de agregar documentación nueva.

```bash
# Ejecutar auditoría
python3 scripts/audit-api-documentation.py

# Ver último reporte
cat docs/frontend-rebuild/audit-reports/audit-report-*.md | tail -100
```

---

## 📈 Tracking de Progreso

Para comparar progreso entre auditorías:

```bash
# Listar todos los reportes
ls -lh audit-report-*.json

# Comparar dos reportes
diff \
  audit-report-20260130_044636.json \
  audit-report-20260206_104500.json
```

---

## 🎯 Meta de Cobertura

| Sprint       | Meta | Estado      |
| ------------ | ---- | ----------- |
| **Actual**   | 9.3% | ⚠️ Muy bajo |
| **Sprint 1** | 40%  | 🎯 Objetivo |
| **Sprint 3** | 60%  | 🎯 Objetivo |
| **Sprint 5** | 80%  | 🎯 Objetivo |
| **Sprint 7** | 95%+ | 🎯 Objetivo |

---

## 🔧 Formato de Reportes

### JSON Format

```json
{
  "audit_date": "2026-01-30T08:45:59.397913Z",
  "summary": {
    "total_documented": 12,
    "total_gateway_routes": 129,
    "coverage_percentage": 9.3
  },
  "documented_endpoints": [
    {
      "method": "POST",
      "route": "/api/auth/login",
      "file": "02-autenticacion.md"
    }
  ],
  "endpoints_by_file": {
    "02-autenticacion.md": 8
  }
}
```

### CSV Format

```csv
Método,Ruta,Archivo Documentado
POST,/api/auth/login,02-autenticacion.md
POST,/api/auth/register,02-autenticacion.md
...
```

### Markdown Format

Ver cualquier archivo `.md` en este directorio para el formato completo.

---

## 📚 Documentación Relacionada

- [AUDITORIA-GATEWAY-ENDPOINTS.md](../AUDITORIA-GATEWAY-ENDPOINTS.md) - Auditoría completa actualizada
- [AUDITORIA-CORRECCION.md](../AUDITORIA-CORRECCION.md) - Metodología de corrección
- [IMPLEMENTACION-SUGERENCIAS-AUDITORIA.md](../IMPLEMENTACION-SUGERENCIAS-AUDITORIA.md) - Implementación completa
- [00-PLAN-AUDITORIA-CORRECCION.md](../00-PLAN-AUDITORIA-CORRECCION.md) - Plan de trabajo

---

_Generado por: audit-api-documentation.py_  
_Última actualización: Enero 30, 2026_
