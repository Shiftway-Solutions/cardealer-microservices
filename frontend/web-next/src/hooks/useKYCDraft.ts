'use client';

import { useCallback, useEffect, useRef } from 'react';
import { upsertKYCDraft, getKYCDraft, deleteKYCDraft } from '@/services/kyc';

/**
 * Serialized form data for all wizard steps.
 * Stored as JSON in the backend — the backend treats it as opaque.
 */
export interface KYCWizardFormData {
  personalInfo: {
    firstName: string;
    lastName: string;
    dateOfBirth: string;
    nationality: string;
    documentNumber: string;
    gender: string;
    phoneNumber: string;
    occupation: string;
  };
  address: {
    street: string;
    sector: string;
    city: string;
    province: string;
    postalCode: string;
    country: string;
  };
}

/** Step name → step number mapping */
const STEP_MAP: Record<string, number> = {
  info: 1,
  address: 2,
  documents: 3,
  review: 4,
};

interface UseKYCDraftOptions {
  userId: string | undefined;
  currentStep: string;
  formData: KYCWizardFormData;
  /** Called when a draft is loaded to pre-fill the form */
  onDraftLoaded: (data: KYCWizardFormData, step: string) => void;
  /** Autosave interval in ms (default: 30000 = 30s) */
  intervalMs?: number;
  /** Whether to enable autosave (disable when submitting) */
  enabled?: boolean;
}

/**
 * Hook that autosaves KYC wizard progress to the backend.
 * - Saves onStep change
 * - Saves every 30 seconds
 * - Loads existing draft on mount
 * - Deletes draft on successful submission
 */
export function useKYCDraft({
  userId,
  currentStep,
  formData,
  onDraftLoaded,
  intervalMs = 30_000,
  enabled = true,
}: UseKYCDraftOptions) {
  const lastSavedRef = useRef<string>('');
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isMountedRef = useRef(true);
  const draftLoadedRef = useRef(false);

  /** Serialize current form state into the API request format */
  const buildPayload = useCallback(() => {
    const stepNum = STEP_MAP[currentStep] ?? 1;
    const json = JSON.stringify(formData);
    return { currentStep: stepNum, formData: json };
  }, [currentStep, formData]);

  /** Save draft to backend (debounce by content hash) */
  const saveDraft = useCallback(async () => {
    if (!userId || !enabled) return;

    const payload = buildPayload();
    const hash = `${payload.currentStep}::${payload.formData}`;

    // Skip if nothing changed since last save
    if (hash === lastSavedRef.current) return;

    try {
      await upsertKYCDraft(payload);
      lastSavedRef.current = hash;
    } catch (err) {
      // Silent fail — drafts are best-effort
      console.warn('[KYC Draft] Autosave failed:', err);
    }
  }, [userId, enabled, buildPayload]);

  /** Load existing draft on mount */
  useEffect(() => {
    if (!userId || draftLoadedRef.current) return;
    draftLoadedRef.current = true;

    (async () => {
      try {
        const draft = await getKYCDraft(userId);
        if (!draft || !isMountedRef.current) return;

        const parsed: KYCWizardFormData = JSON.parse(draft.formData);
        const stepName =
          Object.entries(STEP_MAP).find(([, v]) => v === draft.currentStep)?.[0] ?? 'info';

        onDraftLoaded(parsed, stepName);
      } catch {
        // No draft or invalid JSON — start fresh
      }
    })();
  }, [userId, onDraftLoaded]);

  /** Save on step change */
  useEffect(() => {
    if (!userId || !enabled) return;
    // Small delay to avoid saving during rapid step transitions
    const t = setTimeout(() => saveDraft(), 500);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentStep]);

  /** Periodic autosave every 30s */
  useEffect(() => {
    if (!userId || !enabled) return;

    timerRef.current = setInterval(() => {
      saveDraft();
    }, intervalMs);

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [userId, enabled, intervalMs, saveDraft]);

  /** Cleanup on unmount */
  useEffect(() => {
    return () => {
      isMountedRef.current = false;
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  /** Call after successful KYC submission to remove the draft */
  const clearDraft = useCallback(async () => {
    if (!userId) return;
    try {
      await deleteKYCDraft(userId);
      lastSavedRef.current = '';
    } catch {
      // Best-effort cleanup
    }
  }, [userId]);

  /** Force-save now (useful before navigation) */
  const saveNow = useCallback(() => saveDraft(), [saveDraft]);

  return { saveDraft: saveNow, clearDraft };
}
