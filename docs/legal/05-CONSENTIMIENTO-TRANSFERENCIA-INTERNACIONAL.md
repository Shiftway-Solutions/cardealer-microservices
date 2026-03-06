# 05 — Transferencia Internacional de Datos Personales

> **Prioridad:** 🟡 MEDIA  
> **Tiempo estimado:** 2-4 semanas  
> **Costo:** RD$5,000-15,000 (revisión legal de contratos)  
> **Responsable:** DPO + Equipo Legal + DevOps  
> **Base legal:** Ley 172-13, Artículo 27

---

## 1. Situación Actual de OKLA

### ¿Qué datos se transfieren internacionalmente?

Todos los datos personales de los usuarios de OKLA se almacenan en servidores de **DigitalOcean** ubicados en **Estados Unidos**. Esto constituye una **transferencia internacional de datos** desde República Dominicana hacia Estados Unidos.

### Datos que se transfieren:

- Datos de autenticación (nombre, email, teléfono)
- Datos de identidad/KYC (cédula, fotos, dirección)
- Datos de vehículos y publicaciones
- Mensajes entre usuarios
- Datos de pago (tokenizados)
- Logs y auditoría
- Archivos multimedia (fotos de vehículos, documentos)

### Lo que ya se implementó en código:

- ✅ Checkbox de consentimiento en el formulario de registro
- ✅ Texto informando que los datos se almacenan en servidores en EE.UU.
- ✅ Cifrado en tránsito (TLS 1.3) y en reposo (AES-256)

### Lo que falta (procesos administrativos):

- ❌ Cláusulas Contractuales Estándar (SCC) con DigitalOcean
- ❌ Documentación formal de las salvaguardas
- ❌ Evaluación de impacto de la transferencia (TIA)
- ❌ Actualización detallada de la Política de Privacidad

---

## 2. Marco Legal: Ley 172-13, Artículo 27

### Lo que dice la ley:

La transferencia de datos personales a países que no cuenten con un **nivel adecuado de protección** solo puede realizarse cuando:

1. El titular haya dado su **consentimiento expreso** e inequívoco ✅ (ya implementado)
2. La transferencia sea necesaria para la **ejecución de un contrato** entre el titular y el responsable
3. Se establezcan **garantías suficientes** de protección de datos

### ¿Tiene EE.UU. un "nivel adecuado de protección"?

No existe un acuerdo formal de adecuación entre RD y EE.UU. similar al EU-US Privacy Shield. Por lo tanto, OKLA debe implementar **salvaguardas adicionales**.

---

## 3. Acciones Requeridas

### 3.1 Cláusulas Contractuales Estándar (SCC) con DigitalOcean

Las SCC son cláusulas contractuales que obligan al receptor de los datos (DigitalOcean) a proteger los datos conforme a los estándares del país de origen.

#### Paso 1: Revisar el Contrato Actual con DigitalOcean

1. Acceder a la cuenta de DigitalOcean
2. Ir a **Settings** → **Legal** → **Terms of Service**
3. Revisar el **Data Processing Agreement (DPA)** de DigitalOcean
4. DigitalOcean ya incluye un DPA estándar: [digitalocean.com/legal/data-processing-agreement](https://www.digitalocean.com/legal/data-processing-agreement)
5. Verificar si el DPA incluye cláusulas de transferencia internacional

#### Paso 2: Firmar/Aceptar el DPA de DigitalOcean

1. Si DigitalOcean ofrece un DPA con SCC:
   - Revisarlo con el abogado
   - Aceptarlo formalmente (generalmente se acepta desde el panel de control)
   - Guardar copia firmada/aceptada
2. Si el DPA estándar es insuficiente:
   - Contactar a DigitalOcean para negociar cláusulas adicionales
   - Solicitar la firma de SCC específicas

#### Paso 3: Preparar Cláusulas Adicionales (si es necesario)

Si el DPA de DigitalOcean no cubre suficientemente las necesidades:

```
CLÁUSULAS CONTRACTUALES ESTÁNDAR COMPLEMENTARIAS

Entre:
EXPORTADOR DE DATOS: [RAZÓN SOCIAL DE OKLA], RNC [número]
                     (en adelante "OKLA")

IMPORTADOR DE DATOS: DigitalOcean, LLC
                     101 Avenue of the Americas, 10th Floor
                     New York, NY 10013, USA

CLÁUSULA 1: FINALIDAD
Los datos se transfieren exclusivamente para el almacenamiento
y procesamiento necesario para la operación de la plataforma
OKLA conforme a los servicios contratados.

CLÁUSULA 2: OBLIGACIONES DEL IMPORTADOR
DigitalOcean se compromete a:
a) Tratar los datos únicamente según las instrucciones de OKLA
b) Mantener medidas de seguridad adecuadas
c) No transferir datos a terceros sin autorización
d) Notificar brechas de seguridad en un plazo de 72 horas
e) Eliminar los datos al finalizar el servicio
f) Cooperar con auditorías de cumplimiento

CLÁUSULA 3: DERECHOS DE LOS TITULARES
El importador asistirá al exportador en el cumplimiento de las
solicitudes de acceso, rectificación y cancelación de los
titulares de los datos.

CLÁUSULA 4: MEDIDAS DE SEGURIDAD
[Anexo con las medidas de seguridad de DigitalOcean]

CLÁUSULA 5: LEY APLICABLE
Estas cláusulas se regirán por las leyes de la República
Dominicana en lo que respecta a la protección de datos.
```

### 3.2 Evaluación de Impacto de Transferencia (TIA)

Documentar formalmente la evaluación:

```
EVALUACIÓN DE IMPACTO DE TRANSFERENCIA INTERNACIONAL DE DATOS

Fecha: [fecha]
Responsable: [DPO]

1. DESCRIPCIÓN DE LA TRANSFERENCIA
   - Origen: República Dominicana (OKLA)
   - Destino: Estados Unidos (DigitalOcean - región NYC/SFO)
   - Datos: [lista de tipos de datos]
   - Volumen: Aproximadamente [X] registros de usuarios
   - Frecuencia: Continua (en tiempo real)

2. BASE LEGAL
   - Consentimiento expreso del titular (Art. 27, Ley 172-13)
   - Ejecución del contrato de servicio
   - Cláusulas contractuales estándar con el procesador

3. EVALUACIÓN DE RIESGOS
   Riesgo: Acceso gubernamental a datos (FISA, CLOUD Act)
   Mitigación: Cifrado en reposo, DigitalOcean no tiene acceso
               a datos cifrados por la aplicación
   Probabilidad: Baja
   Impacto: Medio

   Riesgo: Brecha de seguridad en DigitalOcean
   Mitigación: DPA con notificación en 72h, cifrado, respaldos
   Probabilidad: Baja
   Impacto: Alto

   Riesgo: Cambio regulatorio en EE.UU.
   Mitigación: Monitoreo, plan de migración alternativo
   Probabilidad: Baja
   Impacto: Medio

4. SALVAGUARDAS IMPLEMENTADAS
   - Cifrado en tránsito (TLS 1.3)
   - Cifrado en reposo (AES-256)
   - Control de acceso estricto (Kubernetes RBAC)
   - DPA firmado con DigitalOcean
   - Consentimiento explícito del titular
   - Capacidad de migración a otra región

5. CONCLUSIÓN
   La transferencia es aceptable dado el nivel de salvaguardas
   implementadas y el consentimiento de los titulares.

6. PRÓXIMA REVISIÓN: [fecha + 12 meses]
```

### 3.3 Actualización de la Política de Privacidad

Agregar o actualizar la sección de transferencia internacional:

```
TRANSFERENCIA INTERNACIONAL DE DATOS

Para proporcionarle nuestros servicios, sus datos personales son
transferidos y almacenados en servidores ubicados en Estados Unidos,
operados por DigitalOcean, LLC.

BASE LEGAL: Esta transferencia se realiza con su consentimiento
expreso, otorgado al momento de registrarse en la plataforma,
conforme al Artículo 27 de la Ley 172-13.

SALVAGUARDAS: Hemos implementado las siguientes medidas para
proteger sus datos:
1. Cláusulas contractuales estándar con DigitalOcean
2. Cifrado de datos en tránsito y en reposo
3. Control de acceso estricto a los datos
4. Auditorías periódicas de seguridad
5. Respaldos cifrados

PROVEEDOR: DigitalOcean, LLC
UBICACIÓN: Nueva York / San Francisco, Estados Unidos
DPA: Vigente y disponible bajo solicitud

DERECHOS: Usted puede revocar su consentimiento en cualquier
momento. Sin embargo, esto implica la cancelación de su cuenta,
ya que los servicios no pueden prestarse sin el procesamiento
de datos en nuestros servidores.

Para más información, contacte a nuestro DPO: dpo@okla.do
```

---

## 4. Opciones Alternativas de Infraestructura

### Opción A: Migrar a DigitalOcean Miami (Recomendado)

- DigitalOcean no tiene datacenter en RD, pero **Miami** es la región más cercana
- Menor latencia para usuarios dominicanos
- Sigue siendo transferencia internacional, pero con menor distancia
- **Acción:** Evaluar migración de la región actual a NYC3 → Miami (si disponible)

### Opción B: Proveedor de Cloud Local

- Verificar si existen proveedores de cloud en RD con certificaciones adecuadas
- Generalmente más caros y con menor nivel de servicio
- Eliminaría la necesidad de transferencia internacional
- **No recomendado** actualmente por limitaciones de infraestructura local

### Opción C: Modelo Híbrido

- Datos sensibles (KYC, cédulas) en servidor local o región cercana
- Datos operativos (vehículos, publicaciones) en DigitalOcean
- Mayor complejidad técnica
- **Considerar** si hay presión regulatoria

---

## 5. Información de Contacto

| Concepto                         | Detalle                                          |
| -------------------------------- | ------------------------------------------------ |
| **INDOTEL** (regulador datos RD) | 809-732-5555, indotel.gob.do                     |
| **DigitalOcean Legal**           | digitalocean.com/legal                           |
| **DigitalOcean DPA**             | digitalocean.com/legal/data-processing-agreement |
| **DigitalOcean Support**         | Panel de control → Support Ticket                |

---

## 6. Checklist de Completitud

- [ ] DPA de DigitalOcean revisado por abogado
- [ ] DPA firmado/aceptado formalmente
- [ ] SCC adicionales negociadas (si necesario)
- [ ] Evaluación de Impacto de Transferencia (TIA) completada
- [ ] Política de Privacidad actualizada con detalles de transferencia
- [ ] Consentimiento en registro verificado (texto completo y claro)
- [ ] Documentación archivada y accesible al DPO
- [ ] Evaluación de migración a región más cercana realizada
- [ ] Próxima revisión anual programada
