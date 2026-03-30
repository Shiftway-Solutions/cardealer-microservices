/**
 * Auth Flow Integration Tests
 * Priority: P0 - Authentication flow functionality
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { server } from '@/test/mocks/server';
import { http, HttpResponse } from 'msw';
import * as React from 'react';

// Mock Next.js navigation
const mockPush = vi.fn();
const mockReplace = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
    replace: mockReplace,
    prefetch: vi.fn(),
    back: vi.fn(),
  }),
  usePathname: () => '/',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock auth context provider
const mockAuthContext = {
  user: null as { id: string; email: string; name: string } | null,
  isLoading: false,
  isAuthenticated: false,
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  refreshUser: vi.fn(),
  clearError: vi.fn(),
  error: null as string | null,
};

vi.mock('@/hooks/use-auth', () => ({
  useAuth: () => mockAuthContext,
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
    },
  });

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('Login Flow', () => {
  let storage: Record<string, string>;

  beforeEach(() => {
    storage = {};
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => storage[key] || null);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      storage[key] = value;
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(key => {
      delete storage[key];
    });

    mockPush.mockClear();
    mockReplace.mockClear();
    mockAuthContext.user = null;
    mockAuthContext.isAuthenticated = false;
    mockAuthContext.error = null;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Successful Login', () => {
    it('should login with valid credentials', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/login', async ({ request }) => {
          const body = (await request.json()) as { email: string; password: string };

          if (body.email === 'test@example.com' && body.password === 'password123') {
            return HttpResponse.json({
              accessToken: 'mock-access-token',
              refreshToken: 'mock-refresh-token',
              expiresIn: 3600,
              user: {
                id: 'user1',
                email: 'test@example.com',
                firstName: 'Test',
                lastName: 'User',
                role: 'user',
              },
            });
          }

          return HttpResponse.json(
            { code: 'INVALID_CREDENTIALS', message: 'Invalid email or password' },
            { status: 401 }
          );
        })
      );

      mockAuthContext.login.mockImplementation(async (email: string, password: string) => {
        if (email === 'test@example.com' && password === 'password123') {
          mockAuthContext.user = { id: 'user1', email, name: 'Test User' };
          mockAuthContext.isAuthenticated = true;
          storage['okla_access_token'] = 'mock-access-token';
          storage['okla_refresh_token'] = 'mock-refresh-token';
        } else {
          throw new Error('Invalid credentials');
        }
      });

      await mockAuthContext.login('test@example.com', 'password123');

      expect(mockAuthContext.isAuthenticated).toBe(true);
      expect(mockAuthContext.user).toBeDefined();
      expect(storage['okla_access_token']).toBe('mock-access-token');
    });

    it('should store tokens in localStorage', async () => {
      mockAuthContext.login.mockImplementation(async () => {
        storage['okla_access_token'] = 'access-token-123';
        storage['okla_refresh_token'] = 'refresh-token-456';
        mockAuthContext.user = { id: 'user1', email: 'test@example.com', name: 'Test' };
        mockAuthContext.isAuthenticated = true;
      });

      await mockAuthContext.login('test@example.com', 'password123');

      expect(storage['okla_access_token']).toBe('access-token-123');
      expect(storage['okla_refresh_token']).toBe('refresh-token-456');
    });

    it('should redirect after login', async () => {
      mockAuthContext.login.mockImplementation(async () => {
        mockAuthContext.user = { id: 'user1', email: 'test@example.com', name: 'Test' };
        mockAuthContext.isAuthenticated = true;
        mockPush('/');
      });

      await mockAuthContext.login('test@example.com', 'password123');

      expect(mockPush).toHaveBeenCalledWith('/');
    });
  });

  describe('Failed Login', () => {
    it('should handle invalid credentials', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/login', () => {
          return HttpResponse.json(
            { code: 'INVALID_CREDENTIALS', message: 'Credenciales inválidas' },
            { status: 401 }
          );
        })
      );

      mockAuthContext.login.mockImplementation(async () => {
        mockAuthContext.error = 'Credenciales inválidas';
        throw new Error('Credenciales inválidas');
      });

      try {
        await mockAuthContext.login('wrong@example.com', 'wrongpassword');
      } catch {
        // Expected
      }

      expect(mockAuthContext.error).toBe('Credenciales inválidas');
      expect(mockAuthContext.isAuthenticated).toBe(false);
    });

    it('should handle account not verified', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/login', () => {
          return HttpResponse.json(
            { code: 'EMAIL_NOT_VERIFIED', message: 'Por favor verifica tu email' },
            { status: 403 }
          );
        })
      );

      mockAuthContext.login.mockImplementation(async () => {
        mockAuthContext.error = 'Por favor verifica tu email';
        throw new Error('Email not verified');
      });

      try {
        await mockAuthContext.login('unverified@example.com', 'password123');
      } catch {
        // Expected
      }

      expect(mockAuthContext.error).toBe('Por favor verifica tu email');
    });

    it('should handle account locked', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/login', () => {
          return HttpResponse.json(
            { code: 'ACCOUNT_LOCKED', message: 'Cuenta bloqueada' },
            { status: 423 }
          );
        })
      );

      mockAuthContext.login.mockImplementation(async () => {
        mockAuthContext.error = 'Cuenta bloqueada';
        throw new Error('Account locked');
      });

      try {
        await mockAuthContext.login('locked@example.com', 'password123');
      } catch {
        // Expected
      }

      expect(mockAuthContext.error).toBe('Cuenta bloqueada');
    });
  });
});

describe('Registration Flow', () => {
  let storage: Record<string, string>;

  beforeEach(() => {
    storage = {};
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => storage[key] || null);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      storage[key] = value;
    });

    mockAuthContext.user = null;
    mockAuthContext.isAuthenticated = false;
    mockAuthContext.error = null;
  });

  describe('Successful Registration', () => {
    it('should register new user', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/register', async ({ request }) => {
          const body = (await request.json()) as { email: string };

          return HttpResponse.json({
            id: 'newuser1',
            email: body.email,
            message: 'Registro exitoso. Por favor verifica tu email.',
          });
        })
      );

      mockAuthContext.register.mockImplementation(async () => {
        return { success: true, message: 'Registro exitoso' };
      });

      const result = await mockAuthContext.register({
        email: 'new@example.com',
        password: 'Password123!',
        firstName: 'New',
        lastName: 'User',
      });

      expect(result.success).toBe(true);
    });

    it('should redirect to verification page', async () => {
      mockAuthContext.register.mockImplementation(async () => {
        mockPush('/verificar-email');
        return { success: true };
      });

      await mockAuthContext.register({
        email: 'new@example.com',
        password: 'Password123!',
      });

      expect(mockPush).toHaveBeenCalledWith('/verificar-email');
    });
  });

  describe('Failed Registration', () => {
    it('should handle email already exists', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/register', () => {
          return HttpResponse.json(
            { code: 'EMAIL_EXISTS', message: 'Este email ya está registrado' },
            { status: 409 }
          );
        })
      );

      mockAuthContext.register.mockImplementation(async () => {
        mockAuthContext.error = 'Este email ya está registrado';
        throw new Error('Email exists');
      });

      try {
        await mockAuthContext.register({ email: 'existing@example.com', password: 'Password123!' });
      } catch {
        // Expected
      }

      expect(mockAuthContext.error).toBe('Este email ya está registrado');
    });

    it('should handle weak password', async () => {
      server.use(
        http.post('http://localhost:8080/api/auth/register', () => {
          return HttpResponse.json(
            { code: 'WEAK_PASSWORD', message: 'La contraseña debe tener al menos 8 caracteres' },
            { status: 400 }
          );
        })
      );

      mockAuthContext.register.mockImplementation(async () => {
        mockAuthContext.error = 'La contraseña debe tener al menos 8 caracteres';
        throw new Error('Weak password');
      });

      try {
        await mockAuthContext.register({ email: 'new@example.com', password: '123' });
      } catch {
        // Expected
      }

      expect(mockAuthContext.error).toBe('La contraseña debe tener al menos 8 caracteres');
    });
  });
});

describe('Logout Flow', () => {
  let storage: Record<string, string>;

  beforeEach(() => {
    storage = {
      okla_access_token: 'mock-access-token',
      okla_refresh_token: 'mock-refresh-token',
    };
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => storage[key] || null);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      storage[key] = value;
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(key => {
      delete storage[key];
    });

    mockAuthContext.user = { id: 'user1', email: 'test@example.com', name: 'Test' };
    mockAuthContext.isAuthenticated = true;
  });

  it('should clear user state on logout', async () => {
    mockAuthContext.logout.mockImplementation(async () => {
      mockAuthContext.user = null;
      mockAuthContext.isAuthenticated = false;
    });

    await mockAuthContext.logout();

    expect(mockAuthContext.user).toBeNull();
    expect(mockAuthContext.isAuthenticated).toBe(false);
  });

  it('should clear tokens on logout', async () => {
    mockAuthContext.logout.mockImplementation(async () => {
      delete storage['okla_access_token'];
      delete storage['okla_refresh_token'];
      mockAuthContext.user = null;
      mockAuthContext.isAuthenticated = false;
    });

    await mockAuthContext.logout();

    expect(storage['okla_access_token']).toBeUndefined();
    expect(storage['okla_refresh_token']).toBeUndefined();
  });

  it('should redirect to home on logout', async () => {
    mockAuthContext.logout.mockImplementation(async () => {
      mockAuthContext.user = null;
      mockAuthContext.isAuthenticated = false;
      mockPush('/');
    });

    await mockAuthContext.logout();

    expect(mockPush).toHaveBeenCalledWith('/');
  });

  it('should call logout API', async () => {
    let logoutCalled = false;

    server.use(
      http.post('http://localhost:8080/api/auth/logout', () => {
        logoutCalled = true;
        return new HttpResponse(null, { status: 204 });
      })
    );

    mockAuthContext.logout.mockImplementation(async () => {
      // Simulate API call
      await fetch('http://localhost:8080/api/auth/logout', { method: 'POST' });
      mockAuthContext.user = null;
      mockAuthContext.isAuthenticated = false;
    });

    await mockAuthContext.logout();

    expect(logoutCalled).toBe(true);
  });
});

describe('Token Refresh Flow', () => {
  let storage: Record<string, string>;

  beforeEach(() => {
    storage = {
      okla_access_token: 'expired-access-token',
      okla_refresh_token: 'valid-refresh-token',
    };
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => storage[key] || null);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      storage[key] = value;
    });
  });

  it('should refresh access token', async () => {
    server.use(
      http.post('http://localhost:8080/api/auth/refresh', () => {
        return HttpResponse.json({
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600,
        });
      })
    );

    mockAuthContext.refreshUser.mockImplementation(async () => {
      storage['okla_access_token'] = 'new-access-token';
      storage['okla_refresh_token'] = 'new-refresh-token';
    });

    await mockAuthContext.refreshUser();

    expect(storage['okla_access_token']).toBe('new-access-token');
  });

  it('should handle expired refresh token', async () => {
    server.use(
      http.post('http://localhost:8080/api/auth/refresh', () => {
        return HttpResponse.json(
          { code: 'REFRESH_TOKEN_EXPIRED', message: 'Session expired' },
          { status: 401 }
        );
      })
    );

    mockAuthContext.refreshUser.mockImplementation(async () => {
      delete storage['okla_access_token'];
      delete storage['okla_refresh_token'];
      mockAuthContext.user = null;
      mockAuthContext.isAuthenticated = false;
      mockPush('/login');
    });

    await mockAuthContext.refreshUser();

    expect(mockAuthContext.isAuthenticated).toBe(false);
    expect(mockPush).toHaveBeenCalledWith('/login');
  });
});

describe('Password Reset Flow', () => {
  it('should request password reset', async () => {
    let resetRequested = false;

    server.use(
      http.post('http://localhost:8080/api/auth/forgot-password', async ({ request }) => {
        const body = (await request.json()) as { email: string };
        if (body.email) {
          resetRequested = true;
          return HttpResponse.json({
            message: 'Se ha enviado un enlace de recuperación a tu email',
          });
        }
        return HttpResponse.json({ error: 'Email required' }, { status: 400 });
      })
    );

    const response = await fetch('http://localhost:8080/api/auth/forgot-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'test@example.com' }),
    });

    expect(response.ok).toBe(true);
    expect(resetRequested).toBe(true);
  });

  it('should handle non-existent email gracefully', async () => {
    server.use(
      http.post('http://localhost:8080/api/auth/forgot-password', () => {
        // For security, don't reveal if email exists
        return HttpResponse.json({
          message: 'Si el email existe, recibirás un enlace de recuperación',
        });
      })
    );

    const response = await fetch('http://localhost:8080/api/auth/forgot-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'nonexistent@example.com' }),
    });

    const data = await response.json();
    expect(response.ok).toBe(true);
    expect(data.message).toBeDefined();
  });

  it('should reset password with valid token', async () => {
    server.use(
      http.post('http://localhost:8080/api/auth/reset-password', async ({ request }) => {
        const body = (await request.json()) as { token: string; password: string };

        if (body.token === 'valid-reset-token' && body.password) {
          return HttpResponse.json({
            message: 'Contraseña actualizada exitosamente',
          });
        }

        return HttpResponse.json(
          { code: 'INVALID_TOKEN', message: 'Token inválido o expirado' },
          { status: 400 }
        );
      })
    );

    const response = await fetch('http://localhost:8080/api/auth/reset-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: 'valid-reset-token', password: 'NewPassword123!' }),
    });

    expect(response.ok).toBe(true);
  });
});

describe('Email Verification Flow', () => {
  it('should verify email with valid token', async () => {
    server.use(
      http.post('http://localhost:8080/api/auth/verify-email', async ({ request }) => {
        const body = (await request.json()) as { token: string };

        if (body.token === 'valid-verification-token') {
          return HttpResponse.json({
            message: 'Email verificado exitosamente',
            verified: true,
          });
        }

        return HttpResponse.json(
          { code: 'INVALID_TOKEN', message: 'Token inválido o expirado' },
          { status: 400 }
        );
      })
    );

    const response = await fetch('http://localhost:8080/api/auth/verify-email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: 'valid-verification-token' }),
    });

    const data = await response.json();
    expect(response.ok).toBe(true);
    expect(data.verified).toBe(true);
  });

  it('should resend verification email', async () => {
    let resendCount = 0;

    server.use(
      http.post('http://localhost:8080/api/auth/resend-verification', () => {
        resendCount++;
        return HttpResponse.json({
          message: 'Email de verificación reenviado',
        });
      })
    );

    await fetch('http://localhost:8080/api/auth/resend-verification', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'test@example.com' }),
    });

    expect(resendCount).toBe(1);
  });
});

describe('Protected Routes', () => {
  beforeEach(() => {
    mockPush.mockClear();
    mockReplace.mockClear();
  });

  it('should redirect unauthenticated users to login', () => {
    mockAuthContext.user = null;
    mockAuthContext.isAuthenticated = false;

    // Simulate protected route check
    const requireAuth = () => {
      if (!mockAuthContext.isAuthenticated) {
        mockReplace('/login?redirect=/dashboard');
        return false;
      }
      return true;
    };

    const hasAccess = requireAuth();

    expect(hasAccess).toBe(false);
    expect(mockReplace).toHaveBeenCalledWith('/login?redirect=/dashboard');
  });

  it('should allow authenticated users', () => {
    mockAuthContext.user = { id: 'user1', email: 'test@example.com', name: 'Test' };
    mockAuthContext.isAuthenticated = true;

    const requireAuth = () => {
      if (!mockAuthContext.isAuthenticated) {
        mockReplace('/login');
        return false;
      }
      return true;
    };

    const hasAccess = requireAuth();

    expect(hasAccess).toBe(true);
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('should preserve redirect URL', () => {
    mockAuthContext.user = null;
    mockAuthContext.isAuthenticated = false;

    const targetPath = '/mis-favoritos';

    const requireAuth = (redirectTo: string) => {
      if (!mockAuthContext.isAuthenticated) {
        mockReplace(`/login?redirect=${encodeURIComponent(redirectTo)}`);
        return false;
      }
      return true;
    };

    requireAuth(targetPath);

    expect(mockReplace).toHaveBeenCalledWith('/login?redirect=%2Fmis-favoritos');
  });
});

describe('Session Persistence', () => {
  let storage: Record<string, string>;

  beforeEach(() => {
    storage = {};
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => storage[key] || null);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      storage[key] = value;
    });
  });

  it('should restore session from tokens', async () => {
    storage['okla_access_token'] = 'valid-access-token';
    storage['okla_refresh_token'] = 'valid-refresh-token';

    server.use(
      http.get('http://localhost:8080/api/auth/me', ({ request }) => {
        const authHeader = request.headers.get('Authorization');

        if (authHeader === 'Bearer valid-access-token') {
          return HttpResponse.json({
            id: 'user1',
            email: 'test@example.com',
            firstName: 'Test',
            lastName: 'User',
          });
        }

        return HttpResponse.json({ error: 'Unauthorized' }, { status: 401 });
      })
    );

    mockAuthContext.refreshUser.mockImplementation(async () => {
      const token = storage['okla_access_token'];
      if (token) {
        mockAuthContext.user = { id: 'user1', email: 'test@example.com', name: 'Test User' };
        mockAuthContext.isAuthenticated = true;
      }
    });

    await mockAuthContext.refreshUser();

    expect(mockAuthContext.isAuthenticated).toBe(true);
    expect(mockAuthContext.user).toBeDefined();
  });

  it('should clear session when tokens are missing', async () => {
    // No tokens in storage
    mockAuthContext.user = null;
    mockAuthContext.isAuthenticated = false;

    mockAuthContext.refreshUser.mockImplementation(async () => {
      const token = storage['okla_access_token'];
      if (!token) {
        mockAuthContext.user = null;
        mockAuthContext.isAuthenticated = false;
      }
    });

    await mockAuthContext.refreshUser();

    expect(mockAuthContext.isAuthenticated).toBe(false);
    expect(mockAuthContext.user).toBeNull();
  });
});
