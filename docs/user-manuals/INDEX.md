# 📚 Índice de Manuales de Usuario — Plataforma OKLA

**Última actualización:** 2026-03-02

Estos manuales documentan todas las funcionalidades disponibles para cada tipo de usuario en la plataforma OKLA. Son utilizados por el agente de soporte técnico (SupportAgent) como base de conocimiento para asistir a los usuarios.

---

## Manuales por Tipo de Usuario

| #   | Tipo de Usuario          | Archivo                                                | Descripción                                            |
| --- | ------------------------ | ------------------------------------------------------ | ------------------------------------------------------ |
| 1   | 👤 **Usuario Anónimo**   | [MANUAL_USUARIO_ANONIMO.md](MANUAL_USUARIO_ANONIMO.md) | Visitantes sin cuenta — navegación, búsqueda, registro |
| 2   | 🛒 **Comprador (Buyer)** | [MANUAL_USUARIO_BUYER.md](MANUAL_USUARIO_BUYER.md)     | Favoritos, chat, alertas, comparaciones, citas         |
| 3   | 💰 **Vendedor (Seller)** | [MANUAL_USUARIO_SELLER.md](MANUAL_USUARIO_SELLER.md)   | Publicar vehículos, gestionar anuncios, KYC            |
| 4   | 🏢 **Dealer**            | [MANUAL_USUARIO_DEALER.md](MANUAL_USUARIO_DEALER.md)   | Panel completo, CRM, chatbot IA, analíticas            |
| 5   | 🔧 **Administrador**     | [MANUAL_USUARIO_ADMIN.md](MANUAL_USUARIO_ADMIN.md)     | Gestión completa de la plataforma                      |

---

## Jerarquía de Permisos

```
Anónimo → Buyer → Seller → Dealer → Admin
  (cada nivel incluye las funcionalidades del anterior)
```

---

## Uso por el SupportAgent

El agente de soporte de OKLA utiliza estos manuales para:

1. Responder preguntas sobre funcionalidades específicas de cada tipo de usuario.
2. Guiar a los usuarios paso a paso en procesos (registro, publicación, KYC, etc.).
3. Proporcionar información precisa y actualizada sobre la plataforma.
4. Evitar alucinaciones respondiendo SOLO con información contenida en estos documentos.

### Reglas para el SupportAgent:

- **SIEMPRE** identifica el tipo de usuario antes de responder.
- **SIEMPRE** cita la fuente (manual correspondiente).
- **NUNCA** inventa funcionalidades no documentadas.
- Si la información no está en los manuales, responde: "No tengo información disponible sobre eso. Te recomiendo contactar a nuestro equipo de soporte en soporte@okla.com.do."
