/**
 * Measure homepage NLP search performance
 */

import { test, expect } from '@playwright/test';

test('measure NLP search speed on homepage', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(2000);

  // Find the search input on the homepage
  const searchInput = page
    .locator(
      'input[placeholder*="busca"], input[placeholder*="Search"], input[placeholder*="Busca"], input[type="search"], textarea[placeholder*="busca"]'
    )
    .first();

  const altSearchInput = page
    .locator(
      'input[placeholder*="SUV"], input[placeholder*="auto"], input[placeholder*="vehículo"], input[placeholder*="¿Qué"]'
    )
    .first();

  let inputFound = false;
  if ((await searchInput.count()) > 0) {
    await searchInput.scrollIntoViewIfNeeded();
    inputFound = true;
  } else if ((await altSearchInput.count()) > 0) {
    await altSearchInput.scrollIntoViewIfNeeded();
    inputFound = true;
  }

  console.log(`Search input found: ${inputFound}`);

  if (inputFound) {
    const targetInput = (await searchInput.count()) > 0 ? searchInput : altSearchInput;

    // Time the search
    const searchStartTime = Date.now();

    await targetInput.fill('Toyota Corolla 2022');
    await page.keyboard.press('Enter');

    // Wait for navigation to /vehiculos
    await page.waitForURL(/\/vehiculos/, { timeout: 30000 });

    const searchEndTime = Date.now();
    const searchDuration = searchEndTime - searchStartTime;

    console.log(`Search took: ${searchDuration}ms to navigate to /vehiculos`);
    console.log(`URL after search: ${page.url()}`);

    // Check that results are present
    await page.waitForLoadState('domcontentloaded');
    const vehicleCards = page.locator('a[href*="/vehiculos/"]');
    const count = await vehicleCards.count();
    console.log(`Vehicles found after search: ${count}`);

    expect(page.url()).toContain('/vehiculos');
    console.log(`✓ SEARCH PERFORMANCE: ${searchDuration}ms`);
  } else {
    // List all inputs for debugging
    const allInputs = await page.locator('input').all();
    for (const input of allInputs) {
      const placeholder = await input.getAttribute('placeholder');
      const type = await input.getAttribute('type');
      console.log(`Input: type=${type}, placeholder=${placeholder}`);
    }
    console.log('Search input not found - checking for NLP search button');
  }
});
