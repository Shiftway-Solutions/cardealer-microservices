/**
 * Application-wide constants for the OKLA platform.
 */

/**
 * Default DOP/USD exchange rate used as FALLBACK for OKLA Score price analysis.
 *
 * The BFF route (api/score/calculate) now fetches live rates from
 * ExchangeRate-API with a 4-hour cache. This constant is only used when
 * the live fetch fails. Updated 2026-03 from BCRD reference rate.
 *
 * @see frontend/web-next/src/app/api/score/calculate/route.ts fetchExchangeRate()
 */
export const DOP_USD_EXCHANGE_RATE = 60.5;
