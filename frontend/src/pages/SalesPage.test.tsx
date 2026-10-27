import { screen, fireEvent, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useSales } from '../api/sales';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, stubFetch } from '../test/render';
import SalesPage from './SalesPage';

describe('SalesPage', () => {
  const saleSummary = {
    id: 'sale-1',
    number: 123,
    createdAt: '2026-10-14T10:30:00Z',
    netHalalas: 1250,
    vatHalalas: 125,
    totalHalalas: 1375,
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

  it('renders date filter inputs with labels above the table', async () => {
    renderWithProviders(<SalesPage />);

    // Wait for data to load so the page renders fully
    await screen.findByText('رقم الفاتورة');

    expect(screen.getByLabelText('من')).toBeInTheDocument();
    expect(screen.getByLabelText('إلى')).toBeInTheDocument();
  });

  it('applies filter and sends from/to params, resets to page 1', async () => {
    stubFetch({
      sales: [saleSummary],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 2,
    });
    renderWithProviders(<SalesPage />);

    // Wait for initial load
    await screen.findByText('رقم الفاتورة');

    const fromInput = screen.getByLabelText('من');
    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    // Set dates using fireEvent.change (works with React's synthetic events)
    fireEvent.change(fromInput, { target: { value: '2026-10-01' } });
    fireEvent.change(toInput, { target: { value: '2026-10-31' } });

    // Click Apply
    await userEvent.click(applyBtn);

    // Wait for the refetch to complete (data reappears)
    await screen.findByText('رقم الفاتورة');

    // Check fetch was called with from and to params
    const calls = (fetch as ReturnType<typeof vi.fn>).mock.calls;
    const lastCall = calls[calls.length - 1][0] as string;
    expect(lastCall).toContain('from=');
    expect(lastCall).toContain('to=');
    expect(lastCall).toContain('page=1');
  });

  it('clears filter and reloads unfiltered list', async () => {
    stubFetch({
      sales: [saleSummary],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });
    renderWithProviders(<SalesPage />);

    await screen.findByText('رقم الفاتورة');

    const fromInput = screen.getByLabelText('من');
    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });
    const clearBtn = screen.getByRole('button', { name: 'مسح' });

    // Apply a filter
    fireEvent.change(fromInput, { target: { value: '2026-10-01' } });
    fireEvent.change(toInput, { target: { value: '2026-10-31' } });
    await userEvent.click(applyBtn);
    await screen.findByText('رقم الفاتورة');

    // Clear the filter
    await userEvent.click(clearBtn);

    // Wait for the refetch to complete
    await screen.findByText('رقم الفاتورة');

    // Check fetch was called without from/to
    const calls = (fetch as ReturnType<typeof vi.fn>).mock.calls;
    const lastCall = calls[calls.length - 1][0] as string;
    expect(lastCall).not.toContain('from=');
    expect(lastCall).not.toContain('to=');
    expect(lastCall).toContain('page=1');
  });

  it('pagination after filter still includes from/to params', async () => {
    // Use renderHook to test useSales directly with different query keys.
    const { result, rerender } = renderHook(
      ({ page, from, to }: { page: number; from?: string | undefined; to?: string | undefined }) =>
        useSales(page, from, to),
      {
        wrapper: ({ children }) => (
          <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
            {children}
          </QueryClientProvider>
        ),
        initialProps: { page: 1, from: undefined as string | undefined, to: undefined as string | undefined },
      },
    );

    // Initial unfiltered fetch
    await waitFor(() => result.current.isSuccess);
    expect(fetch).toHaveBeenCalledWith(
      '/api/pos/sales?page=1&pageSize=20',
      expect.anything(),
    );

    (fetch as ReturnType<typeof vi.fn>).mockClear();

    // Apply filter: change page to 2 and add from/to
    rerender({ page: 2, from: '2026-10-01T00:00:00Z' as string | undefined, to: '2026-10-31T23:59:59Z' as string | undefined });

    await waitFor(() => result.current.isSuccess);
    const calls = (fetch as ReturnType<typeof vi.fn>).mock.calls;
    expect(calls.length).toBe(1);
    expect(calls[0][0]).toContain('page=2');
    expect(calls[0][0]).toContain('from=');
    expect(calls[0][0]).toContain('to=');
  });

  it('disables Apply button when from > to', async () => {
    renderWithProviders(<SalesPage />);

    await screen.findByText('رقم الفاتورة');

    const fromInput = screen.getByLabelText('من');
    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    // Set inverted range
    (fromInput as HTMLInputElement).value = '2026-10-31';
    fromInput.dispatchEvent(new Event('change', { bubbles: true }));
    (toInput as HTMLInputElement).value = '2026-10-01';
    toInput.dispatchEvent(new Event('change', { bubbles: true }));

    expect(applyBtn).toBeDisabled();
  });
});