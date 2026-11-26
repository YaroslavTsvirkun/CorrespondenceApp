import { useCallback } from 'react';
import { api } from '../api/client';
import { useApiAction, useApiData } from './useApi';

/**
 * Типовий довідник/реєстр REST-ресурсу: GET {url} — список, POST {url} — створення,
 * PUT {url}/{id} — оновлення, DELETE {url}/{id} — видалення.
 * Після успішної зміни список перезавантажується (і викликається onChanged, напр. оновлення lookups).
 */
export function useCrud<T>(url: string, onChanged?: () => void) {
  const run = useApiAction();
  const load = useCallback(() => api<T[]>(url), [url]);
  const { data: rows, loading, reload } = useApiData<T[]>(load, []);

  const afterChange = (ok: boolean) => {
    if (ok) { reload(); onChanged?.(); }
    return ok;
  };

  /** Створення (id відсутній) або оновлення запису; повертає true при успіху. */
  const save = async (id: number | null | undefined, values: unknown, success = 'Збережено') => {
    const body = JSON.stringify(values);
    return afterChange(await run(
      () => (id ? api(`${url}/${id}`, { method: 'PUT', body }) : api(url, { method: 'POST', body })),
      success));
  };

  const remove = async (id: number, success = 'Видалено') =>
    afterChange(await run(() => api(`${url}/${id}`, { method: 'DELETE' }), success));

  return { rows, loading, reload, save, remove };
}
