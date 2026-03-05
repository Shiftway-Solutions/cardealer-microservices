/**
 * Hook: useStageConfig
 *
 * Provides access to the current OKLA platform stage configuration.
 * Features are controlled by the NEXT_PUBLIC_OKLA_STAGE environment variable
 * and can be overridden via ConfigurationService feature flags.
 */

'use client';

import { useMemo } from 'react';
import {
  getCurrentStage,
  getCurrentScorePhase,
  getStageFeatures,
  STAGE_META,
  type OklaStage,
  type StageFeatures,
} from '@/lib/stage-config';

export function useStageConfig() {
  const stage = getCurrentStage();
  const scorePhase = getCurrentScorePhase();

  const features = useMemo(() => getStageFeatures(stage), [stage]);
  const meta = STAGE_META[stage];

  return {
    /** Current platform stage (1-4) */
    stage,
    /** Current OKLA Score phase (1-4) */
    scorePhase,
    /** All feature flags for current stage */
    features,
    /** Stage display metadata */
    meta,
    /** Check if a feature is enabled */
    isEnabled: (category: keyof StageFeatures, feature: string): boolean => {
      const cat = features[category] as Record<string, unknown>;
      return Boolean(cat[feature]);
    },
  };
}

export type { OklaStage, StageFeatures };
