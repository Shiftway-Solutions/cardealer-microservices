# 📧 Guía de Configuración de Correo Corporativo - Zoho Mail Free

**Fecha:** Enero 8, 2026  
**Estado:** 📋 GUÍA PASO A PASO  
**Plataforma:** Zoho Mail - Plan Gratuito ($0 USD)

---

## 📋 Resumen

Esta guía te ayudará a configurar correos corporativos para OKLA usando Zoho Mail Free plan.

**Tiempo estimado:** 30-60 minutos  
**Costo:** $0 USD (solo costo del dominio ~$12-15/año)

---

## ✅ Características del Plan Gratuito

| Feature               | Detalle                      |
| --------------------- | ---------------------------- |
| **Cuentas de correo** | 5 cuentas máximo             |
| **Almacenamiento**    | 5 GB por cuenta              |
| **Adjuntos**          | Hasta 25 MB                  |
| **Dominio**           | @okla.com.do (personalizado) |
| **Webmail**           | ✅ Incluido                  |
| **Cliente Desktop**   | ❌ Solo en planes pagos      |
| **Mobile App**        | ✅ Incluido                  |
| **Antispam**          | ✅ Incluido                  |

---

## 🎯 Cuentas de Correo a Crear

| Email               | Usuario        | Propósito                  |
| ------------------- | -------------- | -------------------------- |
| gmoreno@okla.com.do | Gregory Moreno | Lead Developer / Admin     |
| nmateo@okla.com.do  | Nicauris Mateo | Gerente                    |
| soporte@okla.com.do | Equipo Soporte | Atención al cliente        |
| ventas@okla.com.do  | Equipo Ventas  | Consultas comerciales      |
| noreply@okla.com.do | Sistema        | Notificaciones automáticas |

---

## 🚀 PASO 1: Verificar Dominio

Primero, verifica si ya tienes el dominio registrado:

```bash
# Verificar información del dominio
whois okla.com.do

# O usar dig para verificar NS records
dig okla.com.do NS +short

# O usar nslookup
nslookup okla.com.do
```

### Si el dominio NO está registrado:

**Registradores recomendados para .com.do:**

1. **NIC.do (Oficial)** - https://www.nic.do
   - Precio: ~$15 USD/año
   - Proceso: 100% online
2. **Dominios Hoy** - https://www.dominioshoy.do

   - Precio: ~$12 USD/año
   - Soporte local en RD

3. **Hosttotal.do** - https://hosttotal.do
   - Precio: ~$14 USD/año
   - Incluye DNS gratis

**Comando para verificar disponibilidad:**

```bash
# Verificar si el dominio está disponible
curl -s "https://www.nic.do/whois?domain=okla.com.do" | grep -i "disponible\|available"
```

---

## ⚠️ IMPORTANTE: Limitaciones de Zoho Mail Free

### 🚫 Lo que NO se puede hacer por terminal:

Zoho Mail Free **NO tiene CLI ni API pública**. Estas tareas requieren interfaz web:

- ❌ Crear cuenta de Zoho
- ❌ Verificar dominio en Zoho (aunque DNS sí se hace por terminal)
- ❌ Crear usuarios/cuentas de correo
- ❌ Modificar configuración de cuentas
- ❌ Gestionar permisos y roles

### ✅ Lo que SÍ se puede hacer por terminal:

- ✅ Configurar registros DNS (MX, TXT, SPF, DKIM)
- ✅ Verificar registros DNS con `dig`/`nslookup`
- ✅ Testear SMTP/IMAP con `curl`/`telnet`
- ✅ Enviar/recibir emails con `curl`
- ✅ Automatizar backups con scripts

> **Nota:** Solo los planes pagos (Mail Lite $1/user/mes, Premium $4/user/mes) tienen acceso a API limitada. Para automatización completa, considera Google Workspace ($6/user/mes) o Microsoft 365 ($6/user/mes) que sí tienen APIs robustas.

---

## 🚀 PASO 2: Crear Cuenta en Zoho Mail

### 2.1. Registro en Zoho (⚠️ REQUIERE INTERFAZ WEB)

```bash
# Abrir Zoho Mail en el navegador
open "https://www.zoho.com/mail/zohomail-pricing.html?plan=free"

# O si estás en Linux
xdg-open "https://www.zoho.com/mail/zohomail-pricing.html?plan=free"
```

**Pasos en la web (NO automatizable con plan Free):**

1. Click en **"Sign Up Now"** en el plan Free
2. Completar formulario:

   - **Email personal:** tu_email@gmail.com (temporal)
   - **Password:** [Crear contraseña segura]
   - **Company Name:** OKLA Marketplace
   - **Number of Employees:** 1-5

3. Verificar email personal
4. Ingresar dominio: `okla.com.do`

### 2.2. Seleccionar Plan Free

En la página de planes, asegúrate de seleccionar:

```
✅ FOREVER FREE PLAN
   - 5 Users
   - 5 GB/User
   - Free Forever
   - ❌ No CLI/API access
```

---

## 🚀 PASO 3: Verificar Dominio (DNS Configuration)

Este es el paso MÁS IMPORTANTE. Debes agregar registros DNS para que Zoho verifique que eres dueño del dominio.

### 3.1. Obtener Registros DNS de Zoho

Después de agregar el dominio, Zoho te mostrará registros DNS como estos:

```
TXT Record (Verificación):
Name: @
Type: TXT
Value: zoho-verification=zb12345678.zmverify.zoho.com
TTL: 3600

MX Records (Correo entrante):
Priority: 10
Host: @
Points to: mx.zoho.com
TTL: 3600

Priority: 20
Host: @
Points to: mx2.zoho.com
TTL: 3600

Priority: 50
Host: @
Points to: mx3.zoho.com
TTL: 3600

SPF Record (Autenticación):
Type: TXT
Host: @
Value: v=spf1 include:zoho.com ~all
TTL: 3600

DKIM Record (Seguridad):
Type: TXT
Host: zmail._domainkey
Value: k=rsa; p=MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC... (largo)
TTL: 3600

DMARC Record (Política de correo):
Type: TXT
Host: _dmarc
Value: v=DMARC1; p=none; rua=mailto:gmoreno@okla.com.do
TTL: 3600
```

### 3.2. Configurar DNS (Método depende de tu proveedor)

#### Opción A: Digital Ocean (si usas DO para DNS)

```bash
# Instalar doctl si no lo tienes
brew install doctl

# Autenticar
doctl auth init

# Crear registros DNS
# TXT Record (Verificación)
doctl compute domain records create okla.com.do \
  --record-type TXT \
  --record-name @ \
  --record-data "zoho-verification=zb12345678.zmverify.zoho.com" \
  --record-ttl 3600

# MX Records (Correo)
doctl compute domain records create okla.com.do \
  --record-type MX \
  --record-name @ \
  --record-data "mx.zoho.com" \
  --record-priority 10 \
  --record-ttl 3600

doctl compute domain records create okla.com.do \
  --record-type MX \
  --record-name @ \
  --record-data "mx2.zoho.com" \
  --record-priority 20 \
  --record-ttl 3600

doctl compute domain records create okla.com.do \
  --record-type MX \
  --record-name @ \
  --record-data "mx3.zoho.com" \
  --record-priority 50 \
  --record-ttl 3600

# SPF Record
doctl compute domain records create okla.com.do \
  --record-type TXT \
  --record-name @ \
  --record-data "v=spf1 include:zoho.com ~all" \
  --record-ttl 3600

# DKIM Record (Reemplazar con el valor real de Zoho)
doctl compute domain records create okla.com.do \
  --record-type TXT \
  --record-name "zmail._domainkey" \
  --record-data "k=rsa; p=TU_CLAVE_PUBLICA_DKIM_AQUI" \
  --record-ttl 3600

# DMARC Record
doctl compute domain records create okla.com.do \
  --record-type TXT \
  --record-name "_dmarc" \
  --record-data "v=DMARC1; p=none; rua=mailto:gmoreno@okla.com.do" \
  --record-ttl 3600
```

#### Opción B: Cloudflare (si usas Cloudflare)

```bash
# Usando curl con Cloudflare API
CLOUDFLARE_API_TOKEN="tu_token_aqui"
ZONE_ID="tu_zone_id"

# TXT Record (Verificación)
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "TXT",
    "name": "@",
    "content": "zoho-verification=zb12345678.zmverify.zoho.com",
    "ttl": 3600
  }'

# MX Record 1
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "MX",
    "name": "@",
    "content": "mx.zoho.com",
    "priority": 10,
    "ttl": 3600
  }'

# MX Record 2
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "MX",
    "name": "@",
    "content": "mx2.zoho.com",
    "priority": 20,
    "ttl": 3600
  }'

# MX Record 3
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "MX",
    "name": "@",
    "content": "mx3.zoho.com",
    "priority": 50,
    "ttl": 3600
  }'

# SPF Record
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "TXT",
    "name": "@",
    "content": "v=spf1 include:zoho.com ~all",
    "ttl": 3600
  }'

# DKIM Record
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "TXT",
    "name": "zmail._domainkey",
    "content": "k=rsa; p=TU_CLAVE_PUBLICA_DKIM",
    "ttl": 3600
  }'

# DMARC Record
curl -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
  -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{
    "type": "TXT",
    "name": "_dmarc",
    "content": "v=DMARC1; p=none; rua=mailto:gmoreno@okla.com.do",
    "ttl": 3600
  }'
```

#### Opción C: GoDaddy / HostTotal / Otros

Si tu proveedor no tiene API CLI, puedes:

1. Ir al panel de control DNS del proveedor
2. Agregar los registros manualmente (copia/pega de Zoho)
3. Guardar cambios

**Comando para abrir el panel:**

```bash
# Si es GoDaddy
open "https://dcc.godaddy.com/manage/okla.com.do/dns"

# Si es HostTotal
open "https://hosttotal.do/clientarea.php?action=domaindetails&id=TU_ID"
```

### 3.3. Verificar Registros DNS (IMPORTANTE)

Espera 5-10 minutos después de agregar los registros, luego verifica:

```bash
# Verificar TXT Record (Zoho Verification)
dig okla.com.do TXT +short | grep zoho

# Verificar MX Records
dig okla.com.do MX +short

# Esperado:
# 10 mx.zoho.com.
# 20 mx2.zoho.com.
# 50 mx3.zoho.com.

# Verificar SPF
dig okla.com.do TXT +short | grep spf

# Esperado:
# "v=spf1 include:zoho.com ~all"

# Verificar DKIM
dig zmail._domainkey.okla.com.do TXT +short

# Esperado: Ver la clave pública RSA

# Verificar DMARC
dig _dmarc.okla.com.do TXT +short

# Esperado:
# "v=DMARC1; p=none; rua=mailto:gmoreno@okla.com.do"
```

**Script completo de verificación:**

```bash
#!/bin/bash
# verify-dns-zoho.sh

DOMAIN="okla.com.do"
echo "🔍 Verificando DNS para $DOMAIN..."
echo ""

echo "1️⃣ TXT Record (Zoho Verification):"
dig $DOMAIN TXT +short | grep zoho || echo "❌ NO ENCONTRADO"
echo ""

echo "2️⃣ MX Records:"
dig $DOMAIN MX +short || echo "❌ NO ENCONTRADO"
echo ""

echo "3️⃣ SPF Record:"
dig $DOMAIN TXT +short | grep spf || echo "❌ NO ENCONTRADO"
echo ""

echo "4️⃣ DKIM Record:"
dig zmail._domainkey.$DOMAIN TXT +short || echo "❌ NO ENCONTRADO"
echo ""

echo "5️⃣ DMARC Record:"
dig _dmarc.$DOMAIN TXT +short || echo "❌ NO ENCONTRADO"
echo ""

echo "✅ Si todos los registros aparecen, puedes verificar en Zoho."
```

```bash
# Hacer ejecutable
chmod +x verify-dns-zoho.sh

# Ejecutar
./verify-dns-zoho.sh
```

---

## 🚀 PASO 4: Verificar Dominio en Zoho

### 4.1. Verificar en la Consola Web (⚠️ REQUIERE INTERFAZ WEB)

```bash
# Abrir panel de Zoho
open "https://mailadmin.zoho.com"

# Ir a: Setup > Domain & Users > Domain Verification
```

Click en **"Verify"** y Zoho verificará los registros TXT/MX automáticamente.

**Resultado esperado:**

```
✅ Domain Verified Successfully!
✅ MX Records Configured
✅ SPF Record Found
✅ DKIM Configured
```

### 4.2. ❌ NO existe API/CLI para esto

**Zoho Mail Free NO proporciona:**

- ❌ API REST para verificación de dominios
- ❌ CLI oficial de Zoho Mail
- ❌ Webhooks o automatización

**Alternativa (solo para planes pagos):**

Si upgradeás a Mail Premium, puedes usar la API limitada:

```bash
# Ejemplo conceptual (requiere Mail Premium + API Key)
curl -X GET "https://mail.zoho.com/api/domain (⚠️ REQUIERE INTERFAZ WEB)

**En el panel web de Zoho:**

```

Panel > Setup > Users > Add Users

```

**Datos para gmoreno@okla.com.do:**

```

Email: gmoreno@okla.com.do
First Name: Gregory
Last Name: Moreno
Password: [Generar contraseña segura]
User Role: Super Admin

````

> ⚠️ **Importante:** No existe CLI para crear usuarios en Zoho Free. Debes usar la interfaz web obligatoriamente.

### 5.1.1. Generar Contraseñas Seguras (Esto sí por terminal)

```bash
# Opción 1: Generar contraseña aleatoria (20 caracteres)
openssl rand -base64 20

# Opción 2: Con caracteres especiales
LC_ALL=C tr -dc 'A-Za-z0-9!@#$%^&*' < /dev/urandom | head -c 20; echo

# Opción 3: Script para generar múltiples contraseñas
for i in {1..5}; do
  echo "Password $i: $(openssl rand -base64 20)"
done
````

Email: gmoreno@okla.com.do
First Name: Gregory
Last Name: Moreno
Password: [Generar contraseña segura]
User Role: Super Admin

````

**Comando para generar contraseña segura:**

```bash
# Generar contraseña aleatoria (20 caracteres)
openssl rand -base64 20

# O con caracteres especiales
LC_ALL=C tr -dc 'A-Za-z0-9!@#$%^&*' < /dev/urandom | head -c 20; echo
````

### 5.2. Crear Cuentas Adicionales

**Script para documentar las cuentas (NO crea en Zoho, solo documenta):**

```bash
#!/bin/bash
# create-email-accounts-list.sh

cat > email_accounts.txt << 'EOF'
# Cuentas de correo OKLA - Zoho Mail
# Fecha: $(date)

## Cuenta 1: Administrador Principal
Email: gmoreno@okla.com.do
Nombre: Gregory Moreno
Rol: Super Admin / Lead Developer
Password: [GENERADA - Guardar en LastPass/1Password]
Uso: Desarrollo, integración AZUL, administración

## Cuenta 2: Usuario adicional
Email: nmateo@okla.com.do
Nombre: [Nombre Completo]
Rol: [Rol del usuario]
Password: [GENERADA - Guardar en LastPass/1Password]
Uso: [Descripción]

## Cuenta 3: Soporte al Cliente
Email: soporte@okla.com.do
Nombre: Equipo Soporte OKLA
Rol: Standard User
Password: [GENERADA - Guardar en LastPass/1Password]
Uso: Atención a clientes, responder consultas

## Cuenta 4: Ventas
Email: ventas@okla.com.do
Nombre: Equipo Ventas OKLA
Rol: Standard User
Password: [GENERADA - Guardar en LastPass/1Password]
Uso: Consultas comerciales, negociaciones

## Cuenta 5: Sistema (No-Reply)
Email: noreply@okla.com.do
Nombre: Sistema OKLA
Rol: Standard User
Password: [GENERADA - Guardar en LastPass/1Password]
Uso: Emails automáticos (confirmaciones, notificaciones)
EOF

echo "✅ Archivo email_accounts.txt creado"
cat email_accounts.txt
```

**Crear cada cuenta en Zoho:**

1. Ir a: Panel > Users > Add User
2. Completar datos
3. Asignar rol
4. Enviar invitación (o crear sin invitación)
5. Guardar contraseña en gestor de contraseñas

---

## 🚀 PASO 6: Configurar Cuenta gmoreno@okla.com.do

### 6.1. Primera Sesión

```bash
# Abrir webmail
open "https://mail.zoho.com"

# Login con:
# Email: gmoreno@okla.com.do
# Password: [La que creaste]
```

### 6.2. Configuración Básica

En el webmail:

1. **Firma de correo:**

   ```
   Settings > Mail > Signature

   ---
   Gregory Moreno
   Lead Developer
   OKLA Marketplace

   🌐 okla.com.do
   📧 gmoreno@okla.com.do
   📱 [Tu teléfono]
   ```

2. **Foto de perfil:**

   - Settings > Profile > Upload Photo

3. **Timezone:**
   - Settings > General > Timezone
   - Seleccionar: (GMT-4:00) Atlantic Time (Santo Domingo)

### 6.3. Configurar Cliente de Correo (Opcional)

Si tienes plan pago o quieres usar app móvil:

**iOS/Android:**

```bash
# Descargar app Zoho Mail
# iOS: https://apps.apple.com/app/zoho-mail/id909262651
# Android: https://play.google.com/store/apps/details?id=com.zoho.mail

# Configurar:
# Email: gmoreno@okla.com.do
# Password: [Tu password]
# Server: Autodetectado por Zoho
```

**Thunderbird/Apple Mail (IMAP/SMTP):**

```
IMAP Settings:
  Server: imap.zoho.com
  Port: 993
  Security: SSL/TLS
  Username: gmoreno@okla.com.do
  Password: [Tu password o App Password]

SMTP Settings:
  Server: smtp.zoho.com
  Port: 465 (SSL) o 587 (TLS)
  Security: SSL/TLS
  Authentication: Yes
  Username: gmoreno@okla.com.do
  Password: [Tu password o App Password]
```

**Script para generar configuración:**

```bash
#!/bin/bash
# generate-email-config.sh

EMAIL="gmoreno@okla.com.do"

cat > email_config_${EMAIL}.txt << EOF
# Configuración IMAP/SMTP para $EMAIL
# Generado: $(date)

## IMAP (Correo Entrante)
Server: imap.zoho.com
Port: 993
Security: SSL/TLS
Username: $EMAIL
Password: [Usar password de Zoho o App Password]

## SMTP (Correo Saliente)
Server: smtp.zoho.com
Port: 465 (SSL) o 587 (TLS)
Security: SSL/TLS
Authentication: Required
Username: $EMAIL
Password: [Igual que IMAP]

## Webmail
URL: https://mail.zoho.com
Login: $EMAIL

## Mobile App
iOS: Zoho Mail (App Store)
Android: Zoho Mail (Google Play)
EOF

cat email_config_${EMAIL}.txt
```

---

## 🚀 PASO 7: Probar Envío/Recepción

### 7.1. Test de Envío

```bash
# Enviar email de prueba usando curl
curl -v --url "smtp://smtp.zoho.com:587" \
  --ssl-reqd \
  --mail-from "gmoreno@okla.com.do" \
  --mail-rcpt "tu_email_personal@gmail.com" \
  --user "gmoreno@okla.com.do:TU_PASSWORD" \
  --upload-file - << EOF
From: Gregory Moreno <gmoreno@okla.com.do>
To: tu_email_personal@gmail.com
Subject: Test Email - OKLA Corporate Email
Date: $(date -R)

Este es un email de prueba desde el nuevo correo corporativo de OKLA.

Si recibes este mensaje, la configuración fue exitosa!

---
Gregory Moreno
Lead Developer
OKLA Marketplace
okla.com.do
EOF
```

### 7.2. Test de Recepción

```bash
# Desde tu email personal, envía un email a gmoreno@okla.com.do
# Luego verifica que llegó:

# Opción 1: Abrir webmail
open "https://mail.zoho.com"

# Opción 2: Usar curl para verificar (requiere IMAP)
# (Esto es complejo, mejor usar webmail)
```

### 7.3. Script Completo de Testing

```bash
#!/bin/bash
# test-zoho-email.sh

ZOHO_EMAIL="gmoreno@okla.com.do"
ZOHO_PASSWORD="TU_PASSWORD_AQUI"
TEST_RECIPIENT="tu_email@gmail.com"

echo "🧪 Testing Zoho Email Configuration..."
echo ""

# Test 1: SMTP Connection
echo "Test 1: SMTP Connection (smtp.zoho.com:587)"
timeout 5 bash -c "cat < /dev/null > /dev/tcp/smtp.zoho.com/587" && \
  echo "✅ SMTP port is open" || \
  echo "❌ SMTP port is closed"
echo ""

# Test 2: IMAP Connection
echo "Test 2: IMAP Connection (imap.zoho.com:993)"
timeout 5 bash -c "cat < /dev/null > /dev/tcp/imap.zoho.com/993" && \
  echo "✅ IMAP port is open" || \
  echo "❌ IMAP port is closed"
echo ""

# Test 3: Send Test Email
echo "Test 3: Sending test email to $TEST_RECIPIENT..."
curl -v --url "smtp://smtp.zoho.com:587" \
  --ssl-reqd \
  --mail-from "$ZOHO_EMAIL" \
  --mail-rcpt "$TEST_RECIPIENT" \
  --user "$ZOHO_EMAIL:$ZOHO_PASSWORD" \
  --upload-file - << EOF
From: OKLA Marketplace <$ZOHO_EMAIL>
To: $TEST_RECIPIENT
Subject: ✅ Test Email - OKLA Corporate Email Setup
Date: $(date -R)

Hello!

This is an automated test email from OKLA's new corporate email system.

If you receive this message, the configuration was successful! ✅

---
OKLA Marketplace
okla.com.do
EOF

if [ $? -eq 0 ]; then
  echo "✅ Email sent successfully!"
  echo "📧 Check $TEST_RECIPIENT inbox"
else
  echo "❌ Failed to send email"
  echo "💡 Check username/password"
fi
```

---

## 🔐 PASO 8: Seguridad y Respaldos

### 8.1. Habilitar 2FA (Two-Factor Authentication)

```bash
# Abrir configuración de seguridad
open "https://accounts.zoho.com/home#security/2fa"

# Pasos:
# 1. Enable Two-Factor Authentication
# 2. Usar Google Authenticator, Authy, o SMS
# 3. Guardar códigos de respaldo
```

### 8.2. Crear App Passwords (para apps de terceros)

```bash
# Abrir configuración de App Passwords
open "https://accounts.zoho.com/home#security/apppassword"

# Crear App Password para:
# - Thunderbird
# - Apple Mail
# - Integraciones del sistema (BillingService)
```

### 8.3. Configurar Respaldo Automático

```bash
# Crear script de respaldo
cat > backup-zoho-emails.sh << 'EOF'
#!/bin/bash
# backup-zoho-emails.sh

BACKUP_DIR="$HOME/zoho-email-backups"
DATE=$(date +%Y%m%d)
BACKUP_FILE="$BACKUP_DIR/zoho-backup-$DATE.mbox"

mkdir -p "$BACKUP_DIR"

echo "📦 Backing up Zoho emails..."

# Usar fetchmail o similar para descargar emails vía IMAP
# Esto requiere instalar fetchmail o getmail

# Ejemplo con getmail (instalar primero: pip install getmail6)
getmail -r ~/.getmailrc --quiet

echo "✅ Backup completed: $BACKUP_FILE"

# Opcional: Comprimir
gzip "$BACKUP_FILE"

# Opcional: Upload a S3 o Google Drive
# aws s3 cp "$BACKUP_FILE.gz" s3://okla-email-backups/
EOF

chmod +x backup-zoho-emails.sh

# Agregar a crontab (ejecutar diariamente a las 2 AM)
# crontab -e
# 0 2 * * * /path/to/backup-zoho-emails.sh
```

---

## 🚀 PASO 9: Integrar con BillingService (SMTP)

Para enviar emails desde el BillingService (ej: confirmaciones de pago):

### 9.1. Actualizar appsettings.json

```bash
# Editar archivo
nano backend/BillingService/BillingService.Api/appsettings.json
```

**Agregar sección SMTP:**

```json
{
  "Smtp": {
    "Host": "smtp.zoho.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "noreply@okla.com.do",
    "Password": "APP_PASSWORD_AQUI",
    "FromEmail": "noreply@okla.com.do",
    "FromName": "OKLA Marketplace"
  }
}
```

### 9.2. Crear EmailService

```bash
# Agregar paquete NuGet
cd backend/BillingService/BillingService.Api
dotnet add package MailKit
```

**Código de EmailService:**

```csharp
// BillingService.Infrastructure/Services/EmailService.cs

using MailKit.Net.Smtp;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration config)
    {
        _host = config["Smtp:Host"];
        _port = int.Parse(config["Smtp:Port"]);
        _enableSsl = bool.Parse(config["Smtp:EnableSsl"]);
        _username = config["Smtp:Username"];
        _password = config["Smtp:Password"];
        _fromEmail = config["Smtp:FromEmail"];
        _fromName = config["Smtp:FromName"];
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, _enableSsl);
        await client.AuthenticateAsync(_username, _password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

### 9.3. Test de Integración

```bash
# Crear endpoint de test
curl -X POST http://localhost:15107/api/email/test \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "tu_email@gmail.com",
    "subject": "Test Email from BillingService",
    "body": "<h1>Success!</h1><p>Email integration working.</p>"
  }'
```

---

## 📊 PASO 10: Uso para Solicitud de AZUL Sandbox

Ahora que tienes correo corporativo, puedes solicitar AZUL Sandbox:

### 10.1. Actualizar Template de Email

En el archivo [AZUL_SANDBOX_SETUP_GUIDE.md](AZUL_SANDBOX_SETUP_GUIDE.md), usar:

```
De: Gregory Moreno <gmoreno@okla.com.do>  ✅
Para: solucionesintegradas@azul.com.do
Asunto: Solicitud de Credenciales Sandbox - OKLA Marketplace

[... contenido del template ...]

INFORMACIÓN DE CONTACTO:
- Nombre: Gregory Moreno
- Cargo: Lead Developer
- Email: gmoreno@okla.com.do  ✅
- Teléfono: [Tu teléfono]
```

### 10.2. Enviar Email a AZUL

```bash
# Usando curl y SMTP de Zoho
curl -v --url "smtp://smtp.zoho.com:587" \
  --ssl-reqd \
  --mail-from "gmoreno@okla.com.do" \
  --mail-rcpt "solucionesintegradas@azul.com.do" \
  --user "gmoreno@okla.com.do:TU_PASSWORD" \
  --upload-file - << 'EOF'
From: Gregory Moreno <gmoreno@okla.com.do>
To: solucionesintegradas@azul.com.do
Subject: Solicitud de Credenciales Sandbox - OKLA Marketplace
Date: $(date -R)
Content-Type: text/plain; charset=UTF-8

Estimado Equipo de AZUL,

Me dirijo a ustedes en nombre de OKLA Marketplace (okla.com.do), una
plataforma de compra y venta de vehículos en República Dominicana.

Solicitamos credenciales para el ambiente de pruebas (Sandbox) de AZUL
Payment Gateway para integrar su procesador de pagos en nuestra plataforma.

INFORMACIÓN DE LA EMPRESA:
- Razón Social: OKLA SRL
- RNC: [PENDIENTE - Proporcionar]
- Sitio Web: https://okla.com.do
- Tipo de Negocio: Marketplace de Vehículos
- Volumen Mensual Estimado: 50-100 transacciones inicialmente

INFORMACIÓN TÉCNICA:
- Método de Integración: Payment Page (Hosted)
- Framework Backend: .NET 8.0 / C#
- Tipo de Transacciones: Sale (venta con captura inmediata)
- Necesidad de DataVault: Sí (futuro, para tarjetas guardadas)

CREDENCIALES REQUERIDAS:
1. MerchantId (Test)
2. MerchantName
3. AuthKey
4. Auth1 y Auth2 (para Webservices API en el futuro)
5. Acceso al portal de pruebas

INFORMACIÓN DE CONTACTO:
- Nombre: Gregory Moreno
- Cargo: Lead Developer
- Email: gmoreno@okla.com.do
- Teléfono: [Tu teléfono]

Agradecemos su pronta respuesta.

Saludos cordiales,
Gregory Moreno
Lead Developer
OKLA Marketplace
https://okla.com.do
gmoreno@okla.com.do
EOF
```

**O simplemente abrir webmail y copiar/pegar:**

```bash
open "https://mail.zoho.com"
# Click "Compose"
# Copiar template
# Enviar
```

---

## ✅ Checklist Final

```markdown
### Configuración Completa

- [ ] Dominio okla.com.do registrado y activo
- [ ] Cuenta Zoho Mail Free creada
- [ ] Registros DNS configurados (TXT, MX, SPF, DKIM, DMARC)
- [ ] Dominio verificado en Zoho
- [ ] Cuenta gmoreno@okla.com.do creada y funcionando
- [ ] Firma de correo configurada
- [ ] 2FA habilitado
- [ ] Test de envío exitoso
- [ ] Test de recepción exitoso
- [ ] SMTP integrado en BillingService (opcional)
- [ ] Email enviado a AZUL para solicitar sandbox ✅

### Cuentas Creadas

- [ ] gmoreno@okla.com.do (Super Admin)
- [ ] nmateo@okla.com.do (Usuario adicional)
- [ ] soporte@okla.com.do (Atención al cliente)
- [ ] ventas@okla.com.do (Consultas comerciales)
- [ ] noreply@okla.com.do (Sistema)

### Documentación

- [ ] Passwords guardadas en gestor de contraseñas
- [ ] Configuración IMAP/SMTP documentada
- [ ] App Passwords creados
- [ ] Códigos de respaldo 2FA guardados
```

---

## 🔗 Scripts Útiles

Todos los scripts mencionados en un solo lugar:

````bash
# Crear directorio de scripts
mkdir -p scripts/zoho-mail

# Copiar todos los scripts
cp verify-dns-zoho.sh scripts/zoho-mail/
cp create-email-accounts-list.sh scripts/zoho-mail/
cp generate-email-config.sh scripts/zoho-mail/
cp test-zoho-email.sh scripts/zoho-mail/
cp backup-zoho-emails.sh scripts/zoho-mail/
� Alternativas con CLI/API

Si necesitas automatización completa por terminal, considera estas alternativas:

### Opción 1: Google Workspace (Recomendado si necesitas CLI)

```bash
# Tiene CLI oficial: gcloud y gam (Google Apps Manager)
# Precio: $6/user/mes (14 días gratis)

# Instalar gam (https://github.com/GAM-team/GAM)
bash <(curl -s -S -L https://gam-shortn.appspot.com/gam-install)

# Crear usuarios por CLI
gam create user gmoreno@okla.com.do firstname Gregory lastname Moreno password RandomPass123!

# Configurar MX records
gam info domain okla.com.do
````

### Opción 2: Microsoft 365 (API completa)

```bash
# Usa Graph API con PowerShell o Python
# Precio: $6/user/mes

# Ejemplo con Python SDK
pip install msgraph-sdk
# Luego puedes crear usuarios por código
```

### Opción 3: Migadu (Más barato con IMAP/SMTP completo)

```bash
# Precio: $19/año por dominio (ilimitados usuarios)
# No tiene CLI, pero IMAP/SMTP sin restricciones
# Configuración 100% por DNS
```

### Opción 4: Self-hosted (Control total)

```bash
# Opciones: Mail-in-a-Box, iRedMail, Mailcow
# Precio: Solo el VPS ($5-10/mes)
# CLI completo via SSH

# Mail-in-a-Box ejemplo:
ssh root@your-server
curl -s https://mailinabox.email/setup.sh | sudo bash
```

---

## 📞 Soporte

### Zoho Mail Support

| Canal             | Información                               |
| ----------------- | ----------------------------------------- |
| **Help Center**   | https://help.zoho.com/portal/en/kb/mail   |
| **Community**     | https://help.zoho.com/portal/en/community |
| **Email Support** | support@zohomail.com                      |
| **Live Chat**     | Disponible en panel (horario limitado)    |
| **API Docs**      | ❌ No disponible en Free plan             |
| **CLI**           | ❌ No existe                              |

| Canal             | Información                               |
| ----------------- | ----------------------------------------- |
| **Help Center**   | https://help.zoho.com/portal/en/kb/mail   |
| **Community**     | https://help.zoho.com/portal/en/community |
| **Email Support** | support@zohomail.com                      |
| **Live Chat**     | Disponible en panel (horario limitado)    |

### Problemas Comunes

**Problema:** "Domain verification failed"

```bash
# Solución: Verificar registros DNS
dig okla.com.do TXT +short | grep zoho
dig okla.com.do MX +short

# Esperar propagación (hasta 48 horas, típicamente 1-2 horas)
```

**Problema:** "Can't send emails (SMTP error)"

```bash
# Verificar credenciales
# Verificar que estás usando App Password (no password normal)
# Verificar puerto (587 con TLS, o 465 con SSL)
```

---

## 🎯 Próximos Pasos

1. ✅ **Completar configuración de Zoho Mail** (este documento)
2. 🔄 **Enviar solicitud a AZUL** usando gmoreno@okla.com.do
3. ⏳ **Esperar respuesta de AZUL** (2-3 días hábiles)
4. 🔧 **Configurar credenciales AZUL** cuando las recibas
5. 🧪 **Probar integración AZUL** con sandbox
6. 🚀 **Solicitar credenciales de producción** cuando esté listo

---

**✅ Al completar esta guía, tendrás correo corporativo profesional para OKLA.**

---

_Última actualización: Enero 8, 2026_  
_Mantenido por: Gregory Moreno - gmoreno@okla.com.do_
