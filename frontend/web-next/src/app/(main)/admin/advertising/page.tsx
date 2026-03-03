import { redirect } from 'next/navigation';

/**
 * Admin Advertising Redirect Page
 *
 * Legacy route redirect: /admin/advertising → /admin/publicidad
 * The canonical admin advertising config page is at the publicidad route.
 *
 * Keeping this for backward compatibility with bookmarks and email links.
 */
export default function AdminAdvertisingPage() {
  redirect('/admin/publicidad');
}
