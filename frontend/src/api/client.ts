// Простий HTTP-клієнт до бекенд API з JWT-токеном.
// Запити йдуть на той самий origin (/api...) і прокидаються на бекенд
// через проксі Vite (див. vite.config.ts) — і під Aspire, і standalone.
export const API_BASE = '';

export function getToken(): string | null {
  return localStorage.getItem('token');
}

export async function api<T = unknown>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await send(path, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
  });
  return (res.status === 204 ? null : await res.json()) as T;
}

/** Завантаження файлу (multipart/form-data). Content-Type з boundary виставляє браузер. */
export async function apiUpload<T = unknown>(path: string, form: FormData): Promise<T> {
  const res = await send(path, { method: 'POST', body: form });
  return (res.status === 204 ? null : await res.json()) as T;
}

/** Отримання файлу з бекенда (з JWT) як Blob — для перегляду в браузері. */
export async function apiBlob(path: string, signal?: AbortSignal): Promise<Blob> {
  const res = await send(path, { signal });
  return res.blob();
}

/** Скачування файлу з бекенда (з JWT) і збереження через браузер. */
export async function apiDownload(path: string, fileName: string): Promise<void> {
  const res = await send(path);
  const url = URL.createObjectURL(await res.blob());
  try {
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
  } finally {
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

async function send(path: string, options: RequestInit = {}): Promise<Response> {
  const token = getToken();
  const res = await fetch(API_BASE + path, {
    ...options,
    headers: {
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
    const { title, errors } = body as { title?: string; errors?: Record<string, string[]> };
    const details = errors ? Object.values(errors).flat().join('; ') : '';
    throw new Error(details || title || res.statusText);
  }
  return res;
}
