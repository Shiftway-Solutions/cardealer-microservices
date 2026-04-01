using System.Text.RegularExpressions;

namespace SupportAgent.Application.Services;

/// <summary>
/// Provides deterministic FAQ responses for common OKLA support questions.
/// Used as primary response layer before the Claude API call, ensuring the service
/// remains functional even when the Claude API key is unavailable or exhausted.
/// </summary>
public static class LocalFaqMatcher
{
    private static readonly List<(Regex Pattern, string Response)> Faqs =
    [
        // FAQ 1: Cómo publicar un vehículo
        (
            new Regex(
                @"c[oó]mo\s+(publico|publicar|vender|poner|anunciar|subir|crear).*(veh[íi]culo|carro|auto|moto)|publicar.*(veh[íi]culo|carro|auto)|anuncio.*(veh[íi]culo|carro)|(quiero|quisiera|necesito)\s+(poner|vender|publicar|anunciar)\s+(mi\s+)?(veh[íi]culo|carro|auto|moto)|vendo\s+(mi\s+)?(veh[íi]culo|carro|auto)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "📋 **Para publicar tu vehículo en OKLA:**\n\n1. Ve a okla.com.do/publicar\n2. Necesitas ser Seller (si eres Buyer, convierte tu cuenta en okla.com.do/cuenta/convert-to-seller)\n3. Completa el formulario: marca, modelo, año, precio y fotos (mín. 8 fotos)\n4. Paga RD$1,699 — incluye 45 días de publicación activa\n5. Tu anuncio queda visible en minutos\n\n¿Necesitas más detalle de algún paso? 😊"
        ),

        // FAQ 2: Cambiar / recuperar contraseña
        (
            new Regex(
                @"(cambiar?|cambio|actualizar?|restablecer?|recuperar?|olvid[eé])\s+(mi\s+)?contrase[ñn]a|contrase[ñn]a.*(olvidada?|perdida?|cambio|actualizar?)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "🔑 **Para cambiar tu contraseña:**\n\n1. Ve a okla.com.do/cuenta/seguridad\n2. Sección 'Contraseña' → 'Cambiar'\n3. Ingresa la contraseña actual y la nueva (mín. 8 caracteres, con mayúscula, número y símbolo especial)\n\n**Si la olvidaste:**\n1. Ve a okla.com.do/recuperar-contrasena\n2. Ingresa tu email — recibirás un enlace de recuperación\n3. Al cambiarla, **todas tus sesiones activas se cierran** automáticamente\n\n¿Algo más en lo que te pueda ayudar? 😊"
        ),

        // FAQ 3: Cuánto cuesta / planes y precios
        (
            new Regex(
                @"cu[aá]nto\s+(cuesta|vale|cobran?|es\s+el\s+precio|es\s+el\s+costo)|planes?\s+(de\s+okla|disponibles?|para\s+publicar|del\s+dealer)|precios?\s+(de\s+okla|para\s+publicar)|tarifas?\s+(de\s+okla|para\s+publicar)|cuales\s+son\s+los\s+(planes?|precios?)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "💰 **Planes y precios de OKLA:**\n\n**Vendedores individuales:**\n• Publicar 1 vehículo: **RD$1,699** (45 días)\n• Boost Básico: **RD$499** (7 días, mayor visibilidad)\n• Boost Premium: **RD$1,499** (30 días, destacado + visibilidad máxima)\n\n**Concesionarios (Dealers):**\n• Starter: **RD$2,899/mes** — hasta 20 vehículos\n• Pro: **RD$7,499/mes** — hasta 75 vehículos + CRM\n• Enterprise: **RD$17,499/mes** — ilimitados + API\n\n**Compradores:** ¡Gratis siempre! 🎉\n\nMás detalles en okla.com.do/precios. ¿Tienes dudas sobre algún plan? 😊"
        ),

        // FAQ 4: Estafa / fraude con vehículo
        (
            new Regex(
                @"(me\s+)?(estafaron?|robaron?|enga[ñn]aron?|timaron?)\s.*(veh[íi]culo|carro|auto|compra)|fraude.*(veh[íi]culo|carro|compra)|(veh[íi]culo|carro|auto).*(fraude|estafa|robo|enga[ñn]o)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "🚨 **Lamento que hayas pasado por eso. Esto es lo que debes hacer:**\n\n**Pasos inmediatos:**\n1. Reporta al vendedor en OKLA: okla.com.do/reportar\n2. Guarda toda la evidencia (capturas, mensajes, recibos, contratos)\n3. Reporta a Proconsumidor: proconsumidor.gob.do\n4. Si es fraude significativo, contacta el PEPCA (Ministerio Público)\n\n**Contacta al equipo OKLA:**\n📧 soporte@okla.com.do\n📱 WhatsApp: +1 (809) 555-OKLA (L-V 8AM-6PM AST)\n\nNuestro equipo puede investigar y suspender cuentas fraudulentas. ¿Puedo ayudarte con algo más?"
        ),

        // FAQ 5: Hablar con persona / agente humano
        (
            new Regex(
                @"(quiero|necesito|quisiera)\s+(hablar?|contactar?|comunicarme?|escribirle?)\s+(con\s+)?(una?\s+)?(persona|humano|agente|representante|alguien)|(hablar?|contactar?)\s+(con\s+)?(una?\s+)?(persona|humano|agente|soporte\s+humano)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "👤 **Te pongo en contacto con nuestro equipo:**\n\n📧 **Email:** soporte@okla.com.do\n_(Respuesta en menos de 24 horas hábiles)_\n\n📱 **WhatsApp:** +1 (809) 555-OKLA\nHorario: Lunes-Viernes 8AM-6PM | Sábados 9AM-1PM (AST)\n\n🌐 **Formulario web:** okla.com.do/contacto\n\n¿Hay algo más en lo que te pueda ayudar mientras tanto? 🙌"
        ),

        // FAQ 6: ¿Qué es OKLA Score?
        (
            new Regex(
                @"(qu[eé]\s+es|explica(me)?|c[oó]mo\s+funciona|para\s+qu[eé]\s+(sirve|es))\s+(el\s+)?okla\s+score|okla\s+score\s+(qu[eé]|c[oó]mo|para\s+qu[eé])",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "⭐ **OKLA Score** es el puntaje de reputación de los vendedores en la plataforma.\n\nRefleja:\n• Historial de ventas exitosas\n• Velocidad de respuesta a compradores\n• Verificación KYC completada\n• Calificaciones recibidas de compradores\n• Antigüedad en la plataforma\n\n**Mayor Score = vendedor más confiable.**\nTe ayuda a identificar vendedores serios antes de contactarlos o realizar una compra.\n\n¿Necesitas ayuda con algo más? 😊"
        ),

        // FAQ 7: ¿OKLA garantiza el vehículo?
        (
            new Regex(
                @"okla\s+(garantiza?|es\s+responsable|responde|avala?)|(garantía|garantia|responsabilidad)\s+(de\s+)?okla|okla\s+garantiza?\s+el\s+(veh[íi]culo|carro)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "⚠️ **Respuesta honesta sobre garantías:**\n\nOKLA es una **plataforma de intermediación** — conecta compradores y vendedores, pero **no garantiza el estado mecánico del vehículo** ni la exactitud de toda la información del anuncio.\n\n**Lo que SÍ hace OKLA:**\n• Verifica la identidad de vendedores con KYC\n• Asigna OKLA Score basado en su reputación\n• Permite reportar y suspender vendedores fraudulentos\n• Facilita acceso al historial y documentación del vehículo\n\n**Recomendaciones antes de comprar:**\n1. Solicita inspección mecánica independiente\n2. Verifica documentos con DGII (dgii.gov.do) e INTRANT (intrant.gob.do)\n3. No pagues sin ver físicamente el vehículo\n4. Usa canales de pago seguros y documentados\n\n¿Tienes más preguntas sobre el proceso de compra? 😊"
        ),

        // FAQ 8: Documentos para comprar un vehículo en RD
        (
            new Regex(
                @"(qu[eé]|cu[aá]les)\s+documentos?.*(necesito|requieren?|piden?).*(comprar?|traspaso|transferencia)|(documentos?|papeles?).*(comprar?|traspaso|transferencia).*(veh[íi]culo|carro|auto)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "📄 **Documentos para la compra de un vehículo en RD:**\n\n**Del comprador:**\n• Cédula de identidad o pasaporte vigente\n• Si financias: constancia laboral + estados de cuenta bancarios\n\n**Exige siempre al vendedor:**\n• Cédula del vendedor\n• Matrícula del vehículo vigente\n• Certificado de titularidad (INTRANT: intrant.gob.do)\n• Certificado de deuda (DGII: dgii.gov.do)\n• Últimos 3 recibos de seguro (Ley 146-02)\n\n**Para el traspaso oficial:**\n• Contrato de compraventa notariado (Ley 489-08)\n• Pago del impuesto de traspaso ante la DGII\n\n💡 Verifica siempre titularidad y deudas antes de pagar. ¿Algo más en lo que te ayude?"
        ),
    ];

    /// <summary>
    /// Attempts to match a user message against the local FAQ bank.
    /// Returns <c>null</c> if no match is found — caller should fall through to Claude API.
    /// </summary>
    public static string? TryMatch(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return null;

        foreach (var (pattern, response) in Faqs)
        {
            if (pattern.IsMatch(userMessage))
                return response;
        }

        return null;
    }
}
