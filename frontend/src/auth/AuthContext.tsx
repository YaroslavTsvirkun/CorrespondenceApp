import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { api } from '../api/client';

export interface AuthUser {
  userId: number;
  userName: string;
  fullName: string;
  employeeId?: number;
  departmentId?: number;
  organizationId?: number | null;
  organizationName?: string | null;
  roles: string[];
  permissions: string[];
}

interface AuthCtx {
  user: AuthUser | null;
  loading: boolean;
  login: (userName: string, password: string) => Promise<void>;
  logout: () => void;
  can: (permission: string) => boolean;
  canAny: (...permissions: string[]) => boolean;
}

const Ctx = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Токен — у HttpOnly cookie, скрипт його не бачить: чи є сесія, показує /api/auth/me.
    localStorage.removeItem('token'); // токен зі старих версій, які тримали його в localStorage
    api<AuthUser>('/api/auth/me')
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    const onExpired = () => setUser(null);
    window.addEventListener('auth-expired', onExpired);
    return () => window.removeEventListener('auth-expired', onExpired);
  }, []);

  const login = async (userName: string, password: string) => {
    // Бекенд ставить HttpOnly cookie з JWT і повертає дані користувача
    const me = await api<AuthUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ userName, password }),
    });
    setUser(me);
  };

  const logout = () => {
    setUser(null);
    api('/api/auth/logout', { method: 'POST' }).catch(() => { /* cookie все одно сплине */ });
  };

  const can = (permission: string) => user?.permissions.includes(permission) ?? false;
  const canAny = (...permissions: string[]) => permissions.some((p) => can(p));

  return (
    <Ctx.Provider value={{ user, loading, login, logout, can, canAny }}>
      {children}
    </Ctx.Provider>
  );
}

// Хук поруч із провайдером — звичний шаблон контексту; fast refresh тут не страждає
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthCtx {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
