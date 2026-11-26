import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useApiAction, useApiData } from './useApi';

const message = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock('antd', () => ({ App: { useApp: () => ({ message }) } }));

beforeEach(() => vi.clearAllMocks());

describe('useApiData', () => {
  it('завантажує дані й знімає loading', async () => {
    const load = vi.fn().mockResolvedValue([1, 2]);
    const { result } = renderHook(() => useApiData(load, [] as number[]));

    expect(result.current.loading).toBe(true);
    expect(result.current.data).toEqual([]);
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.data).toEqual([1, 2]);
  });

  it('reload завантажує заново і знову показує loading', async () => {
    const load = vi.fn().mockResolvedValueOnce('a').mockResolvedValueOnce('b');
    const { result } = renderHook(() => useApiData(load, ''));
    await waitFor(() => expect(result.current.data).toBe('a'));

    act(() => result.current.reload());
    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.data).toBe('b'));
    expect(load).toHaveBeenCalledTimes(2);
  });

  it('при помилці показує повідомлення й лишає попередні дані', async () => {
    const load = vi.fn().mockResolvedValueOnce('a').mockRejectedValueOnce(new Error('Сервіс недоступний'));
    const { result } = renderHook(() => useApiData(load, ''));
    await waitFor(() => expect(result.current.data).toBe('a'));

    act(() => result.current.reload());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.data).toBe('a');
    expect(message.error).toHaveBeenCalledWith('Сервіс недоступний');
  });

  it('нова функція завантаження (змінені фільтри) перезавантажує дані', async () => {
    const { result, rerender } = renderHook(({ page }) => useApiData(pageLoader(page), 0), { initialProps: { page: 1 } });
    await waitFor(() => expect(result.current.data).toBe(1));

    rerender({ page: 2 });
    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.data).toBe(2));
  });
});

// Стабільна функція на кожну сторінку — як useCallback із залежністю від фільтра
const loaders = new Map<number, () => Promise<number>>();
function pageLoader(page: number) {
  if (!loaders.has(page)) loaders.set(page, () => Promise.resolve(page));
  return loaders.get(page)!;
}

describe('useApiAction', () => {
  it('показує успіх і повертає true', async () => {
    const { result } = renderHook(() => useApiAction());
    await expect(result.current(() => Promise.resolve(), 'Збережено')).resolves.toBe(true);
    expect(message.success).toHaveBeenCalledWith('Збережено');
  });

  it('показує помилку і повертає false', async () => {
    const { result } = renderHook(() => useApiAction());
    await expect(result.current(() => Promise.reject(new Error('Заборонено')))).resolves.toBe(false);
    expect(message.error).toHaveBeenCalledWith('Заборонено');
    expect(message.success).not.toHaveBeenCalled();
  });
});
