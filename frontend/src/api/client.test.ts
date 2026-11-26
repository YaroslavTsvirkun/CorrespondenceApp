import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';

function respond(status: number, body?: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status, headers: { 'Content-Type': 'application/json' },
    }));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

afterEach(() => vi.unstubAllGlobals());

describe('api', () => {
  it('надсилає заголовок захисту від CSRF і cookie того ж origin', async () => {
    const fetchMock = respond(200, { ok: true });
    await api('/api/tasks', { method: 'POST', body: '{}' });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.credentials).toBe('same-origin');
    expect(init.headers).toMatchObject({ 'X-Requested-With': 'fetch', 'Content-Type': 'application/json' });
  });

  it('повертає null для 204', async () => {
    respond(204);
    expect(await api('/api/x', { method: 'DELETE' })).toBeNull();
  });

  it('збирає повідомлення валідації з усіх полів', async () => {
    respond(400, { title: 'Помилка валідації', errors: { name: ['Вкажіть назву'], code: ['Задовгий код'] } });
    await expect(api('/api/x')).rejects.toThrow('Вкажіть назву; Задовгий код');
  });

  it('на 401 повідомляє про завершення сесії', async () => {
    respond(401, { title: 'Сесію завершено — увійдіть знову' });
    const expired = vi.fn();
    window.addEventListener('auth-expired', expired);

    await expect(api('/api/tasks')).rejects.toThrow('Сесію завершено — увійдіть знову');
    expect(expired).toHaveBeenCalledOnce();
    window.removeEventListener('auth-expired', expired);
  });

  it('невдалий вхід не вважається завершенням сесії', async () => {
    respond(401, { title: 'Невірний логін або пароль' });
    const expired = vi.fn();
    window.addEventListener('auth-expired', expired);

    await expect(api('/api/auth/login', { method: 'POST', body: '{}' })).rejects.toThrow('Невірний логін або пароль');
    expect(expired).not.toHaveBeenCalled();
    window.removeEventListener('auth-expired', expired);
  });
});
