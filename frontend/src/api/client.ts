import { loadSession } from '../session';

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(status: number, code?: string) {
    super(`API request failed with status ${status}`);
    this.status = status;
    this.code = code;
  }
}

/** Extract an error code from a non-OK JSON response, if present. */
async function parseErrorCode(response: Response): Promise<string | undefined> {
  try {
    const json = await response.json();
    return typeof json?.title === 'string' ? json.title : undefined;
  } catch {
    return undefined;
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
  if (!response.ok) throw new ApiError(response.status, await parseErrorCode(response));
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
  if (!response.ok) throw new ApiError(response.status, await parseErrorCode(response));
  return response.text();
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
  if (!response.ok) throw new ApiError(response.status, await parseErrorCode(response));
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
  const response = await fetch(path, { method: 'PATCH', headers, body: body == null ? undefined : JSON.stringify(body) });
  if (!response.ok) throw new ApiError(response.status, await parseErrorCode(response));
  return (await response.json()) as T;
}
