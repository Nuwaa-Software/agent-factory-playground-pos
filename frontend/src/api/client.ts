import { loadSession } from '../session';

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`API request failed with status ${status}`);
    this.status = status;
  }
}

/** GET a JSON endpoint of the MiniPos API as the signed-in company and role. */
export async function apiGet<T>(path: string): Promise<T> {
  const session = loadSession();
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (session) {
    headers['X-Company-Id'] = session.companyId;
    headers['X-Role'] = session.role;
  }
  const response = await fetch(path, { headers });
  if (!response.ok) throw new ApiError(response.status);
  return (await response.json()) as T;
}

/** POST a JSON body to the MiniPos API as the signed-in company and role. */
export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const session = loadSession();
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };
  if (session) {
    headers['X-Company-Id'] = session.companyId;
    headers['X-Role'] = session.role;
  }
  const response = await fetch(path, { method: 'POST', headers, body: JSON.stringify(body) });
  if (!response.ok) throw new ApiError(response.status);
  return (await response.json()) as T;
}

/** PATCH a JSON body to an endpoint of the MiniPos API. */
export async function apiPatch<T>(path: string, body: unknown): Promise<T> {
  const session = loadSession();
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };
  if (session) {
    headers['X-Company-Id'] = session.companyId;
    headers['X-Role'] = session.role;
  }
  const response = await fetch(path, { method: 'PATCH', headers, body: JSON.stringify(body) });
  if (!response.ok) throw new ApiError(response.status);
  return (await response.json()) as T;
}
