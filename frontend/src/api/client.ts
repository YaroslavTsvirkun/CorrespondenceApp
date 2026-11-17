// Простий HTTP-клієнт до бекенд API з JWT-токеном.
// Запити йдуть на той самий origin (/api...) і прокидаються на бекенд
// через проксі Vite (див. vite.config.ts) — і під Aspire, і standalone.
export const API_BASE = '';

export function getToken(): string | null {
  return localStorage.getItem('token');
}

export async function api<T = unknown>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const res = await fetch(API_BASE + path, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {}),
    },
  });

  if (res.status === 401) {
    localStorage.removeItem('token');
    window.dispatchEvent(new Event('auth-expired'));
    throw new Error('Не авторизовано');
  }
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error((body as { title?: string }).title ?? res.statusText);
  }
  return (res.status === 204 ? null : await res.json()) as T;
}
