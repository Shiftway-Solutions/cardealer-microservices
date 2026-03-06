# 02 — Registro de Bases de Datos Personales ante INDOTEL

> **Prioridad:** 🔴 ALTA  
> **Tiempo estimado:** 2-4 semanas  
> **Costo:** Gratuito  
> **Responsable:** DPO (Oficial de Protección de Datos) + Equipo Técnico  
> **Entidad:** Instituto Dominicano de las Telecomunicaciones (INDOTEL)

---

## 1. ¿Por Qué es Necesario?

La **Ley 172-13** sobre Protección Integral de los Datos Personales establece en su **Artículo 22** que toda base de datos que contenga datos personales debe ser registrada ante la autoridad competente. INDOTEL es la entidad encargada de la supervisión y registro de bases de datos personales en República Dominicana.

OKLA recopila, almacena y procesa datos personales de usuarios (compradores, vendedores y dealers), por lo que está **obligada** a registrar sus bases de datos.

### Consecuencias del incumplimiento:

- Multas de **3 a 100 salarios mínimos** (Artículo 29, Ley 172-13)
- Inhabilitación para operar bases de datos
- Responsabilidad civil por daños a los titulares de datos
- Pérdida de confianza de los usuarios

---

## 2. Bases de Datos de OKLA a Registrar

Se debe hacer un inventario completo de todas las bases de datos que contengan datos personales. Para OKLA, estas son:

### Bases de Datos Principales

| #   | Base de Datos               | Servicio            | Datos que Contiene                                       | Clasificación     |
| --- | --------------------------- | ------------------- | -------------------------------------------------------- | ----------------- |
| 1   | **Usuarios/Autenticación**  | AuthService         | Nombre, email, teléfono, contraseña (hash), dirección IP | Datos personales  |
| 2   | **KYC/Identidad**           | KYCService          | Cédula, nombre legal, dirección, foto cédula, selfie     | Datos sensibles   |
| 3   | **Contactos/Mensajes**      | ContactService      | Mensajes entre usuarios, historial de comunicación       | Datos personales  |
| 4   | **Vehículos/Publicaciones** | VehiclesSaleService | Datos del vendedor, fotos, ubicación                     | Datos personales  |
| 5   | **Pagos/Facturación**       | PaymentService      | Datos de tarjeta (tokenizados), historial de pagos       | Datos financieros |
| 6   | **Notificaciones**          | NotificationService | Preferencias, tokens de dispositivo, historial           | Datos personales  |
| 7   | **Auditoría/Logs**          | ErrorService        | IPs, user agents, acciones del usuario                   | Datos de tráfico  |
| 8   | **Media/Archivos**          | MediaService        | Fotos de vehículos, documentos de identidad              | Datos personales  |

### Para cada base de datos se debe registrar:

- Nombre de la base de datos
- Finalidad del tratamiento
- Tipos de datos almacenados
- Categorías de titulares (compradores, vendedores, dealers)
- Medidas de seguridad implementadas
- Si hay transferencia internacional de datos
- Período de retención de los datos
- Base legal para el tratamiento

---

## 3. Proceso de Registro Paso a Paso

### Paso 1: Designar al Responsable (Semana 1)

Antes de iniciar el registro, OKLA debe tener:

- Un **Oficial de Protección de Datos (DPO)** designado (ver documento [03-DESIGNACION-DPO.md](03-DESIGNACION-DPO.md))
- O un representante legal autorizado para el trámite

### Paso 2: Preparar el Inventario de Bases de Datos (Semana 1)

Para cada base de datos, documentar:

```
FICHA DE BASE DE DATOS

Nombre: [Ej: Base de Datos de Usuarios - AuthService]
Responsable: OKLA SRL (o razón social correspondiente)
RNC: [RNC de OKLA]
Dirección: [Dirección fiscal de OKLA]

Finalidad: Gestión de cuentas de usuario, autenticación,
           autorización y seguridad de la plataforma.

Datos recopilados:
  - Nombre completo
  - Correo electrónico
  - Número de teléfono
  - Contraseña (almacenada como hash, no reversible)
  - Dirección IP de registro y último acceso
  - Fecha de registro

Categoría de titulares: Usuarios registrados de la plataforma

Base legal: Consentimiento del titular (Artículo 5, Ley 172-13)

Medidas de seguridad:
  - Cifrado en tránsito (TLS 1.3)
  - Cifrado en reposo (AES-256)
  - Control de acceso basado en roles
  - Auditoría de accesos
  - Respaldos cifrados

Transferencia internacional: Sí — servidores en DigitalOcean (EE.UU.)
Salvaguardas: Cláusulas contractuales estándar con DigitalOcean

Período de retención: Mientras la cuenta esté activa + 12 meses
                      después de la cancelación

Procedimiento de eliminación: Solicitud del titular vía email/plataforma,
                               procesada en máximo 15 días hábiles
```

### Paso 3: Completar el Formulario de Registro (Semana 1-2)

1. Contactar a INDOTEL para obtener el formulario actualizado:
   - **Teléfono:** 809-732-5555
   - **Email:** info@indotel.gob.do
   - **Website:** indotel.gob.do → Sección de Protección de Datos
2. Completar el formulario oficial con los datos de cada base
3. Adjuntar la documentación de la empresa:
   - Copia del RNC
   - Copia del Registro Mercantil
   - Cédula del representante legal
   - Poder de representación (si aplica)
   - Descripción de medidas de seguridad

### Paso 4: Presentar ante INDOTEL (Semana 2-3)

Opciones de presentación:

1. **Presencial:** Llevar documentación a la sede de INDOTEL
   - Dirección: Av. Abraham Lincoln #962, Edificio INDOTEL, Santo Domingo
   - Horario: Lunes a Viernes, 8:30 AM - 4:30 PM
2. **Digital:** Verificar si INDOTEL acepta presentación electrónica
   - Consultar en el portal web o por teléfono

### Paso 5: Seguimiento y Confirmación (Semana 3-4)

1. INDOTEL emitirá un **acuse de recibo** con número de expediente
2. Posibles escenarios:
   - **Aprobación directa:** Registro completado
   - **Observaciones:** INDOTEL solicita información adicional o correcciones
   - **Inspección:** INDOTEL puede solicitar una visita para verificar medidas de seguridad
3. Una vez aprobado, se emite el **certificado de registro**
4. El registro es **público** y puede ser consultado por los titulares

---

## 4. Obligaciones Post-Registro

### Actualización del Registro

- **Obligación:** Notificar a INDOTEL cualquier cambio significativo en las bases de datos
- **Plazo:** Dentro de los 30 días siguientes al cambio
- **Cambios que requieren notificación:**
  - Nueva base de datos
  - Cambio en la finalidad del tratamiento
  - Nuevos tipos de datos recopilados
  - Cambio en medidas de seguridad
  - Nueva transferencia internacional
  - Eliminación de una base de datos

### Derechos de los Titulares

INDOTEL puede canalizar solicitudes de titulares. OKLA debe garantizar:

- **Acceso:** El titular puede solicitar ver sus datos (respuesta en 10 días hábiles)
- **Rectificación:** Corrección de datos inexactos (5 días hábiles)
- **Cancelación:** Eliminación de datos (15 días hábiles)
- **Oposición:** Detener el tratamiento para fines específicos

### Auditorías de INDOTEL

- INDOTEL puede realizar auditorías para verificar cumplimiento
- Mantener documentación actualizada y accesible
- Registros de acceso a datos personales (logs de auditoría)

---

## 5. Recomendaciones Técnicas para el Equipo

### Documentación de Medidas de Seguridad

Preparar un documento técnico que incluya:

```
MEDIDAS DE SEGURIDAD — OKLA

1. CONTROL DE ACCESO
   - Autenticación multi-factor (2FA/MFA) para administradores
   - RBAC (Role-Based Access Control) en todos los servicios
   - Principio de mínimo privilegio

2. CIFRADO
   - En tránsito: TLS 1.3 para todas las comunicaciones
   - En reposo: AES-256 para bases de datos PostgreSQL
   - Contraseñas: bcrypt con salt (factor de costo 12)
   - Tokens: JWT con RS256

3. MONITOREO Y AUDITORÍA
   - Logs centralizados (Serilog/Elasticsearch)
   - Auditoría de acceso a datos personales
   - Alertas de seguridad automatizadas
   - Revisión periódica de logs

4. RESPALDOS
   - Respaldos automáticos diarios
   - Cifrado de respaldos
   - Pruebas de restauración mensuales
   - Retención de respaldos: 30 días

5. INFRAESTRUCTURA
   - Kubernetes con namespaces aislados
   - Network policies entre servicios
   - Secretos gestionados por Kubernetes Secrets
   - Actualizaciones de seguridad periódicas

6. DESARROLLO SEGURO
   - Validación de inputs (SQLi, XSS)
   - CSRF protection
   - Rate limiting
   - Security headers
```

---

## 6. Información de Contacto INDOTEL

| Concepto         | Detalle                                                     |
| ---------------- | ----------------------------------------------------------- |
| **Teléfono**     | 809-732-5555                                                |
| **Website**      | indotel.gob.do                                              |
| **Email**        | info@indotel.gob.do                                         |
| **Dirección**    | Av. Abraham Lincoln #962, Ensanche Serrallés, Santo Domingo |
| **Horario**      | Lunes a Viernes, 8:30 AM - 4:30 PM                          |
| **Departamento** | Dirección de Protección al Usuario y Datos Personales       |

---

## 7. Checklist de Completitud

- [ ] Inventario de bases de datos completado
- [ ] Fichas de cada base de datos preparadas
- [ ] Medidas de seguridad documentadas
- [ ] Formulario de registro obtenido de INDOTEL
- [ ] Formulario completado y revisado por abogado
- [ ] Documentación empresarial reunida
- [ ] Registro presentado ante INDOTEL
- [ ] Acuse de recibo obtenido
- [ ] Certificado de registro recibido
- [ ] Política de privacidad actualizada con número de registro
- [ ] Proceso de actualización periódica establecido
