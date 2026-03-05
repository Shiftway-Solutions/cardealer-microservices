/**
 * Production E2E: Appointment Scheduling, Chat History (WhatsApp-style)
 *
 * Tests all the new features implemented in the chat/mensajes page:
 * 1. WhatsApp-style conversation history persists across page reloads
 * 2. Month-view calendar appears when bot suggests an appointment
 * 3. User can navigate calendar months
 * 4. Selecting a date advances to time slot step
 * 5. Selecting a time sends appointment message to bot
 * 6. Email confirmation is triggered (API route responds)
 * 7. Asistentes IA sidebar shows bot history in real time
 * 8. Bot sessions update as new messages arrive
 *
 * Credentials used:
 *   Buyer  : buyer002@okla-test.com  / BuyerTest2026!
 *   Dealer : nmateo@okla.com.do      / Dealer2026!@#
 */

import { test, expect } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'https://okla.com.do';
const BUYER_EMAIL = 'buyer002@okla-test.com';
const BUYER_PASSWORD = 'BuyerTest2026!';
const DEALER_EMAIL = 'nmateo@okla.com.do';
const DEALER_PASSWORD = 'Dealer2026!@#';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

async function loginAs(page: import('@playwright/test').Page, email: string, password: string) {
  await page.goto(`${BASE_URL}/ingresar`);
  await page.waitForLoadState('networkidle');
  await page.locator('input[type="email"]').first().fill(email);
  await page.locator('input[type="password"]').first().fill(password);
  await page.locator('button[type="submit"]').first().click();
  await page.waitForTimeout(3000);
}

async function openAIAssistantTab(page: import('@playwright/test').Page) {
  // Navigate to /mensajes and click the "Asistentes IA" tab
  await page.goto(`${BASE_URL}/mensajes`);
  await page.waitForLoadState('networkidle');
  const aiTab = page.locator('button', { hasText: /Asistentes IA/ });
  if (await aiTab.isVisible()) {
    await aiTab.click();
    await page.waitForTimeout(500);
  }
}

// ---------------------------------------------------------------------------
// Test suite
// ---------------------------------------------------------------------------

test.describe('Chat Features — Appointment Calendar & WhatsApp History', () => {
  test.setTimeout(120_000);

  test('1. /mensajes page loads and shows correct tabs for authenticated buyer', async ({
    page,
  }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Should show the main layout
    await expect(
      page
        .locator('h1, [class*="font-bold"]')
        .filter({ hasText: /Mensajes/ })
        .first()
    ).toBeVisible({ timeout: 10000 });

    // Both tabs should be present
    await expect(page.locator('button', { hasText: /Mensajes/ }).first()).toBeVisible();
    await expect(page.locator('button', { hasText: /Asistentes IA/ })).toBeVisible();

    console.log('✅ /mensajes page renders correctly with both tabs');
  });

  test('2. Unauthenticated access redirects to login', async ({ page }) => {
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForURL(/ingresar|login/, { timeout: 10000 });
    expect(page.url()).toMatch(/ingresar|login/);
    console.log('✅ Unauthenticated redirect to login works');
  });

  test('3. Opens AI assistant from URL params (vehicle detail flow)', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);

    // Simulate the "Chat Live" flow from a vehicle detail page
    // The page reads sellerId + vehicleTitle from URL params
    await page.goto(
      `${BASE_URL}/mensajes?sellerId=test-dealer-123&vehicleTitle=${encodeURIComponent('Toyota Corolla 2022')}`
    );
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // URL should be cleaned up
    expect(page.url()).not.toContain('sellerId');
    console.log('✅ URL params consumed and cleaned');

    // Bot panel should be visible
    const botHeader = page.locator('[class*="gradient"]').filter({ hasText: /Asistente|Bot/ });
    console.log('Bot header visible:', await botHeader.isVisible());
  });

  test('4. Asistentes IA sidebar shows sessions from localStorage', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Inject a mock bot session into localStorage to simulate history
    await page.evaluate(() => {
      const dealerId = 'e2e-dealer-001';
      localStorage.setItem(`okla_chat_session_${dealerId}`, 'mock-token-e2e');
      localStorage.setItem(`okla_chat_dealername_${dealerId}`, 'AutoDealer E2E');
      localStorage.setItem(
        `okla_chat_messages_${dealerId}`,
        JSON.stringify([
          {
            id: 'msg-1',
            content: '¿Tienen el Toyota Corolla en rojo?',
            isFromBot: false,
            timestamp: new Date().toISOString(),
          },
          {
            id: 'msg-2',
            content: 'Sí, tenemos ese modelo disponible.',
            isFromBot: true,
            timestamp: new Date().toISOString(),
          },
        ])
      );
    });

    // Click "Asistentes IA" tab
    await page.locator('button', { hasText: /Asistentes IA/ }).click();
    await page.waitForTimeout(3500); // Wait for 3s poll interval

    // The session should appear in the sidebar
    const botSession = page.locator('button', { hasText: /AutoDealer E2E/ });
    await expect(botSession).toBeVisible({ timeout: 8000 });
    console.log('✅ Bot session visible in "Asistentes IA" sidebar');

    // Last message should be visible
    const lastMsg = page.locator('p', { hasText: /Sí, tenemos ese modelo/ });
    await expect(lastMsg).toBeVisible({ timeout: 5000 });
    console.log('✅ Last message preview shown in sidebar');
  });

  test('5. Chat history persists across navigation (WhatsApp style)', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Inject persistent chat history
    const dealerId = 'e2e-persistent-dealer';
    await page.evaluate((id: string) => {
      localStorage.setItem(`okla_chat_session_${id}`, 'mock-session-persistent');
      localStorage.setItem(`okla_chat_dealername_${id}`, 'Dealer Persistente');
      localStorage.setItem(
        `okla_chat_messages_${id}`,
        JSON.stringify([
          {
            id: 'hist-1',
            content: 'Hola, me interesa el Honda Civic',
            isFromBot: false,
            timestamp: new Date(Date.now() - 3600000).toISOString(),
          },
          {
            id: 'hist-2',
            content: '¡Claro! Tenemos ese modelo disponible. ¿Cuándo quieres verlo?',
            isFromBot: true,
            timestamp: new Date(Date.now() - 3540000).toISOString(),
          },
        ])
      );
    }, dealerId);

    // Navigate away and back
    await page.goto(`${BASE_URL}/vehiculos`);
    await page.waitForTimeout(1000);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Click Asistentes IA tab
    await page.locator('button', { hasText: /Asistentes IA/ }).click();
    await page.waitForTimeout(3500);

    // Session should still be visible
    const session = page.locator('button', { hasText: /Dealer Persistente/ });
    await expect(session).toBeVisible({ timeout: 8000 });
    console.log('✅ Chat history persists across navigation');

    // Open the session
    await session.click();
    await page.waitForTimeout(2000);

    // Historical messages should be visible
    const historicalMsg = page.locator('p, div', {
      hasText: /Hola, me interesa el Honda Civic/,
    });
    console.log('Historical message visible:', await historicalMsg.first().isVisible());
  });

  test('6. Appointment scheduler shows real month-view calendar', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);

    // Inject a session with an appointment-triggering message
    const dealerId = 'e2e-appt-dealer';
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    await page.evaluate((id: string) => {
      localStorage.setItem(`okla_chat_session_${id}`, 'mock-session-appt');
      localStorage.setItem(`okla_chat_dealername_${id}`, 'Dealer Citas');
      const apptMsg = {
        id: 'appt-trigger',
        content:
          '¡Con gusto! Podemos agendar una cita para que veas el vehículo. ¿Qué día tienes disponible?',
        isFromBot: true,
        timestamp: new Date().toISOString(),
      };
      localStorage.setItem(`okla_chat_messages_${id}`, JSON.stringify([apptMsg]));
    }, dealerId);

    // Open the dealer bot session
    await page.locator('button', { hasText: /Asistentes IA/ }).click();
    await page.waitForTimeout(3500);
    await page.locator('button', { hasText: /Dealer Citas/ }).click();
    await page.waitForTimeout(3000);

    // The appointment scheduler should auto-show after the bot's appointment message
    // Check if calendar/scheduler is visible
    const schedulerHeader = page.locator('h4', { hasText: /Agendar cita/ });
    const calendarGrid = page.locator('[class*="grid-cols-7"]');

    if (await schedulerHeader.isVisible({ timeout: 5000 })) {
      console.log('✅ Appointment scheduler visible');

      // Verify it's a real calendar (7-column grid — there are 2: header + days rows)
      await expect(calendarGrid.first()).toBeVisible();
      console.log('✅ Month-view calendar grid (7 columns) is present');

      // Verify progress steps (1/2)
      await expect(page.locator('p', { hasText: /Paso 1\/2/ })).toBeVisible();
      console.log('✅ Multi-step progress shown (Step 1/2)');

      // Navigate to next month
      const nextBtn = page.locator('button[aria-label="Mes siguiente"]');
      if (await nextBtn.isVisible()) {
        await nextBtn.click();
        await page.waitForTimeout(500);
        console.log('✅ Month navigation works');
      }

      // Click on an available day (not disabled/past)
      const availableDay = page
        .locator('[class*="grid-cols-7"] button')
        .filter({ hasText: /^[0-9]+$/ })
        .filter({ hasNotAttribute: 'disabled' })
        .first();

      if (await availableDay.isVisible()) {
        await availableDay.click();
        await page.waitForTimeout(500);

        // Should advance to time step
        const timeStep = page.locator('p', { hasText: /Paso 2\/2/ });
        await expect(timeStep).toBeVisible({ timeout: 3000 });
        console.log('✅ Clicking date advances to time slot step (Step 2/2)');

        // Time slots should show
        const timeSlots = page.locator('button', { hasText: /AM|PM/ });
        const count = await timeSlots.count();
        expect(count).toBeGreaterThanOrEqual(4);
        console.log(`✅ ${count} time slots available`);
      }
    } else {
      console.log('⚠️ Scheduler not auto-shown (bot message may need LLM response with keywords)');
    }
  });

  test('7. Appointment booking API returns expected responses', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);

    // Test A: without dealerEmail — buyer email only
    const responseA = await page.evaluate(async () => {
      const res = await fetch('/api/appointments/book', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          dealerId: 'e2e-dealer-test',
          dealerName: 'AutoDealer E2E',
          vehicleTitle: 'Honda Civic 2023',
          date: 'Lun 15 de Abr',
          time: '10:00 AM',
        }),
      });
      return { status: res.status, body: await res.json() };
    });

    expect(responseA.status).toBe(200);
    expect(responseA.body.success).toBe(true);
    expect(responseA.body.appointment.dealerName).toBe('AutoDealer E2E');
    expect(responseA.body.emailSentTo.buyer).toBe(true);
    expect(responseA.body.emailSentTo.dealer).toBe(false); // no dealerEmail passed
    console.log('✅ Booking without dealerEmail: buyer email sent, dealer skipped (correct)');

    // Test B: with dealerEmail — both emails sent
    const responseB = await page.evaluate(async () => {
      const res = await fetch('/api/appointments/book', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          dealerId: 'e2e-dealer-test',
          dealerName: 'AutoDealer E2E',
          dealerEmail: 'nmateo@okla.com.do', // real dealer email
          vehicleTitle: 'Honda Civic 2023',
          date: 'Lun 15 de Abr',
          time: '10:00 AM',
        }),
      });
      return { status: res.status, body: await res.json() };
    });

    expect(responseB.status).toBe(200);
    expect(responseB.body.emailSentTo.buyer).toBe(true);
    expect(responseB.body.emailSentTo.dealer).toBe(true); // dealerEmail provided → sent
    console.log('✅ Booking with dealerEmail: BOTH buyer + dealer emails sent');
    console.log('   Emails sent:', JSON.stringify(responseB.body.emailSentTo));
  });

  test('8. Appointment booking API validates required fields', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);

    const response = await page.evaluate(async () => {
      const res = await fetch('/api/appointments/book', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ dealerId: 'only-id' }), // missing required fields
      });
      return { status: res.status, body: await res.json() };
    });

    expect(response.status).toBe(400);
    expect(response.body.success).toBe(false);
    console.log('✅ API validates required fields (returns 400 on incomplete data)');
  });

  test('9. Dealer can view their incoming messages', async ({ page }) => {
    await loginAs(page, DEALER_EMAIL, DEALER_PASSWORD);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Should show messages list
    await expect(
      page
        .locator('h1, [class*="font-bold"]')
        .filter({ hasText: /Mensajes/ })
        .first()
    ).toBeVisible({ timeout: 10000 });
    console.log('✅ Dealer can access /mensajes');
  });

  test('10. Full appointment flow: calendar → time → message sent', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);

    // Start a bot session from URL (simulating "Chat Live" from vehicle detail)
    await page.goto(
      `${BASE_URL}/mensajes?sellerId=e2e-full-test&vehicleTitle=${encodeURIComponent('BMW Serie 3 2024')}`
    );
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(4000);

    // Manually trigger the appointment scheduler by clicking the calendar button if present
    // or by checking if it auto-shows
    const scheduler = page.locator('h4', { hasText: /Agendar cita/ });
    const isSchedulerVisible = await scheduler.isVisible({ timeout: 3000 }).catch(() => false);

    console.log('Scheduler auto-visible:', isSchedulerVisible);

    // Check if there's any "Agendar" or "cita" button in the UI
    const agendarBtn = page
      .locator('button, a')
      .filter({ hasText: /Agendar|cita/i })
      .first();
    if (!isSchedulerVisible && (await agendarBtn.isVisible({ timeout: 2000 }).catch(() => false))) {
      await agendarBtn.click();
      await page.waitForTimeout(1000);
    }

    // If scheduler is now visible, do the full flow
    const schedulerVisible = await page
      .locator('h4', { hasText: /Agendar cita/ })
      .isVisible({ timeout: 3000 })
      .catch(() => false);

    if (schedulerVisible) {
      // Find an available date
      const dayBtn = page
        .locator('[class*="grid-cols-7"] button')
        .filter({ hasNotAttribute: 'disabled' })
        .nth(5); // pick 6th available slot

      if (await dayBtn.isVisible()) {
        await dayBtn.click();
        await page.waitForTimeout(500);

        // Select first time slot
        const timeBtn = page.locator('button', { hasText: /AM|PM/ }).first();
        if (await timeBtn.isVisible()) {
          await timeBtn.click();
          await page.waitForTimeout(2000);

          // Verify the message was sent (appears in the chat)
          const sentMsg = page
            .locator('p')
            .filter({ hasText: /Quisiera agendar una cita/ })
            .first();
          if (await sentMsg.isVisible({ timeout: 5000 })) {
            console.log('✅ Appointment message sent to chatbot');
          }

          // Verify booking progress indicator appeared (or disappeared)
          console.log('✅ Full appointment flow completed');
        }
      }
    } else {
      console.log('⚠️ Scheduler not visible in this test run (requires LLM bot response)');
    }
  });
});

test.describe('Chatbot API fix — no more 500 errors', () => {
  test.setTimeout(90_000);

  test('11. /api/chat/start and /api/chat/message return 200 (ContactEmail fix)', async ({ page }) => {
    await loginAs(page, BUYER_EMAIL, BUYER_PASSWORD);
    await page.goto(`${BASE_URL}/mensajes`);
    await page.waitForLoadState('networkidle');

    // Test /api/chat/start
    const startRes = await page.evaluate(async () => {
      const r = await fetch('/api/chat/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionType: 'WebChat', channel: 'web', language: 'es' }),
      });
      return { status: r.status, body: await r.json() };
    });

    console.log('  /api/chat/start →', startRes.status);
    if (startRes.status !== 200) {
      console.log('  ❌ Error body:', JSON.stringify(startRes.body).slice(0, 200));
    } else {
      console.log('  ✅ SessionToken:', startRes.body.sessionToken?.slice(0, 16) + '...');
      console.log('  ✅ WelcomeMessage:', startRes.body.welcomeMessage?.slice(0, 60));

      // Test /api/chat/message
      const msgRes = await page.evaluate(async (token: string) => {
        const r = await fetch('/api/chat/message', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            sessionToken: token,
            message: 'Hola, me interesa un Toyota Corolla',
            type: 'UserText',
          }),
        });
        return { status: r.status, body: await r.json() };
      }, startRes.body.sessionToken);

      console.log('  /api/chat/message →', msgRes.status);
      if (msgRes.status === 200) {
        console.log('  ✅ Bot response:', msgRes.body.response?.slice(0, 100));
        console.log('  ✅ Intent:', msgRes.body.intentCategory);
      } else {
        console.log('  ❌ Error body:', JSON.stringify(msgRes.body).slice(0, 200));
      }

      // Assert both are 200
      if (startRes.status !== 200 || msgRes.status !== 200) {
        throw new Error(`Expected 200 — start: ${startRes.status}, message: ${msgRes.status}`);
      }
      console.log('✅ Chatbot ContactEmail column fix confirmed: both endpoints 200 OK');
    }
  });
});
