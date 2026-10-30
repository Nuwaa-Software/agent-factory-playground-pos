import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import type { User } from '../api/users';
import { COMPANY_ID, renderWithProviders, signIn, stubFetch } from '../test/render';
import UsersPage from './UsersPage';

const USERS: User[] = [
  { id: 'u1', nameEn: 'Alice', nameAr: 'أليس', role: 0, isActive: true, createdBy: 'admin', createdAt: '2025-01-01T00:00:00Z', modifiedBy: null, modifiedAt: null },
  { id: 'u2', nameEn: 'Bob', nameAr: 'بوب', role: 1, isActive: false, createdBy: 'admin', createdAt: '2025-01-02T00:00:00Z', modifiedBy: null, modifiedAt: null },
];

describe('UsersPage', () => {
  beforeEach(() => signIn('Admin'));

  // ── Loading state ──────────────────────────────────────────────

  it('shows loading in Arabic', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async () => new Promise(() => {})));
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  it('shows loading in English', async () => {
    await i18n.changeLanguage('en');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async () => new Promise(() => {})));
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(screen.getByRole('status')).toHaveTextContent('Loading…');
  });

  // ── Lists users in Arabic ──────────────────────────────────────

  it('lists users in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(await screen.findByText('أليس')).toBeInTheDocument();
    expect(screen.getByText('بوب')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الاسم' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الدور' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الحالة' })).toBeInTheDocument();
  });

  // ── Lists users in English ─────────────────────────────────────

  it('lists users in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(await screen.findByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
    expect(screen.queryByText('أليس')).not.toBeInTheDocument();
  });

  // ── Role select in Arabic ──────────────────────────────────────

  it('shows role options in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    expect(select).toHaveValue('0');
  });

  // ── Role select in English ─────────────────────────────────────

  it('shows role options in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    expect(select).toHaveValue('0');
  });

  // ── Status text in Arabic ──────────────────────────────────────

  it('shows status text in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('أليس')).closest('tr')!;
    expect(within(row).getByText('نشط')).toBeInTheDocument();
  });

  it('shows inactive status in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('بوب')).closest('tr')!;
    expect(within(row).getByText('غير نشط')).toBeInTheDocument();
  });

  // ── Status text in English ─────────────────────────────────────

  it('shows status text in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('Alice')).closest('tr')!;
    expect(within(row).getByText('Active')).toBeInTheDocument();
  });

  // ── Changing role sends correct PATCH body ─────────────────────

  it('changes role and shows success in Arabic', async () => {
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      const body = JSON.parse(init!.body as string);
      const updatedUser = USERS.find((u) => u.id === 'u1')!;
      if (body.role != null) {
        return new Response(JSON.stringify({ ...updatedUser, role: body.role }), { status: 200 });
      }
      if (body.isActive != null) {
        return new Response(JSON.stringify({ ...updatedUser, isActive: body.isActive }), { status: 200 });
      }
      return new Response(JSON.stringify(updatedUser), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    await userEvent.selectOptions(select, '1');

    const patchCall = fetchMock.mock.calls.find((c) => c[0]?.startsWith('/api/users/u1'));
    expect(patchCall).toBeDefined();
    const patchBody = JSON.parse(patchCall![1]!.body as string);
    expect(patchBody).toEqual({ role: 1 });

    await screen.findByText('تم تحديث المستخدم.');
    expect(screen.getByRole('status')).toHaveTextContent('تم تحديث المستخدم.');
  });

  it('changes role and shows success in English', async () => {
    await i18n.changeLanguage('en');
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      const body = JSON.parse(init!.body as string);
      const updatedUser = USERS.find((u) => u.id === 'u1')!;
      if (body.role != null) {
        return new Response(JSON.stringify({ ...updatedUser, role: body.role }), { status: 200 });
      }
      if (body.isActive != null) {
        return new Response(JSON.stringify({ ...updatedUser, isActive: body.isActive }), { status: 200 });
      }
      return new Response(JSON.stringify(updatedUser), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    await userEvent.selectOptions(select, '1');

    const patchCall = fetchMock.mock.calls.find((c) => c[0]?.startsWith('/api/users/u1'));
    expect(patchCall).toBeDefined();
    const patchBody = JSON.parse(patchCall![1]!.body as string);
    expect(patchBody).toEqual({ role: 1 });

    await screen.findByText('User updated.');
    expect(screen.getByRole('status')).toHaveTextContent('User updated.');
  });

  // ── Toggling status sends correct PATCH body ───────────────────

  it('toggles status to inactive and shows success in Arabic', async () => {
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      const body = JSON.parse(init!.body as string);
      const updatedUser = USERS.find((u) => u.id === 'u1')!;
      if (body.role != null) {
        return new Response(JSON.stringify({ ...updatedUser, role: body.role }), { status: 200 });
      }
      if (body.isActive != null) {
        return new Response(JSON.stringify({ ...updatedUser, isActive: body.isActive }), { status: 200 });
      }
      return new Response(JSON.stringify(updatedUser), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const button = within(row).getByRole('button', { name: /أليس إلغاء التفعيل/ });
    await userEvent.click(button);

    const patchCall = fetchMock.mock.calls.find((c) => c[0]?.startsWith('/api/users/u1'));
    expect(patchCall).toBeDefined();
    const patchBody = JSON.parse(patchCall![1]!.body as string);
    expect(patchBody).toEqual({ isActive: false });

    await screen.findByText('تم تحديث المستخدم.');
  });

  it('toggles status to active and shows success in Arabic', async () => {
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      const body = JSON.parse(init!.body as string);
      const updatedUser = USERS.find((u) => u.id === 'u2')!;
      if (body.role != null) {
        return new Response(JSON.stringify({ ...updatedUser, role: body.role }), { status: 200 });
      }
      if (body.isActive != null) {
        return new Response(JSON.stringify({ ...updatedUser, isActive: body.isActive }), { status: 200 });
      }
      return new Response(JSON.stringify(updatedUser), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('بوب');
    const row = (await screen.findByText('بوب')).closest('tr')!;
    const button = within(row).getByRole('button', { name: /بوب تفعيل/ });
    await userEvent.click(button);

    const patchCall = fetchMock.mock.calls.find((c) => c[0]?.startsWith('/api/users/u2'));
    expect(patchCall).toBeDefined();
    const patchBody = JSON.parse(patchCall![1]!.body as string);
    expect(patchBody).toEqual({ isActive: true });

    await screen.findByText('تم تحديث المستخدم.');
  });

  it('toggles status to inactive and shows success in English', async () => {
    await i18n.changeLanguage('en');
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      const body = JSON.parse(init!.body as string);
      const updatedUser = USERS.find((u) => u.id === 'u1')!;
      if (body.role != null) {
        return new Response(JSON.stringify({ ...updatedUser, role: body.role }), { status: 200 });
      }
      if (body.isActive != null) {
        return new Response(JSON.stringify({ ...updatedUser, isActive: body.isActive }), { status: 200 });
      }
      return new Response(JSON.stringify(updatedUser), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const button = within(row).getByRole('button', { name: /Alice Deactivate/ });
    await userEvent.click(button);

    const patchCall = fetchMock.mock.calls.find((c) => c[0]?.startsWith('/api/users/u1'));
    expect(patchCall).toBeDefined();
    const patchBody = JSON.parse(patchCall![1]!.body as string);
    expect(patchBody).toEqual({ isActive: false });

    await screen.findByText('User updated.');
  });

  // ── PATCH 403 → forbidden ─────────────────────────────────────

  it('shows forbidden on PATCH 403 in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'forbidden' }), { status: 403 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('ليس لديك صلاحية إدارة المستخدمين.');
  });

  it('shows forbidden on PATCH 403 in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'forbidden' }), { status: 403 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('You do not have permission to manage users.');
  });

  // ── PATCH 404 → notFound ──────────────────────────────────────

  it('shows notFound on PATCH 404 in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'not_found' }), { status: 404 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('لم يتم العثور على المستخدم.');
  });

  it('shows notFound on PATCH 404 in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'not_found' }), { status: 404 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('The user was not found.');
  });

  // ── PATCH 500 → error ────────────────────────────────────────

  it('shows error on PATCH 500 in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'error' }), { status: 500 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ التغيير.');
  });

  it('shows error on PATCH 500 in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Response(JSON.stringify({ title: 'error' }), { status: 500 });
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));
    await userEvent.selectOptions(select, '1');

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('The change could not be saved.');
  });

  // ── GET 403 on load → forbidden (no retry) ───────────────────

  it('shows forbidden on GET 403 in Arabic', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify({ title: 'forbidden' }), { status: 403 });
      }
      return new Response('{}', { status: 200 });
    }));
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('ليس لديك صلاحية إدارة المستخدمين.');
    expect(screen.queryByRole('button', { name: 'إعادة المحاولة' })).not.toBeInTheDocument();
  });

  it('shows forbidden on GET 403 in English', async () => {
    await i18n.changeLanguage('en');
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users') {
        return new Response(JSON.stringify({ title: 'forbidden' }), { status: 403 });
      }
      return new Response('{}', { status: 200 });
    }));
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByRole('alert');
    expect(screen.getByRole('alert')).toHaveTextContent('You do not have permission to manage users.');
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument();
  });

  // ── Empty list ────────────────────────────────────────────────

  it('shows empty message in Arabic', async () => {
    stubFetch([]);
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(await screen.findByText('لا يوجد مستخدمون بعد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows empty message in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch([]);
    renderWithProviders(<UsersPage />, { route: '/users' });
    expect(await screen.findByText('No users yet.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  // ── In-flight disabled + saving text in Arabic ────────────────

  it('disables controls and shows saving text while in flight in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('أليس');
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    const button = within(row).getByRole('button', { name: /أليس إلغاء التفعيل/ });

    // Mock the PATCH to never resolve (in-flight)
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Promise(() => {});
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));

    await userEvent.selectOptions(select, '1');

    expect(select).toBeDisabled();
    expect(button).toBeDisabled();
    expect(button).toHaveTextContent('جارٍ الحفظ…');
  });

  it('disables controls and shows saving text while in flight in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });

    await screen.findByText('Alice');
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const select = within(row).getByRole('combobox');
    const button = within(row).getByRole('button', { name: /Alice Deactivate/ });

    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users/u1') {
        return new Promise(() => {});
      }
      return new Response(JSON.stringify(USERS), { status: 200 });
    }));

    await userEvent.selectOptions(select, '1');

    expect(select).toBeDisabled();
    expect(button).toBeDisabled();
    expect(button).toHaveTextContent('Saving…');
  });

  // ── aria-pressed on toggle button ─────────────────────────────

  it('has aria-pressed in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('أليس')).closest('tr')!;
    const button = within(row).getByRole('button');
    expect(button).toHaveAttribute('aria-pressed', 'true');
  });

  it('has aria-pressed in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('Alice')).closest('tr')!;
    const button = within(row).getByRole('button');
    expect(button).toHaveAttribute('aria-pressed', 'true');
  });

  // ── axe in Arabic ─────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    stubFetch(USERS);
    const container = renderWithProviders(<UsersPage />, { route: '/users' });
    await screen.findByRole('table');
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── axe in English ────────────────────────────────────────────

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    const container = renderWithProviders(<UsersPage />, { route: '/users' });
    await screen.findByRole('table');
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── GET 500 on load → error with retry ───────────────────────

  it('shows error and loads again on retry in Arabic', async () => {
    let callCount = 0;
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users') {
        callCount++;
        if (callCount === 1) {
          return new Response(JSON.stringify({ title: 'error' }), { status: 500 });
        }
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      return new Response('{}', { status: 200 });
    }));
    renderWithProviders(<UsersPage />, { route: '/users' });

    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر حفظ التغيير.');
    const retryBtn = screen.getByRole('button', { name: 'إعادة المحاولة' });
    await userEvent.click(retryBtn);
    expect(await screen.findByText('أليس')).toBeInTheDocument();
  });

  it('shows error and loads again on retry in English', async () => {
    await i18n.changeLanguage('en');
    let callCount = 0;
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => {
      if (url === '/api/users') {
        callCount++;
        if (callCount === 1) {
          return new Response(JSON.stringify({ title: 'error' }), { status: 500 });
        }
        return new Response(JSON.stringify(USERS), { status: 200 });
      }
      return new Response('{}', { status: 200 });
    }));
    renderWithProviders(<UsersPage />, { route: '/users' });

    expect(await screen.findByRole('alert')).toHaveTextContent('The change could not be saved.');
    const retryBtn = screen.getByRole('button', { name: 'Try again' });
    await userEvent.click(retryBtn);
    expect(await screen.findByText('Alice')).toBeInTheDocument();
  });

  // ── Ask the API as signed-in company and role ────────────────

  it('asks the API as the signed-in company and role', async () => {
    const fetchMock = stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    await screen.findByText('أليس');
    expect(fetchMock).toHaveBeenCalledWith('/api/users', {
      headers: { Accept: 'application/json', 'X-Company-Id': COMPANY_ID, 'X-Role': 'Admin' },
    });
  });

  // ── Name via <bdi> in Arabic ─────────────────────────────────

  it('shows name via bdi in Arabic', async () => {
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('أليس')).closest('tr')!;
    expect(within(row).getByText('أليس')).toBeInTheDocument();
  });

  // ── Name via <bdi> in English ────────────────────────────────

  it('shows name via bdi in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(USERS);
    renderWithProviders(<UsersPage />, { route: '/users' });
    const row = (await screen.findByText('Alice')).closest('tr')!;
    expect(within(row).getByText('Alice')).toBeInTheDocument();
  });
});
