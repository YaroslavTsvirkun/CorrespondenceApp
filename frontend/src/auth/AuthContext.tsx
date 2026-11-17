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
    const token = localStorage.getItem('token');
    if (!token) {
      setLoading(false);
      return;
    }
    api<AuthUser>('/api/auth/me')
      .then(setUser)
      .catch(() => localStorage.removeItem('token'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    const onExpired = () => setUser(null);
    window.addEventListener('auth-expired', onExpired);
    return () => window.removeEventListener('auth-expired', onExpired);
  }, []);

  const login = async (userName: string, password: string) => {
    const res = await api<{ token: string; user: AuthUser }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ userName, password }),
    });
    localStorage.setItem('token', res.token);
    setUser(res.user);
  };

  const logout = () => {
    localStorage.removeItem('token');
    setUser(null);
  };

  const can = (permission: string) => user?.permissions.includes(permission) ?? false;
  const canAny = (...permissions: string[]) => permissions.some((p) => can(p));

  return (
    <Ctx.Provider value={{ user, loading, login, logout, can, canAny }}>
      {children}
    </Ctx.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthCtx {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

// Коди пермішенів (відповідають бекенду)
export const P = {
  docsReadAny: 'documents.read',
  docsCreate: 'documents.create',
  docsUpdate: 'documents.update',
  docsDelete: 'documents.delete',
  resolutionsCreate: 'resolutions.create',
  tasksAssign: 'tasks.assign',
  tasksExecute: 'tasks.execute',
  correspondents: 'correspondents.manage',
  dictionaries: 'dictionaries.manage',
  employees: 'employees.manage',
  departments: 'departments.manage',
  reports: 'reports.view',
  dashOverview: 'dashboard.overview',
  dashControl: 'dashboard.control',
  dashMyTasks: 'dashboard.my-tasks',
  users: 'users.manage',
  organizations: 'organizations.manage',
  settings: 'settings.manage',
  // будь-який скоуп перегляду (для відображення пункту меню «Документи»)
  docsReadScopes: ['documents.read.all', 'documents.read.department', 'documents.read.involved'],
} as const;
