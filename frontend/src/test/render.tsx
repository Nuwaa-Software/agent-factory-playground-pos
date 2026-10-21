import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { saveSession, type Role } from '../session';

export const COMPANY_ID = '3f2504e0-4f89-11d3-9a0c-0305e82c3301';

/** Render with everything a page needs: a fresh query cache (no retries) and a router at `route`. */
export function renderWithProviders(ui: ReactElement, { route = '/' }: { route?: string } = {}) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

export function signIn(role: Role = 'Cashier') {
  saveSession({ companyId: COMPANY_ID, role });
}

/** Replace `fetch` with one that answers every request with `body` (JSON) and `status`. */
export function stubFetch(body: unknown, status = 200) {
  const fetchMock = vi.fn(async () => new Response(JSON.stringify(body), { status }));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}
