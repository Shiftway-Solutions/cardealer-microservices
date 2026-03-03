/**
 * Revalidation API Route
 *
 * ISR on-demand revalidation endpoint
 */

import { NextRequest, NextResponse } from 'next/server';
import { revalidatePath } from 'next/cache';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { path, secret } = body;

    // Verify secret token
    const expectedSecret = process.env.REVALIDATION_SECRET;
    if (expectedSecret && secret !== expectedSecret) {
      return NextResponse.json({ error: 'Invalid secret token' }, { status: 401 });
    }

    if (path) {
      revalidatePath(path);
      return NextResponse.json({
        revalidated: true,
        type: 'path',
        path,
        timestamp: new Date().toISOString(),
      });
    }

    return NextResponse.json({ error: 'Missing path parameter' }, { status: 400 });
  } catch (_error) {
    return NextResponse.json({ error: 'Invalid request body' }, { status: 400 });
  }
}
