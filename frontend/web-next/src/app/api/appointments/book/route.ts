/**
 * Appointment Booking API Route — BFF
 *
 * Books a dealer appointment and sends confirmation emails to:
 *  - The buyer (current authenticated user)
 *  - The dealer/seller (if email is provided)
 *
 * Calls NotificationService internal endpoint directly (pod-to-pod in K8s).
 * No JWT required for the notification call — it is protected by the internal K8s network.
 */

import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';

// Internal URL for the NotificationService (pod-to-pod in K8s or via gateway in dev)
const NOTIFICATION_URL =
  process.env.INTERNAL_NOTIFICATION_URL ||
  process.env.INTERNAL_API_URL ||
  'http://notificationservice:8080';

const AUTH_COOKIE_NAME = 'okla_access_token';

interface BookAppointmentRequest {
  dealerId: string;
  dealerName: string;
  dealerEmail?: string;
  vehicleTitle: string;
  date: string; // e.g. "Lun 10 de Mar"
  time: string; // e.g. "10:00 AM"
  buyerName?: string;
  buyerEmail?: string;
}

async function sendEmailViaNotificationService(
  to: string,
  subject: string,
  body: string,
  authToken?: string
): Promise<void> {
  try {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (authToken) {
      headers['Authorization'] = `Bearer ${authToken}`;
    }

    const notifRes = await fetch(`${NOTIFICATION_URL}/api/internal/notifications/email`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ to, subject, body, isHtml: true }),
    });

    if (!notifRes.ok) {
      console.error(
        `[Appointments] Failed to send email to ${to}: ${notifRes.status} ${notifRes.statusText}`
      );
    }
  } catch (err) {
    console.error(`[Appointments] Error sending email to ${to}:`, err);
  }
}

export async function POST(request: NextRequest) {
  try {
    const body: BookAppointmentRequest = await request.json();
    const { dealerId, dealerName, dealerEmail, vehicleTitle, date, time } = body;

    if (!dealerId || !dealerName || !vehicleTitle || !date || !time) {
      return NextResponse.json({ success: false, error: 'Datos incompletos' }, { status: 400 });
    }

    // ── Obtener datos del comprador desde la sesión ──────────────────────────
    const cookieStore = await cookies();
    const authToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    let buyerName = body.buyerName || 'Cliente';
    let buyerEmail = body.buyerEmail || '';

    if (authToken) {
      try {
        const authUrl = process.env.INTERNAL_API_URL || 'http://gateway:8080';
        const profileRes = await fetch(`${authUrl}/api/auth/me`, {
          headers: {
            Authorization: `Bearer ${authToken}`,
            Cookie: `${AUTH_COOKIE_NAME}=${authToken}`,
          },
        });
        if (profileRes.ok) {
          const profileJson = await profileRes.json();
          const user = profileJson?.data || profileJson;
          if (user?.email) buyerEmail = user.email;
          if (user?.firstName || user?.lastName) {
            buyerName = [user.firstName, user.lastName].filter(Boolean).join(' ') || buyerName;
          } else if (user?.fullName) {
            buyerName = user.fullName;
          }
        }
      } catch (err) {
        console.warn('[Appointments] Could not fetch buyer profile:', err);
      }
    }

    // ── Email al comprador ───────────────────────────────────────────────────
    if (buyerEmail) {
      const buyerSubject = `✅ Cita confirmada con ${dealerName} — OKLA`;
      const buyerBody = `
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background:#f9fafb; margin:0; padding:20px;">
  <div style="max-width:600px; margin:0 auto; background:#fff; border-radius:16px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.08);">
    <div style="background:linear-gradient(135deg,#00A870,#059669); padding:32px 24px; text-align:center;">
      <div style="font-size:48px; margin-bottom:8px;">📅</div>
      <h1 style="color:#fff; margin:0; font-size:24px; font-weight:700;">¡Cita Confirmada!</h1>
      <p style="color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;">Tu cita ha sido agendada exitosamente</p>
    </div>
    <div style="padding:32px 24px;">
      <p style="color:#374151; font-size:16px; margin:0 0 24px;">Hola <strong>${buyerName}</strong>,</p>
      <p style="color:#6b7280; font-size:15px; line-height:1.6; margin:0 0 24px;">
        Tu cita con <strong style="color:#111827;">${dealerName}</strong> ha sido agendada para el vehículo:
      </p>

      <div style="background:#f0fdf4; border:1px solid #bbf7d0; border-radius:12px; padding:20px 24px; margin-bottom:24px;">
        <table style="width:100%; border-collapse:collapse;">
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px; width:140px;">🚗 Vehículo</td>
            <td style="padding:8px 0; color:#111827; font-size:14px; font-weight:600;">${vehicleTitle}</td>
          </tr>
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">🏢 Dealer</td>
            <td style="padding:8px 0; color:#111827; font-size:14px; font-weight:600;">${dealerName}</td>
          </tr>
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">📅 Fecha</td>
            <td style="padding:8px 0; color:#00A870; font-size:14px; font-weight:700;">${date}</td>
          </tr>
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">🕐 Hora</td>
            <td style="padding:8px 0; color:#00A870; font-size:14px; font-weight:700;">${time}</td>
          </tr>
        </table>
      </div>

      <p style="color:#6b7280; font-size:14px; line-height:1.6; margin:0 0 16px;">
        💡 <strong>Consejo:</strong> Llega 5 minutos antes de la hora acordada. Si necesitas cancelar o reagendar, contáctanos a través del chat en OKLA.
      </p>
    </div>
    <div style="background:#f9fafb; padding:24px; text-align:center; border-top:1px solid #f3f4f6;">
      <p style="color:#9ca3af; font-size:13px; margin:0;">
        © ${new Date().getFullYear()} OKLA — El marketplace de vehículos de República Dominicana
      </p>
      <p style="color:#9ca3af; font-size:12px; margin:8px 0 0;">
        <a href="https://okla.com.do/mensajes" style="color:#00A870; text-decoration:none;">Ver mis mensajes</a>
      </p>
    </div>
  </div>
</body>
</html>`;

      await sendEmailViaNotificationService(buyerEmail, buyerSubject, buyerBody, authToken);
    }

    // ── Email al dealer/vendedor ─────────────────────────────────────────────
    if (dealerEmail) {
      const dealerSubject = `📅 Nueva cita agendada — ${buyerName} quiere ver ${vehicleTitle}`;
      const dealerBody = `
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background:#f9fafb; margin:0; padding:20px;">
  <div style="max-width:600px; margin:0 auto; background:#fff; border-radius:16px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.08);">
    <div style="background:linear-gradient(135deg,#1d4ed8,#2563eb); padding:32px 24px; text-align:center;">
      <div style="font-size:48px; margin-bottom:8px;">📅</div>
      <h1 style="color:#fff; margin:0; font-size:24px; font-weight:700;">Nueva Cita Agendada</h1>
      <p style="color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;">Un cliente quiere visitar tu negocio</p>
    </div>
    <div style="padding:32px 24px;">
      <p style="color:#374151; font-size:16px; margin:0 0 24px;">Hola <strong>${dealerName}</strong>,</p>
      <p style="color:#6b7280; font-size:15px; line-height:1.6; margin:0 0 24px;">
        <strong style="color:#111827;">${buyerName}</strong> ha agendado una cita para ver el siguiente vehículo:
      </p>

      <div style="background:#eff6ff; border:1px solid #bfdbfe; border-radius:12px; padding:20px 24px; margin-bottom:24px;">
        <table style="width:100%; border-collapse:collapse;">
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px; width:140px;">👤 Cliente</td>
            <td style="padding:8px 0; color:#111827; font-size:14px; font-weight:600;">${buyerName}</td>
          </tr>
          ${buyerEmail ? `<tr><td style="padding:8px 0; color:#6b7280; font-size:14px;">📧 Email</td><td style="padding:8px 0; color:#111827; font-size:14px;">${buyerEmail}</td></tr>` : ''}
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">🚗 Vehículo</td>
            <td style="padding:8px 0; color:#111827; font-size:14px; font-weight:600;">${vehicleTitle}</td>
          </tr>
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">📅 Fecha</td>
            <td style="padding:8px 0; color:#2563eb; font-size:14px; font-weight:700;">${date}</td>
          </tr>
          <tr>
            <td style="padding:8px 0; color:#6b7280; font-size:14px;">🕐 Hora</td>
            <td style="padding:8px 0; color:#2563eb; font-size:14px; font-weight:700;">${time}</td>
          </tr>
        </table>
      </div>

      <p style="color:#6b7280; font-size:14px; line-height:1.6; margin:0 0 16px;">
        💡 Asegúrate de tener el vehículo disponible para la cita. Puedes comunicarte con el cliente a través del chat en OKLA.
      </p>
    </div>
    <div style="background:#f9fafb; padding:24px; text-align:center; border-top:1px solid #f3f4f6;">
      <p style="color:#9ca3af; font-size:13px; margin:0;">
        © ${new Date().getFullYear()} OKLA — El marketplace de vehículos de República Dominicana
      </p>
      <p style="color:#9ca3af; font-size:12px; margin:8px 0 0;">
        <a href="https://okla.com.do/mensajes" style="color:#2563eb; text-decoration:none;">Ver mis mensajes</a>
      </p>
    </div>
  </div>
</body>
</html>`;

      await sendEmailViaNotificationService(dealerEmail, dealerSubject, dealerBody, authToken);
    }

    return NextResponse.json({
      success: true,
      appointment: { dealerId, dealerName, vehicleTitle, date, time },
      emailSentTo: {
        buyer: !!buyerEmail,
        dealer: !!dealerEmail,
      },
    });
  } catch (err) {
    console.error('[Appointments] Error booking appointment:', err);
    return NextResponse.json(
      { success: false, error: 'Error al agendar la cita' },
      { status: 500 }
    );
  }
}
