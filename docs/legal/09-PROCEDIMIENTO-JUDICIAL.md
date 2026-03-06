# 09 — Procedimiento de Respuesta a Requerimientos Judiciales

> **Prioridad:** 🟢 BAJA  
> **Tiempo estimado:** 1-2 semanas (preparar procedimiento)  
> **Costo:** Incluido en retainer de abogado  
> **Responsable:** Abogado + DPO + CTO  
> **Base legal:** Ley 53-07, Artículos 55-56

---

## 1. ¿Por Qué Necesita OKLA Este Procedimiento?

La **Ley 53-07** sobre Crímenes y Delitos de Alta Tecnología establece que los proveedores de servicios de Internet y plataformas digitales tienen obligaciones específicas cuando reciben requerimientos judiciales de datos de usuarios.

### Escenarios posibles:

- Un vehículo publicado en OKLA está involucrado en un delito (robo, lavado)
- Un usuario es investigado por fraude
- Se necesita identificar a un usuario por una estafa en la plataforma
- Un tribunal ordena la preservación de datos para un caso
- Una investigación de lavado de activos involucra transacciones de la plataforma

### Lo que dice la Ley 53-07:

**Artículo 55:** Los proveedores de servicios están obligados a conservar los datos de tráfico por un período mínimo de **90 días**, pudiendo ser extendido hasta un máximo de **2 años** por orden del Ministerio Público.

**Artículo 56:** Los proveedores deben facilitar la información y datos requeridos por autoridades competentes mediante orden judicial debidamente fundamentada.

---

## 2. Datos que OKLA Puede Ser Requerida a Entregar

### Datos de Tráfico (Artículo 55)

Datos generados por el uso de la plataforma:

- Direcciones IP de conexión
- Fechas y horas de acceso
- URLs visitadas
- Dispositivos utilizados (user agent)
- Acciones realizadas (publicar, buscar, contactar)

### Datos de Identificación

- Nombre del usuario
- Correo electrónico
- Número de teléfono
- Dirección registrada
- Datos KYC (cédula, fotos)

### Datos de Contenido

- Publicaciones de vehículos
- Mensajes entre usuarios
- Fotos subidas
- Historial de búsquedas

### Datos Financieros

- Historial de pagos
- Métodos de pago utilizados (tokenizados)
- Facturas emitidas

---

## 3. Procedimiento Interno

### 3.1 Designación de Contacto para Requerimientos Judiciales

Designar a una o dos personas autorizadas para recibir y procesar requerimientos judiciales:

```
CONTACTO PARA REQUERIMIENTOS JUDICIALES

Contacto Principal:
  Nombre: [Abogado de la empresa / Representante Legal]
  Email: legal@okla.do
  Teléfono: [número directo]

Contacto Secundario (en ausencia del principal):
  Nombre: [DPO o Gerente General]
  Email: dpo@okla.do
  Teléfono: [número directo]

IMPORTANTE: Ningún otro empleado está autorizado a recibir o
procesar requerimientos judiciales. Si un empleado recibe una
solicitud, debe remitirla inmediatamente al contacto principal.
```

### 3.2 Flujo del Procedimiento

```
PASO 1: RECEPCIÓN DEL REQUERIMIENTO
│
├── Verificar que sea un documento oficial válido:
│   ├── Orden judicial firmada por juez competente
│   ├── Solicitud del Ministerio Público (Procuraduría)
│   └── Solicitud de la DICAT (Departamento de Investigación
│       de Crímenes y Delitos de Alta Tecnología)
│
├── Si NO es un documento válido:
│   └── Rechazar y solicitar orden judicial apropiada
│
└── Si SÍ es válido: → PASO 2

PASO 2: REGISTRO Y EVALUACIÓN (24 horas)
│
├── Registrar en el Log de Requerimientos Judiciales:
│   ├── Número de caso/expediente
│   ├── Autoridad solicitante
│   ├── Fecha de recepción
│   ├── Datos solicitados
│   └── Plazo de respuesta
│
├── Evaluar con el abogado:
│   ├── ¿La orden es válida y específica?
│   ├── ¿Los datos solicitados son proporcionales?
│   ├── ¿Hay motivo para apelar u objetar?
│   └── ¿Hay algún conflicto con la Ley 172-13 (privacidad)?
│
└── → PASO 3

PASO 3: EXTRACCIÓN DE DATOS (1-5 días, según complejidad)
│
├── CTO/Equipo Técnico extrae los datos solicitados:
│   ├── Usar consultas específicas a la base de datos
│   ├── Documentar exactamente qué se extrajo
│   ├── Cifrar los datos antes de transmitir
│   └── NO extraer más datos de los solicitados
│
├── DPO revisa que solo se entreguen datos pertinentes
│
└── → PASO 4

PASO 4: PREPARACIÓN DE LA RESPUESTA (1-2 días)
│
├── Abogado prepara carta de respuesta formal
├── Se adjuntan los datos en formato legible
├── Se incluye descripción técnica de los datos
└── → PASO 5

PASO 5: ENTREGA (dentro del plazo)
│
├── Entregar la respuesta por el medio especificado:
│   ├── Entrega personal en el tribunal/fiscalía
│   ├── Correo certificado
│   └── Medio electrónico seguro (si autorizado)
│
├── Obtener acuse de recibo
└── → PASO 6

PASO 6: DOCUMENTACIÓN POST-ENTREGA
│
├── Archivar copia de todo:
│   ├── Orden judicial original
│   ├── Datos entregados
│   ├── Carta de respuesta
│   └── Acuse de recibo
│
├── Retener por mínimo 5 años
└── Notificar al usuario (si es legalmente posible — ver Paso 7)

PASO 7: NOTIFICACIÓN AL USUARIO (si aplica)
│
├── Verificar si la orden incluye restricción de notificación
│   ├── Si SÍ hay restricción: NO notificar al usuario
│   └── Si NO hay restricción: Notificar al usuario que sus
│       datos fueron entregados por requerimiento judicial
│
└── Documentar la decisión de notificar o no
```

---

## 4. Plantilla de Carta de Respuesta

```
[MEMBRETE DE OKLA]

[Fecha]

Señor/a
[Nombre del Juez/Fiscal]
[Cargo]
[Tribunal/Fiscalía]
[Dirección]

Ref: Respuesta a Requerimiento Judicial
     Caso/Expediente No.: [número]
     Orden de fecha: [fecha de la orden]

Distinguido/a [título]:

En cumplimiento con la orden [judicial/del Ministerio Público]
de fecha [fecha], recibida por esta empresa el [fecha de recepción],
mediante la cual se requiere la entrega de datos relativos a
[descripción general], procedemos a dar respuesta en los
siguientes términos:

1. DATOS PROPORCIONADOS
   Conforme a lo solicitado, adjuntamos la siguiente información:

   a) [Descripción de los datos entregados - Ej: "Datos de
      registro del usuario identificado con el correo electrónico
      xxxx@xxxx.com, incluyendo nombre, teléfono y dirección IP
      de registro"]

   b) [Si aplica: "Historial de actividad del usuario en la
      plataforma durante el período del [fecha] al [fecha]"]

   c) [Si aplica: "Publicaciones de vehículos realizadas por
      el usuario durante el período solicitado"]

2. FORMATO DE ENTREGA
   Los datos se entregan en formato [PDF/CSV/JSON], contenidos
   en [medio: USB cifrado / sobre sellado / correo seguro].

3. OBSERVACIONES
   [Si aplica: "No se encontraron registros para el período
   del [fecha] al [fecha] solicitado"]

   [Si aplica: "Algunos datos solicitados no están disponibles
   ya que exceden nuestro período de retención de [X] días"]

4. CONFIDENCIALIDAD
   La empresa [RAZÓN SOCIAL DE OKLA] ha tratado este
   requerimiento con la debida confidencialidad y los datos
   han sido extraídos y manejados exclusivamente por personal
   autorizado.

5. BASE LEGAL
   Esta respuesta se emite en cumplimiento con los Artículos 55
   y 56 de la Ley 53-07 sobre Crímenes y Delitos de Alta
   Tecnología, y conforme a las disposiciones de la Ley 172-13
   sobre Protección de Datos Personales.

Sin otro particular, quedamos a su disposición para cualquier
aclaración adicional.

Atentamente,

_________________________
[Nombre del Representante Legal]
[Cargo]
[RAZÓN SOCIAL DE OKLA]
RNC: [número]
```

---

## 5. Retención de Datos para Cumplimiento Legal

### Períodos de Retención

| Tipo de Dato                 | Retención Mínima                          | Base Legal         |
| ---------------------------- | ----------------------------------------- | ------------------ |
| Datos de tráfico (IPs, logs) | **90 días** (extensible a 2 años)         | Ley 53-07, Art. 55 |
| Datos de usuario (cuenta)    | Mientras la cuenta esté activa + 12 meses | Ley 172-13         |
| Datos KYC                    | 5 años (si sujeto obligado AML)           | Ley 155-17         |
| Datos financieros            | 10 años                                   | Código Tributario  |
| Requerimientos judiciales    | 5 años mínimo                             | Buenas prácticas   |

### Configuración Técnica Actual

- Verificar que los logs de auditoría (ErrorService) retengan datos por al menos 90 días
- Verificar que las copias de seguridad de base de datos permitan recuperar datos históricos
- Implementar política de retención diferenciada si no existe

---

## 6. Preservación de Datos (Litigation Hold)

Si se recibe una orden de preservación de datos (antes del requerimiento formal):

1. **Inmediatamente** suspender cualquier proceso de eliminación automática para los datos relevantes
2. Notificar al equipo técnico para que detenga purges/cleanups
3. Marcar los datos como "preservados" en el sistema
4. Documentar la fecha de inicio de la preservación
5. Mantener hasta recibir orden de liberación

---

## 7. Entidades que Pueden Requerir Datos

| Entidad                           | Tipo de Solicitud       | Responder a                            |
| --------------------------------- | ----------------------- | -------------------------------------- |
| Tribunales (jueces)               | Orden judicial          | Obligatorio                            |
| Ministerio Público (Procuraduría) | Solicitud fiscal        | Obligatorio (con orden)                |
| DICAT (Policía Nacional)          | Solicitud investigativa | Solo con orden judicial                |
| INDOTEL                           | Solicitud regulatoria   | Obligatorio (dentro de su competencia) |
| UAF                               | Solicitud AML           | Obligatorio (si aplica)                |
| Particulares/Abogados             | Solicitud privada       | NO — debe ser vía orden judicial       |

> **⚠️ IMPORTANTE:** OKLA **no debe** entregar datos de usuarios a solicitudes de particulares, abogados privados o empresas sin una orden judicial válida.

---

## 8. Información de Contacto Relevante

| Entidad                  | Teléfono               | Ubicación                              |
| ------------------------ | ---------------------- | -------------------------------------- |
| **DICAT**                | 809-682-2151 ext. 2449 | Palacio de la Policía Nacional         |
| **Procuraduría General** | 809-533-3522           | Av. Jiménez Moya, Centro de los Héroes |
| **INDOTEL**              | 809-732-5555           | Av. Abraham Lincoln #962               |

---

## 9. Checklist de Completitud

- [ ] Contacto para requerimientos judiciales designado
- [ ] Email legal@okla.do configurado y monitoreado
- [ ] Procedimiento interno documentado y aprobado
- [ ] Plantilla de carta de respuesta preparada
- [ ] Equipo técnico capacitado en extracción de datos
- [ ] Política de retención de datos verificada (mínimo 90 días logs)
- [ ] Log de requerimientos judiciales creado
- [ ] Abogado informado del procedimiento
- [ ] Procedimiento de preservación de datos (litigation hold) documentado
- [ ] Equipo informado de que NO deben entregar datos sin autorización
