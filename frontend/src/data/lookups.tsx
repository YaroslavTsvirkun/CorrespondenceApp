import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
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
  correspondentTypes: Option[];
  taskStatuses: StatusOption[];
  departments: Option[];
  employees: Option[];
  correspondents: Option[];
}

const EMPTY: Lookups = {
  directions: [], documentTypes: [], documentStatuses: [], channels: [],
  correspondentTypes: [], taskStatuses: [], departments: [], employees: [], correspondents: [],
};

const Ctx = createContext<{ lookups: Lookups; reload: () => Promise<void> }>({
  lookups: EMPTY, reload: async () => {},
});

export function LookupsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [lookups, setLookups] = useState<Lookups>(EMPTY);

  const reload = async () => {
    try { setLookups(await api<Lookups>('/api/lookups')); } catch { /* ignore */ }
  };

  useEffect(() => { if (user) reload(); else setLookups(EMPTY); }, [user]); // eslint-disable-line

  return <Ctx.Provider value={{ lookups, reload }}>{children}</Ctx.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useLookups() { return useContext(Ctx); }

// eslint-disable-next-line react-refresh/only-export-components
export const opt = (arr: { id: number; name: string }[]) =>
  arr.map((x) => ({ value: x.id, label: x.name }));
