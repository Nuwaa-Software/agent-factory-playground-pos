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

/** GET a text/plain endpoint of the MiniPos API as the signed-in company and role. */
export async function apiGetText(path: string): Promise<string> {
  const session = loadSession();
  const headers: Record<string, string> = { Accept: 'text/plain' };
  if (session) {
    headers['X-Company-Id'] = session.companyId;
    headers['X-Role'] = session.role;
  }
  const response = await fetch(path, { headers });
  if (!response.ok) throw new ApiError(response.status);
  return response.text();
}
