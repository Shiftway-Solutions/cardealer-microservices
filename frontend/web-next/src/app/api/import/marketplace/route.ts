/**
 * Facebook Marketplace Vehicle Import API Route (BFF)
 *
 * Accepts pasted text from a Facebook Marketplace listing and uses
 * the ChatbotService (Claude AI) to extract structured vehicle data.
 *
 * Flow:
 * 1. Dealer pastes the listing text/description from FB Marketplace
 * 2. This route sends it to ChatbotService for AI extraction
 * 3. Returns structured vehicle data to pre-fill the creation form
 *
 * @route POST /api/import/marketplace
 */

import { NextRequest, NextResponse } from 'next/server';

const INTERNAL_API_URL = process.env.INTERNAL_API_URL || 'http://gateway:8080';

interface ExtractedVehicle {
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  mileage: number | null;
  transmission: string | null;
  fuelType: string | null;
  bodyType: string | null;
  condition: string;
  color: string | null;
  description: string;
  location: string | null;
  province: string | null;
  features: string[];
  engineSize: string | null;
  doors: number | null;
  driveType: string | null;
  vin: string | null;
  imageUrls: string[];
  confidence: number;
  rawSource: string;
}

const EXTRACTION_PROMPT = `Eres un experto en vehículos del mercado dominicano. Tu tarea es extraer datos estructurados de un anuncio de vehículo copiado de Facebook Marketplace u otra plataforma.

INSTRUCCIONES:
1. Extrae todos los campos posibles del texto proporcionado
2. Normaliza los nombres de marcas y modelos (ej: "corola" → "Corolla", "rav4" → "RAV4")
3. Si el precio está en pesos dominicanos (RD$), usa currency "DOP". Si está en dólares (US$), usa "USD"
4. Si el kilometraje está en millas, conviértelo a kilómetros
5. Mapea la transmisión a: "Automatic", "Manual", "CVT", o "Other"
6. Mapea el combustible a: "Gasoline", "Diesel", "Electric", "Hybrid", "NaturalGas", "Other"
7. Mapea el tipo de carrocería a: "Sedan", "SUV", "Truck", "Coupe", "Convertible", "Van", "Wagon", "Hatchback", "Other"
8. Mapea la condición a: "New", "Used", "CertifiedPreOwned", "Salvage"
9. Identifica la provincia de RD si se menciona (Santo Domingo, Santiago, La Vega, etc.)
10. Extrae URLs de imágenes si están presentes
11. Asigna un nivel de confianza (0-100) basado en cuántos campos pudiste extraer con certeza

FORMATO DE RESPUESTA (JSON estricto, sin markdown):
{
  "make": "Toyota",
  "model": "Corolla",
  "year": 2020,
  "price": 850000,
  "currency": "DOP",
  "mileage": 45000,
  "transmission": "Automatic",
  "fuelType": "Gasoline",
  "bodyType": "Sedan",
  "condition": "Used",
  "color": "Blanco",
  "description": "Descripción limpia del vehículo...",
  "location": "Santo Domingo Este",
  "province": "Santo Domingo",
  "features": ["GPS", "Cámara de reversa", "Aros"],
  "engineSize": "1.8L",
  "doors": 4,
  "driveType": "FWD",
  "vin": null,
  "imageUrls": [],
  "confidence": 85,
  "rawSource": "facebook_marketplace"
}

IMPORTANTE: Responde SOLO con el JSON, sin explicaciones ni markdown.`;

export async function POST(request: NextRequest) {
  try {
    // Verify auth token
    const authHeader = request.headers.get('authorization');
    if (!authHeader) {
      return NextResponse.json(
        { success: false, error: 'Se requiere autenticación' },
        { status: 401 }
      );
    }

    const body = await request.json();
    const { text, url } = body as { text?: string; url?: string };

    if (!text && !url) {
      return NextResponse.json(
        { success: false, error: 'Se requiere el texto del anuncio o la URL' },
        { status: 400 }
      );
    }

    // If URL is provided, try to extract text from it
    let listingText = text || '';

    if (url && !text) {
      try {
        // Try to fetch the URL (may be blocked by Facebook)
        const pageResponse = await fetch(url, {
          headers: {
            'User-Agent':
              'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
          },
          signal: AbortSignal.timeout(10000),
        });

        if (pageResponse.ok) {
          const html = await pageResponse.text();
          // Extract text content from HTML (basic extraction)
          listingText = html
            .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
            .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
            .replace(/<[^>]+>/g, ' ')
            .replace(/\s+/g, ' ')
            .trim()
            .slice(0, 5000); // Limit to 5000 chars
        } else {
          return NextResponse.json(
            {
              success: false,
              error:
                'No se pudo acceder a la URL. Facebook Marketplace bloquea el acceso externo. Por favor, copia y pega el texto del anuncio directamente.',
              hint: 'Copia el texto del anuncio de Facebook y pégalo en el campo de texto.',
            },
            { status: 422 }
          );
        }
      } catch {
        return NextResponse.json(
          {
            success: false,
            error:
              'No se pudo acceder a la URL. Por favor, copia y pega el texto del anuncio directamente.',
            hint: 'Copia el texto del anuncio de Facebook y pégalo en el campo de texto.',
          },
          { status: 422 }
        );
      }
    }

    if (!listingText || listingText.length < 10) {
      return NextResponse.json(
        { success: false, error: 'El texto del anuncio es muy corto. Incluye más detalles.' },
        { status: 400 }
      );
    }

    // Call ChatbotService via gateway to extract vehicle data using Claude AI
    const chatPayload = {
      message: `Extrae los datos del vehículo de este anuncio de Facebook Marketplace:\n\n${listingText.slice(0, 4000)}`,
      sessionId: `import-${Date.now()}`,
      context: 'vehicle_import',
    };

    const chatResponse = await fetch(`${INTERNAL_API_URL}/api/chatbot/chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: authHeader,
      },
      body: JSON.stringify(chatPayload),
      signal: AbortSignal.timeout(30000),
    });

    let extractedData: ExtractedVehicle | null = null;

    if (chatResponse.ok) {
      const chatResult = await chatResponse.json();
      const responseText = chatResult.response || chatResult.data?.response || chatResult.message;

      if (responseText) {
        try {
          // Try to parse JSON from the response
          const jsonMatch = responseText.match(/\{[\s\S]*\}/);
          if (jsonMatch) {
            extractedData = JSON.parse(jsonMatch[0]) as ExtractedVehicle;
          }
        } catch {
          // If ChatbotService doesn't return structured data, try direct extraction
          console.warn('ChatbotService did not return parseable JSON, using fallback extraction');
        }
      }
    }

    // Fallback: basic text extraction if ChatbotService is unavailable or returns non-JSON
    if (!extractedData) {
      extractedData = fallbackExtraction(listingText);
    }

    return NextResponse.json({
      success: true,
      data: extractedData,
      message: 'Datos extraídos exitosamente. Por favor revisa y ajusta antes de publicar.',
    });
  } catch (error) {
    console.error('Import error:', error);
    return NextResponse.json(
      { success: false, error: 'Error interno al procesar el anuncio' },
      { status: 500 }
    );
  }
}

/**
 * Fallback extraction using regex patterns when ChatbotService is unavailable
 */
function fallbackExtraction(text: string): ExtractedVehicle {
  const upperText = text.toUpperCase();

  // Common DR vehicle brands
  const brands = [
    'Toyota',
    'Honda',
    'Hyundai',
    'Kia',
    'Nissan',
    'Mitsubishi',
    'Chevrolet',
    'Ford',
    'Jeep',
    'BMW',
    'Mercedes',
    'Mazda',
    'Suzuki',
    'Volkswagen',
    'Subaru',
    'Lexus',
    'Acura',
    'Audi',
    'Dodge',
    'RAM',
  ];

  // Extract brand
  const make =
    brands.find(b => upperText.includes(b.toUpperCase())) || '';

  // Extract year (4-digit number between 1990-2027)
  const yearMatch = text.match(/\b(19[9]\d|20[0-2]\d)\b/);
  const year = yearMatch ? parseInt(yearMatch[1]) : new Date().getFullYear();

  // Extract price (RD$ or US$ or just numbers)
  const priceMatch = text.match(
    /(?:RD\$|US\$|DOP|USD|\$)\s*([0-9]{1,3}(?:[,.]?[0-9]{3})*(?:\.\d{2})?)/i
  );
  let price = 0;
  let currency = 'DOP';
  if (priceMatch) {
    price = parseFloat(priceMatch[1].replace(/[,.]/g, ''));
    if (text.match(/US\$|USD/i)) currency = 'USD';
  }

  // Extract mileage
  const mileageMatch = text.match(/([0-9]{1,3}(?:[,.]?[0-9]{3})*)\s*(?:km|kms|kilómetros|kilometros)/i);
  const mileage = mileageMatch ? parseInt(mileageMatch[1].replace(/[,.]/g, '')) : null;

  // Extract transmission
  let transmission: string | null = null;
  if (/autom[áa]tic/i.test(text)) transmission = 'Automatic';
  else if (/manual|mec[áa]nic/i.test(text)) transmission = 'Manual';

  // Extract fuel type
  let fuelType: string | null = null;
  if (/gasolina|naft/i.test(text)) fuelType = 'Gasoline';
  else if (/diesel|di[ée]sel/i.test(text)) fuelType = 'Diesel';
  else if (/el[ée]ctric/i.test(text)) fuelType = 'Electric';
  else if (/h[ií]brid/i.test(text)) fuelType = 'Hybrid';

  // Extract color
  const colors = [
    'Blanco',
    'Negro',
    'Gris',
    'Plata',
    'Rojo',
    'Azul',
    'Verde',
    'Dorado',
    'Beige',
    'Marrón',
  ];
  const color = colors.find(c => upperText.includes(c.toUpperCase())) || null;

  // Extract province
  const provinces = [
    'Santo Domingo',
    'Santiago',
    'La Vega',
    'San Cristóbal',
    'Puerto Plata',
    'La Romana',
    'San Pedro de Macorís',
    'Duarte',
    'Espaillat',
    'La Altagracia',
  ];
  const province = provinces.find(p => upperText.includes(p.toUpperCase())) || null;

  return {
    make,
    model: '',
    year,
    price,
    currency,
    mileage,
    transmission,
    fuelType,
    bodyType: null,
    condition: 'Used',
    color,
    description: text.slice(0, 2000),
    location: null,
    province,
    features: [],
    engineSize: null,
    doors: null,
    driveType: null,
    vin: null,
    imageUrls: [],
    confidence: 30,
    rawSource: 'facebook_marketplace',
  };
}
