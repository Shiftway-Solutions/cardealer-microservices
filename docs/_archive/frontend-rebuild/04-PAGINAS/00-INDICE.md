# 📁 04-PAGINAS - Índice Maestro

> **Total:** 104 documentos organizados en 9 secciones  
> **Última actualización:** Enero 31, 2026  
> **Estado:** ✅ Reorganizado y sincronizado

---

## ⚠️ LECTURA OBLIGATORIA

Antes de implementar cualquier página, leer:

| Documento                                                                                          | Descripción                                                                                            |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| [00-reglas-paginas.md](00-reglas-paginas.md)                                                       | **Reglas de desarrollo de páginas** - Las páginas solo componen componentes, no tienen estilos propios |
| [../03-COMPONENTES/00-metodologia-componentes.md](../03-COMPONENTES/00-metodologia-componentes.md) | **Metodología de componentes** - Los estilos viven en los componentes                                  |

---

## 📊 Resumen de Secciones

| #   | Sección                                            | Documentos | Descripción               | Prioridad |
| --- | -------------------------------------------------- | ---------- | ------------------------- | --------- |
| 1   | [01-PUBLICO/](01-PUBLICO/)                         | 10         | Páginas públicas sin auth | 🔴 P0     |
| 2   | [02-AUTH/](02-AUTH/)                               | 6          | Autenticación y seguridad | 🔴 P0     |
| 3   | [03-COMPRADOR/](03-COMPRADOR/)                     | 14         | Flujos del comprador      | 🟠 P1     |
| 4   | [04-VENDEDOR/](04-VENDEDOR/)                       | 5          | Vendedor individual       | 🟠 P1     |
| 5   | [05-DEALER/](05-DEALER/)                           | 25         | Portal de dealers         | 🟠 P1     |
| 6   | [06-ADMIN/](06-ADMIN/)                             | 20         | Panel administrativo      | 🟡 P2     |
| 7   | [07-PAGOS/](07-PAGOS/)                             | 5          | Pagos y facturación       | 🔴 P0     |
| 8   | [08-DGII-COMPLIANCE/](08-DGII-COMPLIANCE/)         | 8          | DGII y cumplimiento legal | 🟠 P1     |
| 9   | [09-COMPONENTES-COMUNES/](09-COMPONENTES-COMUNES/) | 6          | Componentes y layouts     | 🔴 P0     |

---

## 🎯 Orden de Implementación para IA

### Fase 1: Fundamentos (Semana 1-2)

```
1. 09-COMPONENTES-COMUNES/  → Layouts y componentes base
2. 02-AUTH/                 → Login, registro, verificación
3. 01-PUBLICO/              → Home, búsqueda, detalle
4. 07-PAGOS/                → Checkout básico
```

### Fase 2: Usuario (Semana 3-4)

```
5. 03-COMPRADOR/            → Perfil, favoritos, notificaciones
6. 04-VENDEDOR/             → Publicar, dashboard vendedor
```

### Fase 3: Dealer (Semana 5-6)

```
7. 05-DEALER/               → Portal completo de dealers
```

### Fase 4: Admin y Compliance (Semana 7-8)

```
8. 06-ADMIN/                → Panel administrativo
9. 08-DGII-COMPLIANCE/      → Facturación DGII
```

---

## 📋 Índices por Sección

Cada sección tiene su propio `00-INDICE.md` con:

- Lista de documentos
- Orden de implementación
- Dependencias
- APIs utilizadas

### Links Directos

- [01-PUBLICO/00-INDICE.md](01-PUBLICO/00-INDICE.md)
- [02-AUTH/00-INDICE.md](02-AUTH/00-INDICE.md)
- [03-COMPRADOR/00-INDICE.md](03-COMPRADOR/00-INDICE.md)
- [04-VENDEDOR/00-INDICE.md](04-VENDEDOR/00-INDICE.md)
- [05-DEALER/00-INDICE.md](05-DEALER/00-INDICE.md)
- [06-ADMIN/00-INDICE.md](06-ADMIN/00-INDICE.md)
- [07-PAGOS/00-INDICE.md](07-PAGOS/00-INDICE.md)
- [08-DGII-COMPLIANCE/00-INDICE.md](08-DGII-COMPLIANCE/00-INDICE.md)
- [09-COMPONENTES-COMUNES/00-INDICE.md](09-COMPONENTES-COMUNES/00-INDICE.md)

---

## 🗺️ Mapa de Navegación

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ARQUITECTURA DE PÁGINAS                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     09-COMPONENTES-COMUNES                           │   │
│  │                  (Layouts, Button, Card, etc.)                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    ▲                                        │
│           ┌────────────────────────┼────────────────────────┐              │
│           ▼                        ▼                        ▼              │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐        │
│  │   01-PUBLICO    │    │    02-AUTH      │    │   07-PAGOS      │        │
│  │  (Home, Search) │    │ (Login, 2FA)    │    │  (Checkout)     │        │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘        │
│           │                      │                      │                  │
│           ▼                      ▼                      ▼                  │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐        │
│  │  03-COMPRADOR   │    │  04-VENDEDOR    │    │   05-DEALER     │        │
│  │ (Perfil, Alerts)│    │ (Publicar)      │    │ (Portal)        │        │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘        │
│                                    │                                       │
│                                    ▼                                       │
│                         ┌─────────────────┐                                │
│                         │    06-ADMIN     │                                │
│                         │ (Moderación)    │                                │
│                         └─────────────────┘                                │
│                                    │                                       │
│                                    ▼                                       │
│                         ┌─────────────────┐                                │
│                         │  08-DGII        │                                │
│                         │ (Compliance)    │                                │
│                         └─────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Estadísticas

| Métrica             | Valor |
| ------------------- | ----- |
| Total de documentos | 103   |
| Secciones           | 9     |
| Documentos P0       | ~27   |
| Documentos P1       | ~45   |
| Documentos P2       | ~28   |
| Documentos P3       | ~3    |

---

## ✅ Checklist de Navegación

- [x] Cada sección tiene su `00-INDICE.md`
- [x] Documentos numerados consecutivamente
- [x] Dependencias documentadas
- [x] APIs mapeadas
- [x] Orden de implementación definido
