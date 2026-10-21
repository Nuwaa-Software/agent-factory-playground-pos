/**
 * Development sign-in: the API trusts `X-Company-Id` and `X-Role` headers (see the README). Real
 * authentication replaces this; until then the session is just those two values in the browser.
 */
export const ROLES = ['Admin', 'Cashier', 'Viewer'] as const;
export type Role = (typeof ROLES)[number];

export interface Session {
  companyId: string;
  role: Role;
}

const STORAGE_KEY = 'minipos.session';
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const isGuid = (value: string) => GUID.test(value.trim());

export function loadSession(): Session | null {
  try {
    const value = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null') as Partial<Session> | null;
    if (value && typeof value.companyId === 'string' && isGuid(value.companyId) && ROLES.includes(value.role as Role)) {
      return { companyId: value.companyId, role: value.role as Role };
    }
  } catch {
    // unreadable or blocked storage: signed out
  }
  return null;
}

export function saveSession(session: Session) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...session, companyId: session.companyId.trim() }));
}

export function clearSession() {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // nothing stored
  }
}
