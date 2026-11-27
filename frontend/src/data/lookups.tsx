import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export interface Option { id: number; name: string; }
export interface DirectionOption { id: number; code: string; name: string; }
export interface StatusOption { id: number; name: string; isFinal: boolean; }

export interface Lookups {
  directions: DirectionOption[];
  documentTypes: Option[];
  documentStatuses: StatusOption[];
  channels: Option[];
  taskStatuses: StatusOption[];
  departments: Option[];
  employees: Option[];
  correspondents: Option[];
}

const EMPTY: Lookups = {
  directions: [], documentTypes: [], documentStatuses: [], channels: [],
  taskStatuses: [], departments: [], employees: [], correspondents: [],
};

/** ready — довідники поточного користувача отримано (або спроба завершилась помилкою). */
const Ctx = createContext<{ lookups: Lookups; ready: boolean; reload: () => void }>({
  lookups: EMPTY, ready: false, reload: () => {},
});

export function LookupsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  // Довідники належать конкретному користувачу (організація, права): після виходу чи зміни
  // користувача попередні не показуються
  const [loaded, setLoaded] = useState<{ userId: number; lookups: Lookups } | null>(null);
  const [version, setVersion] = useState(0);
  const userId = user?.userId;

  useEffect(() => {
    if (userId == null) return;
    let active = true;
    api<Lookups>('/api/lookups')
      .then((lookups) => { if (active) setLoaded({ userId, lookups }); })
      // Без довідників форми лишаються порожніми, але сторінки не чекають їх вічно
      .catch(() => { if (active) setLoaded({ userId, lookups: EMPTY }); });
    return () => { active = false; };
  }, [userId, version]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const ready = userId != null && loaded?.userId === userId;
  const lookups = ready ? loaded.lookups : EMPTY;

  return <Ctx.Provider value={{ lookups, ready, reload }}>{children}</Ctx.Provider>;
}

// Хук поруч із провайдером — звичний шаблон контексту
// eslint-disable-next-line react-refresh/only-export-components
export function useLookups() { return useContext(Ctx); }
