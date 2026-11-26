import { useCallback, useEffect, useState } from 'react';
import { App } from 'antd';

/** Текст помилки для повідомлення користувачу. */
export function errorMessage(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}

/**
 * Дія з викликом API: помилка показується повідомленням, успіх — за потреби теж.
 * Повертає true, якщо дія завершилась успішно.
 *
 *   const run = useApiAction();
 *   if (await run(() => api(`/api/x/${id}`, { method: 'DELETE' }), 'Видалено')) reload();
 */
export function useApiAction() {
  const { message } = App.useApp();
  return useCallback(async (action: () => Promise<unknown>, success?: string): Promise<boolean> => {
    try {
      await action();
      if (success) message.success(success);
      return true;
    } catch (e) {
      message.error(errorMessage(e));
      return false;
    }
  }, [message]);
}

/**
 * Дані з API: завантажуються при монтуванні й щоразу, коли змінюється `load`
 * (обгорніть його в useCallback із залежностями — напр. фільтрами), а також за `reload()`.
 * `loading` — true, поки результат для поточного `load` ще не отримано.
 * Помилка показується повідомленням; попередні дані при цьому лишаються.
 */
export function useApiData<T>(load: () => Promise<T>, initial: T) {
  const { message } = App.useApp();
  const [data, setData] = useState<T>(initial);
  const [version, setVersion] = useState(0);
  const [settled, setSettled] = useState<{ load: () => Promise<T>; version: number } | null>(null);

  useEffect(() => {
    let active = true;
    load()
      .then((result) => { if (active) setData(result); })
      .catch((e) => { if (active) message.error(errorMessage(e)); })
      .finally(() => { if (active) setSettled({ load, version }); });
    return () => { active = false; };
  }, [load, version, message]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const loading = settled?.load !== load || settled.version !== version;
  return { data, setData, loading, reload };
}
