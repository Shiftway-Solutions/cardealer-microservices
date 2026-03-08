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

const _EXTRACTION_PROMPT = `Eres un experto en vehículos del mercado dominicano. Tu tarea es extraer datos estructurados de un anuncio de vehículo copiado de Facebook Marketplace u otra plataforma.

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
    // AUDIT FIX: Include the dedicated extraction system prompt instead of a generic message.
    // This ensures structured JSON output with DR market normalization.
    const chatPayload = {
      message: `${_EXTRACTION_PROMPT}\n\n--- ANUNCIO A PROCESAR ---\n\n${listingText.slice(0, 4000)}`,
      sessionId: `import-${Date.now()}`,
      context: 'vehicle_import',
      systemPrompt: _EXTRACTION_PROMPT,
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
 * Fallback extraction using regex patterns when ChatbotService is unavailable.
 * AUDIT FIX: Added model detection, body type detection, VIN extraction, and
 * improved confidence scoring for Dominican Republic vehicle market.
 */
function fallbackExtraction(text: string): ExtractedVehicle {
  const upperText = text.toUpperCase();
  let confidence = 10; // Start low, increase with each detected field

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
    'Infiniti',
    'Volvo',
    'Land Rover',
    'Porsche',
    'Mini',
    'Fiat',
    'Peugeot',
    'Renault',
    'Genesis',
    'Lincoln',
    'Cadillac',
    'Buick',
    'GMC',
    'Chrysler',
  ];

  // Popular models in DR market (brand → model patterns)
  const modelPatterns: Record<string, string[]> = {
    Toyota: [
      'Corolla',
      'Camry',
      'RAV4',
      'Rav4',
      'Hilux',
      'Yaris',
      'Prado',
      'Land Cruiser',
      'Rush',
      'Fortuner',
      'C-HR',
      'Tacoma',
      '4Runner',
      'Highlander',
      'Supra',
      'GR86',
      'Tundra',
      'Prius',
    ],
    Honda: [
      'Civic',
      'Accord',
      'CR-V',
      'CRV',
      'HR-V',
      'HRV',
      'Fit',
      'City',
      'Pilot',
      'Odyssey',
      'Ridgeline',
    ],
    Hyundai: [
      'Tucson',
      'Santa Fe',
      'Elantra',
      'Sonata',
      'Accent',
      'Kona',
      'Palisade',
      'Venue',
      'Creta',
      'Grand i10',
    ],
    Kia: [
      'Sportage',
      'Seltos',
      'Forte',
      'Sorento',
      'Rio',
      'K5',
      'Soul',
      'Carnival',
      'Stinger',
      'Telluride',
      'Picanto',
    ],
    Nissan: [
      'Kicks',
      'Sentra',
      'X-Trail',
      'Pathfinder',
      'Frontier',
      'Versa',
      'Altima',
      'Rogue',
      'Murano',
      'Navara',
      'Qashqai',
    ],
    Mitsubishi: [
      'Outlander',
      'ASX',
      'L200',
      'Montero',
      'Eclipse Cross',
      'Mirage',
      'Pajero',
      'Triton',
    ],
    Jeep: [
      'Wrangler',
      'Grand Cherokee',
      'Cherokee',
      'Compass',
      'Renegade',
      'Gladiator',
      'Commander',
    ],
    BMW: [
      'X1',
      'X3',
      'X5',
      'Serie 3',
      'Serie 5',
      '320i',
      '330i',
      '520i',
      'X6',
      'X7',
      'Z4',
      'M3',
      'M5',
    ],
    Mercedes: [
      'C200',
      'C300',
      'E300',
      'GLC',
      'GLE',
      'CLA',
      'A200',
      'Clase C',
      'Clase E',
      'GLA',
      'GLB',
      'AMG',
    ],
    Ford: [
      'Explorer',
      'Escape',
      'Bronco',
      'Ranger',
      'F-150',
      'Maverick',
      'Edge',
      'Expedition',
      'Mustang',
    ],
    Chevrolet: [
      'Equinox',
      'Tracker',
      'Silverado',
      'Trax',
      'Blazer',
      'Tahoe',
      'Suburban',
      'Traverse',
      'Colorado',
      'Onix',
    ],
    Mazda: ['CX-5', 'CX-30', 'CX-50', 'Mazda3', 'Mazda6', 'CX-3', 'CX-9', 'MX-5'],
    Audi: ['Q3', 'Q5', 'Q7', 'A3', 'A4', 'A5', 'A6', 'Q8', 'e-tron', 'RS'],
    Volkswagen: ['Tiguan', 'Jetta', 'Golf', 'Taos', 'Atlas', 'ID.4', 'T-Cross', 'Passat'],
    Dodge: ['Durango', 'Charger', 'Challenger', 'Journey'],
  };

  // Extract brand
  const make = brands.find(b => upperText.includes(b.toUpperCase())) || '';
  if (make) confidence += 15;

  // Extract model based on detected brand
  let model = '';
  if (make && modelPatterns[make]) {
    const foundModel = modelPatterns[make].find(m => upperText.includes(m.toUpperCase()));
    if (foundModel) {
      model = foundModel;
      confidence += 15;
    }
  }

  // If no brand-specific model found, try generic model detection
  if (!model) {
    const allModels = Object.values(modelPatterns).flat();
    const foundModel = allModels.find(m => upperText.includes(m.toUpperCase()));
    if (foundModel) {
      model = foundModel;
      confidence += 10;
    }
  }

  // Extract year (4-digit number between 1990-2027)
  const yearMatch = text.match(/\b(19[9]\d|20[0-2]\d)\b/);
  const year = yearMatch ? parseInt(yearMatch[1]) : new Date().getFullYear();
  if (yearMatch) confidence += 10;

  // Extract price (RD$ or US$ or just numbers)
  const priceMatch = text.match(
    /(?:RD\$|US\$|DOP|USD|\$)\s*([0-9]{1,3}(?:[,.]?[0-9]{3})*(?:\.\d{2})?)/i
  );
  let price = 0;
  let currency = 'DOP';
  if (priceMatch) {
    price = parseFloat(priceMatch[1].replace(/[,.]/g, ''));
    if (text.match(/US\$|USD/i)) currency = 'USD';
    confidence += 10;
  }

  // Extract mileage
  const mileageMatch = text.match(
    /([0-9]{1,3}(?:[,.]?[0-9]{3})*)\s*(?:km|kms|kilómetros|kilometros|millas|miles)/i
  );
  let mileage: number | null = null;
  if (mileageMatch) {
    mileage = parseInt(mileageMatch[1].replace(/[,.]/g, ''));
    // Convert miles to km if needed
    if (/millas|miles/i.test(text)) mileage = Math.round(mileage * 1.60934);
    confidence += 5;
  }

  // Extract transmission
  let transmission: string | null = null;
  if (/autom[áa]tic/i.test(text)) {
    transmission = 'Automatic';
    confidence += 5;
  } else if (/manual|mec[áa]nic/i.test(text)) {
    transmission = 'Manual';
    confidence += 5;
  } else if (/cvt/i.test(text)) {
    transmission = 'CVT';
    confidence += 5;
  }

  // Extract fuel type
  let fuelType: string | null = null;
  if (/gasolina|naft/i.test(text)) {
    fuelType = 'Gasoline';
    confidence += 5;
  } else if (/diesel|di[ée]sel/i.test(text)) {
    fuelType = 'Diesel';
    confidence += 5;
  } else if (/el[ée]ctric/i.test(text)) {
    fuelType = 'Electric';
    confidence += 5;
  } else if (/h[ií]brid/i.test(text)) {
    fuelType = 'Hybrid';
    confidence += 5;
  }

  // Extract body type
  let bodyType: string | null = null;
  if (/suv|crossover/i.test(text)) bodyType = 'SUV';
  else if (/sedan|sed[áa]n/i.test(text)) bodyType = 'Sedan';
  else if (/pickup|camioneta|truck/i.test(text)) bodyType = 'Truck';
  else if (/coupe|coup[ée]/i.test(text)) bodyType = 'Coupe';
  else if (/van|minivan/i.test(text)) bodyType = 'Van';
  else if (/hatchback/i.test(text)) bodyType = 'Hatchback';
  else if (/convertible|descapotable/i.test(text)) bodyType = 'Convertible';
  if (bodyType) confidence += 5;

  // Extract VIN (17-character alphanumeric, excluding I, O, Q)
  const vinMatch = text.match(/\b[A-HJ-NPR-Z0-9]{17}\b/);
  const vin = vinMatch ? vinMatch[0] : null;
  if (vin) confidence += 10;

  // Extract color
  const colors = [
    'Blanco',
    'Negro',
    'Gris',
    'Plata',
    'Plateado',
    'Rojo',
    'Azul',
    'Verde',
    'Dorado',
    'Beige',
    'Marrón',
    'Naranja',
    'Champagne',
    'Perla',
    'Vino',
  ];
  const color = colors.find(c => upperText.includes(c.toUpperCase())) || null;
  if (color) confidence += 3;

  // Extract province (Dominican Republic)
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
    'Distrito Nacional',
    'San Juan',
    'Azua',
    'Barahona',
    'Monte Plata',
    'Higüey',
    'Boca Chica',
    'Los Alcarrizos',
    'Punta Cana',
  ];
  const province = provinces.find(p => upperText.includes(p.toUpperCase())) || null;
  if (province) confidence += 3;

  // Extract engine size
  const engineMatch = text.match(/(\d\.\d)\s*(?:L|litros?|lts?)/i);
  const engineSize = engineMatch ? `${engineMatch[1]}L` : null;

  // Extract features
  const featureKeywords = [
    'GPS',
    'Cámara',
    'Reversa',
    'Bluetooth',
    'Aros',
    'Sunroof',
    'Techo',
    'Cuero',
    'Pantalla',
    'Apple CarPlay',
    'Android Auto',
    'Sensores',
    'Llave inteligente',
    'Arranque',
    'Cruise',
    'Asientos calefacción',
  ];
  const features = featureKeywords.filter(f => upperText.includes(f.toUpperCase()));

  confidence = Math.min(confidence, 95); // Cap at 95 for fallback

  return {
    make,
    model,
    year,
    price,
    currency,
    mileage,
    transmission,
    fuelType,
    bodyType,
    condition: 'Used',
    color,
    description: text.slice(0, 2000),
    location: null,
    province,
    features,
    engineSize,
    doors: null,
    driveType: null,
    vin,
    imageUrls: [],
    confidence,
    rawSource: 'facebook_marketplace',
  };
}
