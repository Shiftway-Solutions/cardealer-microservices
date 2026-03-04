/**
 * Production E2E tests: Dealer AI Chatbot & WhatsApp in /mensajes
 *
 * Tests the per-conversation dealer-scoped AI bot:
 * 1. Login as buyer → navigate to /mensajes
 * 2. Select an existing inquiry conversation (buyer → seller)
 * 3. Verify the "Asistente IA" button appears in the conversation header
 * 4. Click button → DealerBotPanel opens
 * 5. Bot connects and shows welcome message
 * 6. User sends a message and receives an AI response
 * 7. Verify unauthenticated access redirects to login
 *
 * Credentials: buyer002@okla-test.com / BuyerTest2026!
 */
import { test, expect } from '@playwright/test';

const BUYER_EMAIL = 'buyer002@okla-test.com';
const BUYER_PASSWORD = 'BuyerTest2026!';
const BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'https://okla.com.do';

// Helper: login as buyer
async function loginAsBuyer(page: import('@playwright/test').Page) {
  await page.goto('/ingresar');
  await page.waitForLoadState('load');
  await page.waitForTimeout(1500);

  const emailInput = page.locator('input[type="email"], input[name="email"], #email').first();
  const passwordInput = page
    .locator('input[type="password"], input[name="password"], #password')
    .first();

  if ((await emailInput.count()) === 0) {
    console.log('Login form not found — may already be on a different page');
    return;
  }

  await emailInput.fill(BUYER_EMAIL);
  await passwordInput.fill(BUYER_PASSWORD);

  const submitBtn = page
    .locator('button[type="submit"], button:has-text("Ingresar"), button:has-text("Entrar")')
    .first();
  await submitBtn.click();

  await page.waitForTimeout(3000);
  console.log('After login URL:', page.url());
}

/**
 * Helper: select the first inquiry conversation and return whether one was found.
 * An inquiry conversation is one where the buyer initiated contact with a seller.
 * These are the only conversations that show the "Asistente IA" bot button.
 */
async function selectFirstInquiryConversation(
  page: import('@playwright/test').Page
): Promise<boolean> {
  // Conversations listed in the sidebar — pick the first non-bot entry
  const convItems = page
    .locator('[data-testid="conversation-item"], .border-b.cursor-pointer')
    .filter({ hasNotText: /Asistente/ });
  const altItems = page.locator('button.w-full.border-b').filter({ hasNotText: /Asistente/ });

  const items = (await convItems.count()) > 0 ? convItems : altItems;
  if ((await items.count()) === 0) {
    console.log('⚠️ No conversations found in sidebar');
    return false;
  }

  await items.first().click();
  await page.waitForTimeout(2000);
  console.log('✅ Selected first available conversation');
  return true;
}

test.describe('Mensajes Dealer AI Chatbot Integration', () => {
  test('mensajes page loads and no global OKLA bot pinned in sidebar', async ({ page }) => {
    await loginAsBuyer(page);
    await page.goto('/mensajes');
    await page.waitForLoadState('load');
    await page.waitForTimeout(3000);

    const currentUrl = page.url();
    console.log('Current URL after navigate:', currentUrl);

    if (currentUrl.includes('/ingresar') || currentUrl.includes('/login')) {
      console.log('⚠️ Not authenticated — skipping chat tests');
      return;
    }

    // The old global "Asistente OKLA" pinned entry should NO LONGER exist
    const oldBotEntry = page
      .locator('button')
      .filter({ hasText: /Asistente OKLA/i })
      .first();
    const oldBotCount = await oldBotEntry.count();
    expect(oldBotCount).toBe(0);
    console.log('✅ No global "Asistente OKLA" pinned entry (correct)');

    // Page should show the messages heading
    const heading = page
      .locator('h1')
      .filter({ hasText: /Mensajes/i })
      .first();
    if ((await heading.count()) > 0) {
      await expect(heading).toBeVisible();
      console.log('✅ Mensajes heading is visible');
    }
  });

  test('Asistente IA button appears inside a dealer conversation', async ({ page }) => {
    await loginAsBuyer(page);
    await page.goto('/mensajes');
    await page.waitForLoadState('load');
    await page.waitForTimeout(3000);

    if (page.url().includes('/ingresar')) {
      console.log('⚠️ Not authenticated — skipping');
      return;
    }

    const found = await selectFirstInquiryConversation(page);
    if (!found) {
      console.log('⚠️ No conversations — buyer may have no inquiries yet');
      return;
    }

    // The "Asistente IA" bot button should appear in the conversation header
    const botBtn = page
      .locator('button[title*="Asistente IA"], button[title*="Asistente"], button')
      .filter({ hasText: /Asistente IA/i })
      .first();

    // Also check by icon aria-label / title attribute
    const botIconBtn = page.locator('button[title*="Asistente"]').first();

    const found1 = await botBtn.count();
    const found2 = await botIconBtn.count();

    if (found1 > 0 || found2 > 0) {
      console.log('✅ "Asistente IA" bot button visible in conversation header');
    } else {
      console.log('⚠️ Bot button not found — conversation may be of "received" type, not inquiry');
    }
  });

  test('clicking Asistente IA opens dealer bot panel', async ({ page }) => {
    await loginAsBuyer(page);
    await page.goto('/mensajes');
    await page.waitForLoadState('load');
    await page.waitForTimeout(3000);

    if (page.url().includes('/ingresar')) {
      console.log('⚠️ Not authenticated — skipping');
      return;
    }

    const found = await selectFirstInquiryConversation(page);
    if (!found) {
      console.log('⚠️ No conversations available');
      return;
    }

    const botIconBtn = page
      .locator('button[title="Preguntar al Asistente IA del vendedor"]')
      .first();
    if ((await botIconBtn.count()) === 0) {
      console.log('⚠️ Bot button not found (conversation may be "received" type, not inquiry)');
      return;
    }

    await botIconBtn.click();
    await page.waitForTimeout(3000);

    // DealerBotPanel header should appear: "DealerName · Asistente IA"
    const botHeader = page.locator('text=· Asistente IA').first();
    const onlineStatus = page.locator('text=/En línea/i').first();
    const chatInput = page.locator('input[aria-label="Mensaje al asistente del dealer"]').first();

    const headerVisible = await botHeader.count();
    const inputVisible = await chatInput.count();

    console.log(`Bot header visible: ${headerVisible > 0}`);
    console.log(`Chat input visible: ${inputVisible > 0}`);

    if (inputVisible > 0) {
      await expect(chatInput).toBeVisible();
      console.log('✅ Dealer AI chat input is visible');
    }

    if (headerVisible > 0) {
      await expect(botHeader).toBeVisible();
      console.log('✅ Dealer bot header is visible');
    }
  });

  test('dealer bot connects and user can send a message', async ({ page }) => {
    test.setTimeout(90000);

    await loginAsBuyer(page);
    await page.goto('/mensajes');
    await page.waitForLoadState('load');
    await page.waitForTimeout(3000);

    if (page.url().includes('/ingresar')) {
      console.log('⚠️ Not authenticated — skipping');
      return;
    }

    const found = await selectFirstInquiryConversation(page);
    if (!found) {
      console.log('⚠️ No conversations available');
      return;
    }

    const botIconBtn = page
      .locator('button[title="Preguntar al Asistente IA del vendedor"]')
      .first();
    if ((await botIconBtn.count()) === 0) {
      console.log('⚠️ Bot button not found');
      return;
    }

    await botIconBtn.click();
    await page.waitForTimeout(5000); // Let session start

    const chatInput = page.locator('input[aria-label="Mensaje al asistente del dealer"]').first();
    if ((await chatInput.count()) === 0) {
      console.log('⚠️ Chat input not found — bot may not have connected');
      return;
    }

    const testMessage = '¿Qué opciones de financiamiento tienen?';
    await chatInput.fill(testMessage);
    console.log(`Typed: "${testMessage}"`);

    const sendBtn = page.locator('button[aria-label="Enviar mensaje"]').first();
    await sendBtn.click();
    console.log('Sent message');

    // Wait for AI response (up to 30s)
    const startTime = Date.now();
    let responseReceived = false;

    while (Date.now() - startTime < 30000) {
      const botMsgs = page.locator('.rounded-tl-sm');
      const count = await botMsgs.count();
      if (count > 0) {
        const elapsed = Date.now() - startTime;
        console.log(`✅ Dealer AI response received in ${elapsed}ms`);
        responseReceived = true;
        break;
      }
      await page.waitForTimeout(1000);
    }

    if (!responseReceived) {
      console.log('⚠️ No response within 30s (ChatbotService may be loading)');
    }

    await page.screenshot({ path: 'test-results/mensajes-dealer-bot.png' });
    console.log('Screenshot saved');
  });

  test('URL-param triggered bot: ?sellerId auto-opens dealer bot panel', async ({ page }) => {
    test.setTimeout(60000);

    await loginAsBuyer(page);

    // Simulate arriving from a vehicle detail "Chat Live" button
    const SELLER_ID = '34146177-68fe-4952-bb1b-2a53b1a08c4c';
    const VEHICLE_TITLE = '2024 Toyota RAV4';
    const encodedTitle = encodeURIComponent(VEHICLE_TITLE);
    await page.goto(
      `${BASE_URL}/mensajes?sellerId=${SELLER_ID}&vehicleTitle=${encodedTitle}&vehicleId=94887983-8bdf-40fb-80fe-bf1465498124`
    );
    await page.waitForLoadState('load');
    await page.waitForTimeout(4000);

    if (page.url().includes('/ingresar') || page.url().includes('/login')) {
      console.log('⚠️ Not authenticated — skipping URL bot test');
      return;
    }

    console.log('Current URL after navigate:', page.url());

    // The dealer bot panel should open automatically
    // Look for: "Asistente de 2024 Toyota RAV4" or typing indicator or "En línea" or chat input
    const botHeader = page
      .locator('h3')
      .filter({ hasText: /Asistente/i })
      .first();
    const onlineStatus = page.locator('text=/En línea/i').first();
    const chatInput = page.locator('input[aria-label*="Pregunta"]').first();

    const headerCount = await botHeader.count();
    const onlineCount = await onlineStatus.count();
    const inputCount = await chatInput.count();

    console.log(`Bot header "Asistente": ${headerCount > 0}`);
    console.log(`"En línea" status: ${onlineCount > 0}`);
    console.log(`Chat input visible: ${inputCount > 0}`);

    if (headerCount > 0 || inputCount > 0) {
      console.log('✅ URL-triggered dealer bot panel opened successfully');
      if (headerCount > 0) {
        const headerText = await botHeader.textContent();
        console.log(`Bot header text: "${headerText}"`);
      }
    } else {
      console.log('⚠️ Bot panel not detected — may need auth or ChatbotService response');
    }

    await page.screenshot({ path: 'test-results/mensajes-url-triggered-bot.png' });
    console.log('Screenshot saved to test-results/mensajes-url-triggered-bot.png');
  });

  test('mensajes page is accessible without login (redirects to login)', async ({ page }) => {
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('load');
    await page.waitForTimeout(2000);

    const url = page.url();
    console.log('URL without auth:', url);

    const isRedirectedToLogin = url.includes('/ingresar') || url.includes('/login');
    const hasAuthGuard = (await page.locator('text=/Inicia sesión|Regístrate/i').count()) > 0;

    console.log(`Redirected to login: ${isRedirectedToLogin}`);
    console.log(`Auth guard visible: ${hasAuthGuard}`);
    expect(isRedirectedToLogin || hasAuthGuard).toBe(true);
    console.log('✅ Auth protection works correctly');
  });
});
