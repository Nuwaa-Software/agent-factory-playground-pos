import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, stubFetch, signIn } from '../test/render';
import SalesPage from './SalesPage';

describe('SalesPage', () => {
  const saleSummary = {
    id: 'sale-1',
    number: 123,
    createdAt: '2026-10-14T10:30:00Z',
    netHalalas: 1250,
    vatHalalas: 125,
    totalHalalas: 1375,
    voided: false,
    voidedAt: null,
    paymentType: 1,
    customerId: null,
  };

  const salesListResponse = {
    sales: [saleSummary],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    stubFetch(salesListResponse);
  });

  it('lists sales in Arabic with prices in riyals', async () => {
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    // Check headers are in Arabic
    expect(screen.getByText('رقم الفاتورة')).toBeInTheDocument();
    expect(screen.getByText('التاريخ')).toBeInTheDocument();
    expect(screen.getByText('صافي')).toBeInTheDocument();
    expect(screen.getByText('الضريبة')).toBeInTheDocument();
    expect(screen.getByText('الإجمالي')).toBeInTheDocument();

    // Check sale data
    expect(screen.getByText('123')).toBeInTheDocument();
    // The actual rendered date has different Unicode characters for slashes
    const dateCell = screen.getByText(/١٤‏\/١٠‏\/٢٠٢٦/);
    expect(dateCell).toBeInTheDocument();
    expect(screen.getByText('12.50 ر.س')).toBeInTheDocument(); // Arabic money format
    expect(screen.getByText('1.25 ر.س')).toBeInTheDocument();
    expect(screen.getByText('13.75 ر.س')).toBeInTheDocument();
  });

  it('lists sales in English', async () => {
    await i18n.changeLanguage('en');
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('Invoice #');

    // Check headers are in English
    expect(screen.getByText('Invoice #')).toBeInTheDocument();
    expect(screen.getByText('Date')).toBeInTheDocument();
    expect(screen.getByText('Net')).toBeInTheDocument();
    expect(screen.getByText('VAT')).toBeInTheDocument();
    expect(screen.getByText('Total')).toBeInTheDocument();

    // Check sale data
    expect(screen.getByText('123')).toBeInTheDocument();
    expect(screen.getByText('10/14/2026')).toBeInTheDocument(); // English date format
    expect(screen.getByText('SAR 12.50')).toBeInTheDocument(); // English money format
    expect(screen.getByText('SAR 1.25')).toBeInTheDocument();
    expect(screen.getByText('SAR 13.75')).toBeInTheDocument();
  });

  it('shows empty state', async () => {
    stubFetch({
      sales: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('لا توجد مبيعات بعد.');

    expect(screen.getByText('لا توجد مبيعات بعد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows error state with retry button', async () => {
    stubFetch({}, 500);
    renderWithProviders(<SalesPage />);

    // Wait for error to appear
    await screen.findByRole('alert');

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('تعذّر تحميل المبيعات.')).toBeInTheDocument();
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();

    // Mock successful response and click retry
    stubFetch(salesListResponse);
    await userEvent.click(screen.getByText('إعادة المحاولة'));

    // Wait for data to load after retry
    await screen.findByText('123');
    expect(screen.getByText('123')).toBeInTheDocument();
  });

  it('disables previous button on first page', async () => {
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const prevButton = screen.getByRole('button', { name: 'السابقة' });
    expect(prevButton).toBeDisabled();
  });

  it('disables next button on last page', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 1,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    expect(nextButton).toBeDisabled();
  });

  it('enables next button on first page with multiple pages', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 2,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    expect(nextButton).not.toBeDisabled();
  });

  it('fetches page 2 when clicking next button', async () => {
    stubFetch({
      ...salesListResponse,
      totalPages: 2,
    });
    renderWithProviders(<SalesPage />);

    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const nextButton = screen.getByRole('button', { name: 'التالية' });
    await userEvent.click(nextButton);

    // Check that fetch was called with page 2
    expect(fetch).toHaveBeenCalledWith('/api/pos/sales?page=2&pageSize=20', expect.anything());
  });

  it('passes accessibility checks in Arabic', async () => {
    const { container } = renderWithProviders(<SalesPage />);
    
    // Wait for data to load
    await screen.findByText('رقم الفاتورة');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('passes accessibility checks in English', async () => {
    await i18n.changeLanguage('en');
    const { container } = renderWithProviders(<SalesPage />);
    
    // Wait for data to load
    await screen.findByText('Invoice #');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});

describe('VoidSaleDialog', () => {
  const saleSummary = {
    id: 'sale-1',
    number: 123,
    createdAt: '2026-10-14T10:30:00Z',
    netHalalas: 1250,
    vatHalalas: 125,
    totalHalalas: 1375,
    voided: false,
    voidedAt: null,
    paymentType: 1,
    customerId: null,
  };

  const salesListResponse = {
    sales: [saleSummary],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  };

  function stubUrlAwareFetch(
    onVoid: (url: string) => Response | Promise<Response>
  ) {
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      void init;
      if (url.includes('/api/pos/sales?page=')) {
        return new Response(JSON.stringify(salesListResponse), { status: 200 });
      }
      return onVoid(url);
    });
    Object.defineProperty(globalThis, 'fetch', {
      value: fetchMock,
      writable: true,
      configurable: true,
    });
    return fetchMock;
  }

  it('voids a sale successfully (PATCH with no body)', async () => {
    signIn('Admin');
    const voidResponse = {
      id: 'sale-1',
      number: 123,
      createdAt: '2026-10-14T10:30:00Z',
      lines: [],
      netHalalas: 1250,
      vatHalalas: 125,
      totalHalalas: 1375,
      qrCode: '',
      voided: true,
      voidedAt: '2026-10-14T11:00:00Z',
      paymentType: 1,
      customerId: null,
    };

    const mock = vi.fn(async (url: string, init?: RequestInit) => {
      void init;
      if (url.includes('/api/pos/sales?page=')) {
        return new Response(JSON.stringify(salesListResponse), { status: 200 });
      }
      if (url.includes('/api/pos/sales/sale-1/void')) {
        return new Response(JSON.stringify(voidResponse), { status: 200 });
      }
      return new Response('', { status: 404 });
    });

    vi.stubGlobal('fetch', mock);

    renderWithProviders(<SalesPage />);
    await screen.findByText('رقم الفاتورة');

    // Open the void dialog
    const voidButton = screen.getByRole('button', { name: 'إلغاء' });
    await userEvent.click(voidButton);

    // Dialog should be open
    expect(screen.getByRole('dialog')).toBeInTheDocument();

    // Type a reason (should NOT be sent to the API)
    await userEvent.type(screen.getByLabelText(/السبب/), 'Customer complaint');

    // Confirm
    const confirmBtn = screen.getByRole('button', { name: 'إلغاء العملية' });
    await userEvent.click(confirmBtn);

    // Wait for success message
    await screen.findByText('تم إلغاء العملية.');

    // Assert the fetch was called with PATCH and no body
    const voidCall = mock.mock.calls.find(
      (c) => c[0]?.includes('/api/pos/sales/sale-1/void')
    );
    expect(voidCall).toBeDefined();
    expect(voidCall![1]?.method).toBe('PATCH');
    // body should be undefined (no body sent)
    expect(voidCall![1]?.body).toBeUndefined();

    // Reason value should not appear in any fetch call's body
    for (const call of mock.mock.calls) {
      const body = call[1]?.body;
      if (typeof body === 'string') expect(body).not.toContain('Customer complaint');
    }

    // Dialog should be closed
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('shows void.forbidden on 403', async () => {
    signIn('Cashier');

    stubUrlAwareFetch(async (url: string) => {
      if (url.includes('/api/pos/sales/sale-1/void')) {
        return new Response('', { status: 403 });
      }
      return new Response('', { status: 404 });
    });

    renderWithProviders(<SalesPage />);
    await screen.findByText('رقم الفاتورة');

    const voidButton = screen.getByRole('button', { name: 'إلغاء' });
    await userEvent.click(voidButton);

    expect(screen.getByRole('dialog')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'إلغاء العملية' }));

    // Error message should be shown in role="alert"
    const alert = screen.getByRole('alert');
    expect(alert).toHaveTextContent('ليس لديك صلاحية إلغاء العمليات.');

    // Dialog should still be open
    expect(screen.queryByRole('dialog')).toBeInTheDocument();
  });

  it('shows void.notFound on 404', async () => {
    signIn('Admin');

    stubUrlAwareFetch(async (url: string) => {
      if (url.includes('/api/pos/sales/sale-1/void')) {
        return new Response(JSON.stringify({ title: 'sale.not_found' }), { status: 404 });
      }
      return new Response('', { status: 404 });
    });

    renderWithProviders(<SalesPage />);
    await screen.findByText('رقم الفاتورة');

    const voidButton = screen.getByRole('button', { name: 'إلغاء' });
    await userEvent.click(voidButton);

    expect(screen.getByRole('dialog')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'إلغاء العملية' }));

    const alert = screen.getByRole('alert');
    expect(alert).toHaveTextContent('لم يتم العثور على العملية.');

    expect(screen.queryByRole('dialog')).toBeInTheDocument();
  });

  it('shows void.windowExpired on sale.void_window_expired code', async () => {
    signIn('Admin');

    stubUrlAwareFetch(async (url: string) => {
      if (url.includes('/api/pos/sales/sale-1/void')) {
        return new Response(JSON.stringify({ title: 'sale.void_window_expired' }), { status: 400 });
      }
      return new Response('', { status: 404 });
    });

    renderWithProviders(<SalesPage />);
    await screen.findByText('رقم الفاتورة');

    const voidButton = screen.getByRole('button', { name: 'إلغاء' });
    await userEvent.click(voidButton);

    expect(screen.getByRole('dialog')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'إلغاء العملية' }));

    const alert = screen.getByRole('alert');
    expect(alert).toHaveTextContent('هذه العملية خارج نافذة الإلغاء ولا يمكن إلغاؤها.');

    expect(screen.queryByRole('dialog')).toBeInTheDocument();
  });
});