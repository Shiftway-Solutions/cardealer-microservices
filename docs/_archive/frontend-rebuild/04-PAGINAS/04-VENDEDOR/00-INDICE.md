# 📁 04-VENDEDOR - Vendedor Individual

> **Descripción:** Páginas para vendedores individuales (no dealers)  
> **Total:** 5 documentos (fusionados)  
> **Prioridad:** 🟠 P1 - Monetización  
> **Última actualización:** Enero 30, 2026

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                            | Descripción                     | Prioridad |
| --- | -------------------------------------------------- | ------------------------------- | --------- |
| 1   | [01-publicar-vehiculo.md](01-publicar-vehiculo.md) | Formulario de publicación       | P0        |
| 2   | [02-seller-dashboard.md](02-seller-dashboard.md)   | Dashboard completo del vendedor | P1        |
| 3   | [03-seller-profiles.md](03-seller-profiles.md)     | Perfiles públicos de vendedores | P1        |
| 4   | [04-sell-your-car.md](04-sell-your-car.md)         | Landing "Vende tu carro"        | P0        |
| 5   | [05-media-multimedia.md](05-media-multimedia.md)   | Subida de fotos y videos        | P1        |

---

## 🎯 Orden de Implementación para IA

```
1. 04-sell-your-car.md       → Landing de captación
2. 01-publicar-vehiculo.md   → Formulario de publicación
3. 05-media-multimedia.md    → Subida de media
4. 02-seller-dashboard.md    → Dashboard completo
5. 03-seller-profiles.md     → Perfil público
```

---

## 🔗 Dependencias Externas

- **02-AUTH/**: Autenticación requerida
- **05-API-INTEGRATION/**: vehicles-api, media-api
- **07-PAGOS/**: Pago por publicación

---

## 📊 APIs Utilizadas

| Servicio            | Endpoints Principales                                   |
| ------------------- | ------------------------------------------------------- |
| VehiclesSaleService | POST /vehicles, PUT /vehicles/:id, DELETE /vehicles/:id |
| MediaService        | POST /media/upload, DELETE /media/:id                   |
| UserService         | GET /users/me/vehicles                                  |
| BillingService      | POST /billing/listing-payment                           |
