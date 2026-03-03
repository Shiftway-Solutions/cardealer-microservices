import { NextRequest, NextResponse } from 'next/server';

// =============================================================================
// BFF: NHTSA vPIC — VIN Decode (FREE, no API key needed)
// =============================================================================
// Docs: https://vpic.nhtsa.dot.gov/api/
// =============================================================================

export async function GET(request: NextRequest) {
  const vin = request.nextUrl.searchParams.get('vin');

  if (!vin || vin.length !== 17) {
    return NextResponse.json(
      { success: false, error: 'VIN must be exactly 17 characters' },
      { status: 400 }
    );
  }

  try {
    const res = await fetch(
      `https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/${vin}?format=json`,
      { next: { revalidate: 86400 } } // cache 24h — VIN data doesn't change
    );

    if (!res.ok) {
      return NextResponse.json(
        { success: false, error: `NHTSA API error: ${res.status}` },
        { status: 502 }
      );
    }

    const data = await res.json();
    const results: { Variable: string; Value: string | null; ValueId: string | null }[] =
      data.Results || [];

    const get = (varName: string): string | null => {
      const entry = results.find(r => r.Variable === varName);
      return entry?.Value && entry.Value.trim() !== '' ? entry.Value.trim() : null;
    };
    const getNum = (varName: string): number | undefined => {
      const v = get(varName);
      return v ? parseInt(v, 10) || undefined : undefined;
    };

    const errorCode = get('Error Code');
    const errorText = get('Error Text');

    // Check for decoding errors (code != 0 means issues)
    if (errorCode && !['0', '6'].includes(errorCode)) {
      // Error code 6 = incomplete data but still decoded
      return NextResponse.json(
        {
          success: false,
          error: errorText || 'VIN decode failed',
          errorCode,
        },
        { status: 422 }
      );
    }

    const decoded = {
      vin,
      make: get('Make') || 'Unknown',
      model: get('Model') || 'Unknown',
      year: getNum('Model Year') || 0,
      trim: get('Trim'),
      bodyType: get('Body Class'),
      engineType: get('Engine Model') || get('Engine Configuration'),
      engineCylinders: getNum('Engine Number of Cylinders'),
      displacementL: (() => {
        const v = get('Displacement (L)');
        return v ? parseFloat(v) || undefined : undefined;
      })(),
      fuelType: get('Fuel Type - Primary'),
      transmission: get('Transmission Style'),
      drivetrain: get('Drive Type'),
      plantCountry: get('Plant Country'),
      plantCity: get('Plant City'),
      doors: getNum('Doors'),
      gvwr: get('Gross Vehicle Weight Rating From'),
      vehicleType: get('Vehicle Type'),
      errorCode,
      errorText,
    };

    return NextResponse.json({ success: true, data: decoded });
  } catch (error) {
    console.error('[NHTSA VIN Decode] Error:', error);
    return NextResponse.json({ success: false, error: 'Failed to decode VIN' }, { status: 500 });
  }
}
