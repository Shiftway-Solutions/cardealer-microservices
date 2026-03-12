/**
 * Auth Hook - Global authentication state management
 *
 * Provides:
 * - Current user state
 * - Authentication status
 * - Login/logout functions
 * - Loading states
 */

'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import {
  authService,
  TwoFactorRequiredError,
  type LoginRequest,
  type RegisterRequest,
} from '@/services/auth';
import type { User } from '@/types';

// =============================================================================
// TYPES
// =============================================================================

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

interface AuthContextValue extends AuthState {
  login: (data: LoginRequest) => Promise<{ user: User }>;
  verifyTwoFactorLogin: (tempToken: string, code: string) => Promise<{ user: User }>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  clearError: () => void;
}

// =============================================================================
// CONTEXT
// =============================================================================

const AuthContext = React.createContext<AuthContextValue | undefined>(undefined);

// =============================================================================
// PROVIDER
// =============================================================================

interface AuthProviderProps {
  children: React.ReactNode;
  initialUser?: User | null;
}

export function AuthProvider({ children, initialUser = null }: AuthProviderProps) {
  const router = useRouter();

  const [state, setState] = React.useState<AuthState>({
    user: initialUser,
    isAuthenticated: !!initialUser,
    isLoading: !initialUser, // Only load if no initial user
    error: null,
  });

  // Load user on mount if not provided
  React.useEffect(() => {
    if (!initialUser) {
      loadUser();
    }
  }, [initialUser]);

  const loadUser = async () => {
    try {
      const user = await authService.getCurrentUser();
      setState({
        user,
        isAuthenticated: !!user,
        isLoading: false,
        error: null,
      });
    } catch {
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  };

  const login = async (data: LoginRequest): Promise<{ user: User }> => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const { user } = await authService.login(data);
      setState({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
      return { user };
    } catch (err) {
      // If 2FA is required, stop loading and re-throw — the login page handles this
      if (err instanceof TwoFactorRequiredError) {
        setState(prev => ({ ...prev, isLoading: false }));
        throw err;
      }
      const error = err as { message?: string };
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error.message || 'Error al iniciar sesión',
      }));
      throw err;
    }
  };

  /**
   * Complete login after 2FA verification.
   * Called from the login page after the user enters their 2FA code.
   */
  const verifyTwoFactorLogin = async (tempToken: string, code: string): Promise<{ user: User }> => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const { user } = await authService.verifyTwoFactorLogin(tempToken, code);
      setState({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
      return { user };
    } catch (err) {
      const error = err as { message?: string };
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error.message || 'Código de verificación inválido',
      }));
      throw err;
    }
  };

  const register = async (data: RegisterRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      await authService.register(data);
      // Do NOT set user/authenticated state — user must verify email first.
      // The registration page handles redirect to /verificar-email.
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: null,
      }));
    } catch (err) {
      const error = err as { message?: string };
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error.message || 'Error al registrarse',
      }));
      throw err;
    }
  };

  const logout = async () => {
    setState(prev => ({ ...prev, isLoading: true }));

    try {
      await authService.logout();
    } finally {
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
      router.push('/');
      router.refresh();
    }
  };

  const refreshUser = async () => {
    await loadUser();
  };

  const clearError = () => {
    setState(prev => ({ ...prev, error: null }));
  };

  const value: AuthContextValue = {
    ...state,
    login,
    verifyTwoFactorLogin,
    register,
    logout,
    refreshUser,
    clearError,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// =============================================================================
// HOOK
// =============================================================================

export function useAuth(): AuthContextValue {
  const context = React.useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}

// =============================================================================
// REQUIRE AUTH HOOK
// =============================================================================

interface RequireAuthOptions {
  redirectTo?: string;
}

export function useRequireAuth(options: RequireAuthOptions = {}) {
  const { redirectTo = '/login' } = options;
  const auth = useAuth();
  const router = useRouter();

  React.useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      const currentPath = window.location.pathname;
      router.push(`${redirectTo}?redirect=${encodeURIComponent(currentPath)}`);
    }
  }, [auth.isLoading, auth.isAuthenticated, redirectTo, router]);

  return auth;
}

export default useAuth;
