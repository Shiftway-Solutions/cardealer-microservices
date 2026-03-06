# 10 — Plan de Contingencia de Datos (Cese de Operaciones)

> **Prioridad:** 🟢 BAJA  
> **Tiempo estimado:** 2-4 semanas (preparar el plan)  
> **Costo:** Incluido en retainer de abogado  
> **Responsable:** Gerencia General + DPO + CTO  
> **Base legal:** Ley 172-13 (Protección de Datos), buenas prácticas internacionales

---

## 1. ¿Qué es un Plan de Contingencia de Datos?

Es un documento que establece qué sucede con los datos personales de los usuarios si OKLA **cesa operaciones** de forma permanente. La Ley 172-13 establece que los responsables de bases de datos deben garantizar los derechos de los titulares en todo momento, incluyendo situaciones de cierre.

### ¿Por qué es necesario?

- Los usuarios tienen derecho a saber qué pasará con sus datos
- Es una obligación legal bajo la Ley 172-13
- Genera confianza en la plataforma
- Es una buena práctica exigida por estándares internacionales
- INDOTEL puede requerirlo como parte del registro de bases de datos

### Escenarios de cese:

1. **Cierre voluntario:** Los fundadores deciden cerrar la empresa
2. **Quiebra o insolvencia:** La empresa no puede continuar financieramente
3. **Adquisición/Fusión:** Otra empresa adquiere OKLA
4. **Orden regulatoria:** Una autoridad ordena el cese de operaciones
5. **Fuerza mayor:** Desastre que imposibilita la continuidad

---

## 2. El Plan de Contingencia

### 2.1 Principios Fundamentales

1. **Transparencia:** Los usuarios serán informados con suficiente anticipación
2. **Minimización:** Se retendrán datos solo el tiempo legalmente requerido
3. **Seguridad:** Los datos seguirán protegidos hasta su eliminación final
4. **Portabilidad:** Los usuarios podrán exportar sus datos antes del cierre
5. **Eliminación segura:** Los datos serán eliminados de forma irreversible

---

### 2.2 Fases del Plan

```
FASE 1: DECISIÓN Y NOTIFICACIÓN (Día 0 - Día 30)
│
├── Día 0: Se toma la decisión de cese
│   ├── Documentar la decisión formalmente
│   ├── Notificar a INDOTEL
│   └── Notificar al DPO
│
├── Día 1-3: Preparar comunicaciones
│   ├── Preparar email de notificación a todos los usuarios
│   ├── Preparar aviso en la plataforma (banner prominente)
│   ├── Preparar comunicado de prensa (si aplica)
│   └── Preparar FAQ para usuarios
│
├── Día 3-5: Notificar a usuarios
│   ├── Email a TODOS los usuarios registrados
│   ├── Banner en la plataforma
│   ├── Notificación push (si aplica)
│   └── SMS a usuarios con teléfono registrado
│
├── Día 5: Notificar a entidades regulatorias
│   ├── INDOTEL (registro de bases de datos)
│   ├── DGII (obligaciones fiscales)
│   ├── ProConsumidor (si está registrado)
│   └── UAF (si es sujeto obligado)
│
└── Días 5-30: Período de gracia
    ├── La plataforma sigue operativa en modo limitado
    ├── No se aceptan nuevos registros
    ├── No se procesan nuevos pagos
    └── Los usuarios pueden exportar sus datos

FASE 2: EXPORTACIÓN DE DATOS (Día 30 - Día 60)
│
├── Habilitar herramienta de exportación masiva
│   ├── Datos personales (perfil, email, teléfono)
│   ├── Publicaciones de vehículos (texto, fotos)
│   ├── Mensajes (historial de conversaciones)
│   ├── Historial de pagos/facturas
│   └── Datos KYC (si el usuario lo solicita)
│
├── Formato de exportación:
│   ├── JSON (datos estructurados)
│   ├── CSV (datos tabulares)
│   └── ZIP (fotos y documentos)
│
├── Notificación recordatorio:
│   ├── Día 30: "Le quedan 30 días para exportar sus datos"
│   ├── Día 45: "Le quedan 15 días para exportar sus datos"
│   └── Día 55: "Le quedan 5 días para exportar sus datos"
│
└── Registrar quiénes exportaron sus datos

FASE 3: CIERRE DE SERVICIOS (Día 60 - Día 90)
│
├── Día 60: Desactivar la plataforma
│   ├── Reemplazar con página informativa de cierre
│   ├── Incluir enlace para solicitud de datos residuales
│   └── Incluir información de contacto
│
├── Día 60-75: Procesar solicitudes pendientes
│   ├── Responder solicitudes de exportación tardías
│   ├── Emitir facturas/comprobantes pendientes
│   └── Procesar reembolsos si aplica
│
└── Día 75-90: Preparar para eliminación
    ├── Verificar que todos los usuarios fueron notificados
    ├── Documentar datos que NO se pueden eliminar (obligaciones legales)
    └── Preparar proceso de eliminación

FASE 4: RETENCIÓN LEGAL Y ELIMINACIÓN (Día 90 - Día 180+)
│
├── Datos que DEBEN retenerse (no se eliminan aún):
│   ├── Datos fiscales (facturas, registros DGII) → 10 años
│   ├── Datos de tráfico (Ley 53-07) → Hasta 2 años
│   ├── Datos AML/KYC (si sujeto obligado) → 5 años
│   └── Documentación de requerimientos judiciales → 5 años
│
├── Datos que SE ELIMINAN:
│   ├── Perfiles de usuario (datos personales)
│   ├── Mensajes entre usuarios
│   ├── Fotos de vehículos
│   ├── Preferencias y configuraciones
│   ├── Tokens de autenticación
│   └── Cookies y datos de sesión
│
├── Método de eliminación:
│   ├── Bases de datos: DROP + sobrescritura del espacio
│   ├── Archivos (S3/Spaces): Eliminación + política de expiración
│   ├── Respaldos: Eliminación de todas las copias
│   ├── Logs: Eliminación después del período de retención
│   └── Certificación de destrucción (por tercero si posible)
│
└── Documentar todo el proceso de eliminación
```

---

### 2.3 Notificación a Usuarios

#### Email de Notificación Inicial

```
Asunto: Aviso importante sobre el futuro de OKLA

Estimado/a [Nombre],

Le informamos que [RAZÓN SOCIAL DE OKLA], operadora de la
plataforma OKLA (okla.do), ha tomado la decisión de cesar
sus operaciones a partir del [fecha de cierre].

¿QUÉ SIGNIFICA ESTO PARA USTED?

1. SUS DATOS
   Tiene hasta el [fecha límite] para exportar sus datos
   personales y contenido desde la plataforma. Después de
   esa fecha, sus datos serán eliminados de forma permanente.

2. CÓMO EXPORTAR SUS DATOS
   Ingrese a okla.do/mi-cuenta/exportar-datos para descargar:
   - Su perfil y datos personales
   - Sus publicaciones de vehículos
   - Su historial de mensajes
   - Sus facturas y comprobantes

3. REEMBOLSOS
   [Si aplica: "Si tiene un plan de suscripción activo,
   recibirá un reembolso proporcional por el período no
   utilizado. El reembolso se procesará automáticamente al
   mismo medio de pago original."]

4. PUBLICACIONES ACTIVAS
   Sus publicaciones de vehículos permanecerán visibles hasta
   el [fecha]. Le recomendamos buscar plataformas alternativas
   para sus listados.

5. PROTECCIÓN DE SUS DATOS
   Sus datos personales seguirán protegidos conforme a la
   Ley 172-13 hasta su eliminación final. Los datos fiscales
   se conservarán según lo requerido por ley.

CRONOGRAMA:
- [Fecha]: Último día para exportar datos
- [Fecha]: Cierre de la plataforma
- [Fecha]: Eliminación final de datos

CONTACTO:
Para cualquier pregunta, comuníquese con:
- Email: soporte@okla.do (disponible hasta [fecha])
- Teléfono: [número] (disponible hasta [fecha])

Agradecemos su confianza durante este tiempo.

Atentamente,
El equipo de OKLA
```

---

### 2.4 Transferencia a Entidad Sucesora (Adquisición)

Si OKLA es adquirida por otra empresa:

1. **Notificar a los usuarios** sobre la transferencia
2. **Obtener nuevo consentimiento** si la finalidad del tratamiento cambia
3. **La entidad sucesora** debe:
   - Asumir todas las obligaciones de protección de datos
   - Respetar los consentimientos existentes
   - Actualizar la Política de Privacidad
   - Registrarse ante INDOTEL como nuevo responsable
4. **Si el usuario no consiente** la transferencia:
   - Sus datos deben ser eliminados
   - Se le deben entregar sus datos exportados

```
MODELO DE NOTIFICACIÓN DE TRANSFERENCIA

Estimado/a [Nombre],

Le informamos que [RAZÓN SOCIAL DE OKLA] ha sido adquirida
por [NOMBRE DE LA EMPRESA ADQUIRENTE], RNC [número].

A partir del [fecha], sus datos personales serán tratados
por [EMPRESA ADQUIRENTE] bajo las siguientes condiciones:
[Descripción]

Si está de acuerdo con la transferencia de sus datos, no
necesita hacer nada.

Si NO desea que sus datos sean transferidos, tiene hasta
el [fecha] para:
1. Exportar sus datos: okla.do/mi-cuenta/exportar-datos
2. Solicitar la eliminación: okla.do/mi-cuenta/eliminar-cuenta

Después de [fecha], si no ha tomado acción, sus datos serán
transferidos a [EMPRESA ADQUIRENTE].
```

---

## 3. Actualización de la Política de Privacidad

Incluir en la Política de Privacidad:

```
CONTINUIDAD Y CESE DE OPERACIONES

En caso de que OKLA cese sus operaciones:

1. Serán notificados con al menos 60 días de anticipación.
2. Tendrán la oportunidad de exportar todos sus datos personales.
3. Sus datos serán eliminados de forma segura e irreversible
   después del período de gracia, excepto aquellos que debamos
   retener por obligación legal.
4. Las autoridades regulatorias correspondientes serán
   notificadas.

En caso de adquisición o fusión:
1. Serán notificados sobre la transferencia de sus datos.
2. Podrán oponerse a la transferencia y solicitar la
   eliminación de sus datos.
3. La entidad adquirente estará obligada a respetar esta
   política de privacidad o obtener su nuevo consentimiento.
```

---

## 4. Responsabilidades por Rol

| Rol                  | Responsabilidad                                                            |
| -------------------- | -------------------------------------------------------------------------- |
| **Gerencia General** | Tomar la decisión, aprobar el plan, comunicación pública                   |
| **DPO**              | Supervisar cumplimiento de protección de datos, notificar a INDOTEL        |
| **CTO**              | Ejecutar exportación, eliminación técnica, certificar destrucción          |
| **Abogado**          | Verificar cumplimiento legal, gestionar obligaciones fiscales y judiciales |
| **Contador**         | Emitir últimas facturas, procesar reembolsos, retener datos fiscales       |
| **Soporte**          | Atender consultas de usuarios durante el período de transición             |

---

## 5. Pruebas del Plan

Se recomienda realizar una **prueba anual** del plan:

- Verificar que la herramienta de exportación funcione
- Verificar que se puede eliminar datos de forma segura
- Actualizar los contactos y responsables
- Revisar cambios regulatorios que afecten el plan
- Actualizar la lista de datos con retención legal obligatoria

---

## 6. Checklist de Completitud

- [ ] Plan de contingencia redactado y aprobado por la gerencia
- [ ] Herramienta de exportación de datos disponible para usuarios
- [ ] Plantillas de notificación preparadas (email, SMS, banner)
- [ ] Política de Privacidad actualizada con sección de cese
- [ ] Procedimiento de eliminación segura documentado
- [ ] Datos con retención legal obligatoria identificados y catalogados
- [ ] Proceso de transferencia a entidad sucesora documentado
- [ ] Responsabilidades por rol asignadas
- [ ] Prueba anual del plan programada
- [ ] Abogado ha revisado y aprobado el plan
- [ ] DPO informado y capacitado en la ejecución del plan
