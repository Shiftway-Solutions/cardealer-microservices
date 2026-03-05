# 🏦 Guía de Obtención de Sandbox AZUL

**Fecha:** Enero 8, 2026  
**Estado:** 📋 PROCESO DOCUMENTADO  
**Audiencia:** Equipo de Desarrollo OKLA

---

## 📋 Resumen

Esta guía detalla el proceso completo para obtener credenciales de ambiente de pruebas (Sandbox) de AZUL y configurarlas en el BillingService de OKLA Marketplace.

**Tiempo estimado:** 3-5 días hábiles  
**Costo:** Gratis (ambiente de pruebas)

---

## 🎯 Objetivos

1. ✅ Obtener credenciales de prueba de AZUL
2. ✅ Configurar credenciales en BillingService
3. ✅ Probar integración con Payment Page
4. ✅ Validar transacciones de prueba
5. ✅ Documentar resultados

---

## 📞 PASO 1: Contacto Inicial con AZUL

### Datos de Contacto

| Canal                 | Información                      | Horario        |
| --------------------- | -------------------------------- | -------------- |
| **Email Principal**   | solucionesintegradas@azul.com.do | L-V 8:00-17:00 |
| **Email Alternativo** | vozdelcliente@azul.com.do        | L-V 8:00-17:00 |
| **Teléfono**          | 809-544-2985                     | L-V 8:00-17:00 |
| **Portal**            | https://dev.azul.com.do          | 24/7           |
| **WhatsApp Business** | +1 809-544-AZUL (2985)           | L-V 8:00-17:00 |

### Correo de Solicitud (Template)

```
Asunto: Solicitud de Credenciales Sandbox - OKLA Clasificados de Vehículos (Proyecto en Desarrollo)

Estimado Equipo de AZUL,

Me dirijo a ustedes en nombre de OKLA (okla.com.do), un proyecto de plataforma
de clasificados automotrices que estamos desarrollando para el mercado dominicano para publicacion de vehiculos para comprar y ventas

Solicitamos credenciales para el ambiente de pruebas (Sandbox) de AZUL
Payment Gateway para integrar y probar el procesamiento de pagos antes de
nuestro lanzamiento oficial previsto para Q1 2026.

INFORMACIÓN DE LA EMPRESA:
- Razón Social: OKLA SRL (en proceso de constitución)
- RNC: [EN TRÁMITE]
- Sitio Web: https://okla.com.do (en desarrollo)
- Tipo de Negocio: Portal de Clasificados Automotrices
- Modelo de Negocio: Cobro por publicación de anuncios de vehículos
- Estado: Proyecto en fase de desarrollo y testing

MODELO DE TRANSACCIONES PLANIFICADO:
Procesaremos pagos de dealers y vendedores individuales por:
- Publicaciones individuales: $29 USD por listing (vendedores individuales)
- Planes mensuales para dealers: $49-$299 USD/mes
- Paquetes de publicaciones destacadas: $10-20 USD
- Renovaciones automáticas mensuales

VOLUMEN ESTIMADO POST-LANZAMIENTO:
- Primeros 3 meses: 20-30 transacciones/mes
- 6 meses: 50-75 transacciones/mes
- 12 meses: 100+ transacciones/mes
- Ticket promedio: $50-150 USD



CASOS DE USO:
1. Vendedor individual publica un vehículo → Pago único de $29 USD
2. Dealer se suscribe a plan mensual → Pago recurrente de $49-$299 USD
3. Destacar publicación → Pago adicional de $10-20 USD
4. Renovación de suscripción → Cargo automático mensual

CREDENCIALES REQUERIDAS PARA AMBIENTE SANDBOX:
1. MerchantId (Test)
2. MerchantName
3. AuthKey
4. Auth1 y Auth2 (para Webservices API - DataVault)
5. Acceso al portal de pruebas

OBJETIVO:
Implementar y probar completamente la integración con AZUL durante el período
de desarrollo (Enero-Febrero 2026) para solicitar credenciales de producción
antes del lanzamiento oficial.

INFORMACIÓN DE CONTACTO:
- Nombre: Gregory Moreno
- Cargo: Lead Developer
- Email: gmoreno@okla.com.do
- Teléfono: [Tu Teléfono]

Agradecemos su pronta respuesta y apoyo en esta etapa de desarrollo.

Saludos cordiales,
Gregory Moreno/payment/azul                → AzulPaymentPage
/payment/azul/approved       → AzulApprovedPage  
/payment/azul/declined       → AzulDeclinedPage
/payment/azul/cancelled      → AzulCancelledPage
Lead Developer
OKLA - Clasificados Automotrices
https://okla.com.do
```

---

## 📝 PASO 2: Documentación Requerida

AZUL típicamente solicita los siguientes documentos:

### Para Empresas Registradas

| Documento                 | Descripción                             | Formato     |
| ------------------------- | --------------------------------------- | ----------- |
| **RNC**                   | Registro Nacional de Contribuyentes     | PDF         |
| **Constitutiva**          | Documento de constitución de la empresa | PDF         |
| **Cédula Representante**  | Cédula del representante legal          | PDF         |
| **Comprobante Domicilio** | Factura de luz/agua/teléfono            | PDF         |
| **Formato KYC**           | Formulario Know Your Customer de AZUL   | PDF firmado |

### Para Testing/Desarrollo (Simplificado)

Para ambiente de pruebas, AZUL puede proporcionar credenciales con:

- ✅ Solicitud formal por email
- ✅ Información básica de la empresa
- ✅ Descripción del proyecto
- ✅ Casos de uso

**Nota:** Para producción sí se requerirá documentación completa.

---

## 🔐 PASO 3: Credenciales que Recibirás

### Credenciales de Payment Page

```json
{
  "MerchantId": "39038540035", // Ejemplo
  "MerchantName": "OKLA Marketplace",
  "AuthKey": "E2A7A7A7E4F8...", // SHA-512 key (64 chars)
  "Environment": "Test",
  "PaymentPageUrl": "https://pruebas.azul.com.do/PaymentPage/"
}
```

### Credenciales de Webservices API (Opcional)

```json
{
  "Auth1": "testuser1",
  "Auth2": "testpassword123",
  "WebservicesUrl": "https://pruebas.azul.com.do/webservices/JSON/Default.aspx"
}
```

### Acceso al Portal de Merchant

- **URL:** https://azulmerchant.azul.com.do/
- **Usuario:** [Proporcionado por AZUL]
- **Contraseña:** [Proporcionado por AZUL]

**Funciones del Portal:**

- Ver transacciones en tiempo real
- Consultar reportes
- Gestionar refunds/voids
- Ver dashboards de ventas

---

## ⚙️ PASO 4: Configuración en el Sistema

### 4.1. Actualizar appsettings.json

```bash
# Ubicación del archivo
cd backend/BillingService/BillingService.Api
nano appsettings.json
```

**Contenido a actualizar:**

```json
{
  "Azul": {
    "MerchantId": "39038540035", // ← REEMPLAZAR con tu MerchantId
    "MerchantName": "OKLA Marketplace",
    "MerchantType": "E-Commerce",
    "CurrencyCode": "214",
    "AuthKey": "E2A7A7A7E4F8A9B3C5D...", // ← REEMPLAZAR con tu AuthKey
    "Auth1": "", // Opcional para Phase 1
    "Auth2": "", // Opcional para Phase 1
    "IsTestEnvironment": true, // ← IMPORTANTE: true para sandbox
    "ApprovedUrl": "http://localhost:3000/payment/approved",
    "DeclinedUrl": "http://localhost:3000/payment/declined",
    "CancelUrl": "http://localhost:3000/payment/cancelled"
  }
}
```

### 4.2. Actualizar Variables de Entorno (Docker)

Si usas Docker Secrets o Environment Variables:

```bash
# compose.yaml
services:
  billingservice:
    environment:
      - Azul__MerchantId=39038540035
      - Azul__MerchantName=OKLA Marketplace
      - Azul__AuthKey=E2A7A7A7E4F8A9B3C5D...
      - Azul__IsTestEnvironment=true
```

O usando Docker Secrets:

```bash
# Crear secrets
echo "39038540035" | docker secret create azul_merchant_id -
echo "E2A7A7A7E4F8A9B3C5D..." | docker secret create azul_auth_key -

# compose.yaml
services:
  billingservice:
    secrets:
      - azul_merchant_id
      - azul_auth_key
```

### 4.3. Actualizar para Producción (Futuro)

Cuando obtengas credenciales de producción:

```json
{
  "Azul": {
    "MerchantId": "PROD_MERCHANT_ID",
    "AuthKey": "PROD_AUTH_KEY",
    "IsTestEnvironment": false, // ← CAMBIAR a false
    "ApprovedUrl": "https://okla.com.do/payment/approved",
    "DeclinedUrl": "https://okla.com.do/payment/declined",
    "CancelUrl": "https://okla.com.do/payment/cancelled"
  }
}
```

---

## 🧪 PASO 5: Probar la Integración

### 5.1. Reiniciar BillingService

```bash
# Si usas Docker
docker-compose restart billingservice

# O rebuild
docker-compose up -d --build billingservice

# Verificar logs
docker logs billingservice --tail 50
```

### 5.2. Test 1: Verificar Configuración

```bash
# Verificar que el servicio lee las credenciales correctamente
curl http://localhost:15107/health
# Esperado: "Healthy"

# Verificar logs de startup
docker logs billingservice 2>&1 | grep -i "azul"
# Debe mostrar que cargó la configuración
```

### 5.3. Test 2: Iniciar Pago de Prueba

```bash
# Crear payment request
curl -X POST http://localhost:15107/api/payment/azul/initiate \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 1000.00,
    "itbis": 180.00,
    "orderNumber": "TEST-SANDBOX-001"
  }' | jq
```

**Respuesta Esperada:**

```json
{
  "paymentPageUrl": "https://pruebas.azul.com.do/PaymentPage/",
  "formFields": {
    "MerchantId": "39038540035", // ← Tu MerchantId
    "MerchantName": "OKLA Marketplace",
    "Amount": "100000",
    "ITBIS": "18000",
    "AuthHash": "a1b2c3d4e5f6..." // ← Hash generado correctamente
  }
}
```

### 5.4. Test 3: Completar Flujo de Pago

1. **Crear formulario HTML de prueba:**

```html
<!-- test-azul-payment.html -->
<!DOCTYPE html>
<html>
  <head>
    <title>Test AZUL Payment</title>
  </head>
  <body>
    <h1>Test AZUL Payment Page</h1>
    <form action="https://pruebas.azul.com.do/PaymentPage/" method="POST">
      <input type="hidden" name="MerchantId" value="39038540035" />
      <input type="hidden" name="MerchantName" value="OKLA Marketplace" />
      <input type="hidden" name="MerchantType" value="E-Commerce" />
      <input type="hidden" name="CurrencyCode" value="214" />
      <input type="hidden" name="OrderNumber" value="TEST-SANDBOX-001" />
      <input type="hidden" name="Amount" value="100000" />
      <input type="hidden" name="ITBIS" value="18000" />
      <input
        type="hidden"
        name="ApprovedUrl"
        value="http://localhost:3000/payment/approved"
      />
      <input
        type="hidden"
        name="DeclinedUrl"
        value="http://localhost:3000/payment/declined"
      />
      <input
        type="hidden"
        name="CancelUrl"
        value="http://localhost:3000/payment/cancelled"
      />
      <input type="hidden" name="UseCustomField1" value="0" />
      <input type="hidden" name="UseCustomField2" value="0" />
      <input type="hidden" name="AuthHash" value="[HASH_GENERADO]" />

      <button type="submit">Pagar con AZUL</button>
    </form>
  </body>
</html>
```

2. **Probar con Tarjetas de Test:**

| Tarjeta                   | Número           | CVV | Exp     | Resultado Esperado |
| ------------------------- | ---------------- | --- | ------- | ------------------ |
| **Visa (Aprobada)**       | 4265880000000007 | 999 | 12/2027 | ✅ APROBADA        |
| **Visa (Declinada)**      | 4005520000000137 | 999 | 12/2027 | ❌ DECLINADA       |
| **Visa (3DS Challenge)**  | 4005520000000129 | 999 | 12/2027 | 🔒 Requiere OTP    |
| **Mastercard (Aprobada)** | 5425230000000002 | 999 | 12/2027 | ✅ APROBADA        |

3. **Verificar Callback:**

Después de completar el pago en AZUL Payment Page, deberías ver:

```bash
# Logs del callback
docker logs billingservice --tail 100 | grep -i "callback\|azul"

# Ejemplo de log esperado:
[16:30:45 INF] Callback AZUL recibido - Tipo: Approved, OrderNumber: TEST-SANDBOX-001
[16:30:45 INF] Hash validado correctamente para OrderNumber: TEST-SANDBOX-001
[16:30:45 INF] Transacción AZUL persistida: TEST-SANDBOX-001
```

4. **Verificar Base de Datos:**

```bash
# Conectar a PostgreSQL
docker exec -it postgres_db psql -U postgres -d billingservice

# Consultar transacción
SELECT order_number, status, amount, authorization_code, created_at
FROM azul_transactions
WHERE order_number = 'TEST-SANDBOX-001';

# Resultado esperado:
#  order_number      | status   | amount  | authorization_code |        created_at
# -------------------+----------+---------+--------------------+---------------------------
#  TEST-SANDBOX-001  | Approved | 1000.00 | 123456             | 2026-01-08 16:30:45+00
```

---

## 🎭 PASO 6: Escenarios de Prueba

### Casos de Uso a Validar

| #   | Escenario       | Tarjeta             | Resultado Esperado             | Status |
| --- | --------------- | ------------------- | ------------------------------ | ------ |
| 1   | Pago exitoso    | 4265880000000007    | Approved, guardado en DB       | ⏳     |
| 2   | Pago declinado  | 4005520000000137    | Declined, guardado en DB       | ⏳     |
| 3   | Usuario cancela | Cualquiera → Cancel | Cancelled, guardado en DB      | ⏳     |
| 4   | 3DS Challenge   | 4005520000000129    | Redirect a 3DS, luego Approved | ⏳     |
| 5   | Hash inválido   | Modificar AuthHash  | 400 Bad Request                | ⏳     |
| 6   | Timeout         | Esperar 15 min      | Expired                        | ⏳     |

### Script de Testing Automatizado

```bash
#!/bin/bash
# test-azul-sandbox.sh

echo "🧪 Iniciando pruebas de AZUL Sandbox..."

# Test 1: Payment Initiation
echo "Test 1: Iniciando pago..."
RESPONSE=$(curl -s -X POST http://localhost:15107/api/payment/azul/initiate \
  -H "Content-Type: application/json" \
  -d '{"amount": 1000.00, "itbis": 180.00, "orderNumber": "AUTO-TEST-001"}')

HASH=$(echo $RESPONSE | jq -r '.formFields.AuthHash')

if [ -n "$HASH" ] && [ "$HASH" != "null" ]; then
    echo "✅ Test 1 PASS: AuthHash generado correctamente"
else
    echo "❌ Test 1 FAIL: No se generó AuthHash"
    exit 1
fi

# Test 2: Verify Database Connection
echo "Test 2: Verificando conexión a base de datos..."
DB_CHECK=$(docker exec postgres_db psql -U postgres -d billingservice -c "SELECT 1" 2>&1)

if echo "$DB_CHECK" | grep -q "1"; then
    echo "✅ Test 2 PASS: Conexión a DB exitosa"
else
    echo "❌ Test 2 FAIL: No se pudo conectar a DB"
    exit 1
fi

# Test 3: Check Table Exists
echo "Test 3: Verificando tabla azul_transactions..."
TABLE_CHECK=$(docker exec postgres_db psql -U postgres -d billingservice \
  -c "SELECT table_name FROM information_schema.tables WHERE table_name='azul_transactions'" 2>&1)

if echo "$TABLE_CHECK" | grep -q "azul_transactions"; then
    echo "✅ Test 3 PASS: Tabla existe"
else
    echo "❌ Test 3 FAIL: Tabla no existe"
    exit 1
fi

echo ""
echo "🎉 Todos los tests automáticos pasaron!"
echo "📋 Próximos pasos:"
echo "  1. Probar manualmente con tarjetas de test AZUL"
echo "  2. Verificar callbacks en logs"
echo "  3. Validar persistencia en base de datos"
```

---

## 📊 PASO 7: Validación Final

### Checklist de Validación

- [ ] **Credenciales recibidas de AZUL**

  - [ ] MerchantId configurado
  - [ ] AuthKey configurado
  - [ ] IsTestEnvironment = true

- [ ] **Configuración del sistema**

  - [ ] appsettings.json actualizado
  - [ ] BillingService reiniciado
  - [ ] Health check responde OK

- [ ] **Tests de integración**

  - [ ] Payment initiation genera AuthHash correcto
  - [ ] Redirect a AZUL Payment Page funciona
  - [ ] Callback approved persiste transacción
  - [ ] Callback declined persiste transacción
  - [ ] Callback cancelled persiste transacción

- [ ] **Validación de datos**

  - [ ] Transacciones guardadas en azul_transactions
  - [ ] Amount formateado correctamente (sin decimales)
  - [ ] Timestamps en UTC
  - [ ] Status correcto (Approved/Declined/Cancelled)

- [ ] **Seguridad**

  - [ ] Hash validation funciona
  - [ ] Request con hash inválido es rechazado
  - [ ] Logs no muestran información sensible (AuthKey)

- [ ] **Documentación**
  - [ ] Credenciales guardadas en lugar seguro
  - [ ] Proceso documentado para el equipo
  - [ ] Casos de prueba documentados

---

## 🚨 Troubleshooting

### Problema 1: No recibo respuesta de AZUL

**Síntomas:**

- Email enviado hace más de 5 días hábiles
- No hay respuesta

**Soluciones:**

1. Llamar al 809-544-2985 directamente
2. Enviar follow-up por email CC a ambos correos
3. Contactar por WhatsApp Business
4. Solicitar hablar con un ejecutivo de cuentas

### Problema 2: AuthHash inválido

**Síntomas:**

```
ERROR: Hash de autenticación inválido
```

**Soluciones:**

1. Verificar orden de campos en concatenación (ver código AzulHashGenerator.cs)
2. Verificar que AuthKey es correcto (64 caracteres hex)
3. Verificar encoding UTF-8 en ambos lados
4. Revisar logs de AZUL en portal de merchant

### Problema 3: Transacción no se guarda en DB

**Síntomas:**

- Callback se ejecuta pero no hay registro en azul_transactions

**Soluciones:**

1. Verificar logs de BillingService
2. Comprobar que migration se aplicó
3. Verificar permisos de PostgreSQL
4. Revisar connection string

### Problema 4: 3D Secure no funciona

**Síntomas:**

- Error al procesar tarjeta con 3DS

**Soluciones:**

1. Verificar que tarjeta requiere 3DS (4005520000000129)
2. Confirmar con AZUL que 3DS está habilitado en sandbox
3. Revisar TermUrl en configuración

---

## 📞 Contactos de Soporte

### AZUL

| Departamento              | Contacto                         | Teléfono               | Email                            |
| ------------------------- | -------------------------------- | ---------------------- | -------------------------------- |
| **Soluciones Integradas** | Soporte Técnico                  | 809-544-2985 ext. 5000 | solucionesintegradas@azul.com.do |
| **Voz del Cliente**       | Atención General                 | 809-544-2985           | vozdelcliente@azul.com.do        |
| **Ejecutivo de Cuentas**  | [Asignado después de afiliación] | -                      | -                                |

### Interno OKLA

| Rol                | Nombre      | Responsabilidad                  |
| ------------------ | ----------- | -------------------------------- |
| **Lead Developer** | [Tu Nombre] | Integración técnica              |
| **Product Owner**  | [Nombre]    | Negociación con AZUL             |
| **DevOps**         | [Nombre]    | Configuración de infraestructura |

---

## 📚 Referencias

### Documentación AZUL

- **Manual de Integración:** Proporcionado por AZUL después de solicitud
- **Portal Developer:** https://dev.azul.com.do
- **FAQ:** https://azul.com.do/faq

### Documentación Interna OKLA

- [SPRINT_4_AZUL_INTEGRATION_RESEARCH.md](SPRINT_4_AZUL_INTEGRATION_RESEARCH.md)
- [SPRINT_4_COMPLETED.md](SPRINT_4_COMPLETED.md)
- [AzulHashGenerator.cs](../backend/BillingService/BillingService.Infrastructure/Azul/AzulHashGenerator.cs)
- [AzulPaymentService.cs](../backend/BillingService/BillingService.Application/Services/AzulPaymentService.cs)

---

## ✅ Checklist Final

Antes de considerar el sandbox configurado completamente:

```markdown
### Setup Completo

- [ ] Email de solicitud enviado a AZUL
- [ ] Credenciales recibidas y validadas
- [ ] appsettings.json configurado correctamente
- [ ] BillingService funcionando con nuevas credenciales
- [ ] Test manual con tarjeta 4265880000000007 → Aprobado
- [ ] Test manual con tarjeta 4005520000000137 → Declinado
- [ ] Callbacks funcionando y persistiendo correctamente
- [ ] Acceso al portal AZUL Merchant verificado
- [ ] Script de testing automatizado ejecutado exitosamente
- [ ] Documentación actualizada con credenciales (en lugar seguro)
- [ ] Equipo capacitado en uso del sandbox

### Evidencias Requeridas

- [ ] Screenshot de transacción aprobada
- [ ] Screenshot de portal AZUL Merchant
- [ ] Logs de callback exitoso
- [ ] Query de DB mostrando transacción guardada
- [ ] Captura de email de confirmación de AZUL

### Próximos Pasos

- [ ] Planificar pruebas con Frontend
- [ ] Solicitar revisión de integración por AZUL
- [ ] Preparar documentación para credenciales de producción
```

---

## 🎯 Timeline Estimado

| Día         | Actividad                          | Responsable   | Status       |
| ----------- | ---------------------------------- | ------------- | ------------ |
| **Día 1**   | Enviar solicitud a AZUL            | Product Owner | ⏳ Pendiente |
| **Día 2-3** | Esperar respuesta                  | AZUL          | ⏳ Pendiente |
| **Día 3**   | Follow-up si no hay respuesta      | Product Owner | ⏳ Pendiente |
| **Día 4-5** | Recibir credenciales               | AZUL          | ⏳ Pendiente |
| **Día 5**   | Configurar credenciales en sistema | Dev Team      | ⏳ Pendiente |
| **Día 5**   | Ejecutar tests de integración      | Dev Team      | ⏳ Pendiente |
| **Día 5**   | Validar persistencia y callbacks   | Dev Team      | ⏳ Pendiente |
| **Día 5**   | Documentar resultados              | Dev Team      | ⏳ Pendiente |

**Total:** 5 días hábiles desde solicitud inicial

---

**✅ Al completar esta guía, el ambiente Sandbox de AZUL estará 100% funcional.**

---

_Última actualización: Enero 8, 2026_  
_Mantenido por: Equipo de Desarrollo OKLA_
