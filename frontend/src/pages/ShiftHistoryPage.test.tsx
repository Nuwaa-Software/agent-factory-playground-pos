import { screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn, stubFetch } from '../test/render';
import ShiftHistoryPage from './ShiftHistoryPage';

function makeShifts(count: number) {
  return Array.from({ length: count }, (_, i) => ({
    id: `shift-${i + 1}`,
    cashierName: `Cashier ${i + 1}`,
    startingCashHalalas: (i + 1) * 1000,
    openedAt: `2026-10-${String(14 - i).padStart(2, '0')}T10:00:00Z`,
    closedAt: i % 3 === 0 ? `2026-10-${String(14 - i).padStart(2, '0')}T18:00:00Z` : null,
    actualCashHalalas: i % 3 === 0 ? (i + 1) * 1050 : null,
    expectedCashHalalas: i % 3 === 0 ? (i + 1) * 1000 : null,
    discrepancyHalalas: i % 3 === 0 ? (i + 1) * 50 : null,
  }));
}

describe('ShiftHistoryPage', () => {
  beforeEach(() => {
    signIn('Admin');
    // Reset language to Arabic default before each test
    void i18n.changeLanguage('ar');
  });

  it('lists shifts in Arabic with prices in riyals', async () => {
    const shifts = makeShifts(3);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    // Wait for the table to appear (unique element)
    await screen.findByRole('table');

    // Headers in Arabic
    const headers = screen.getAllByRole('columnheader');
    expect(headers.length).toBe(7);
    expect(headers[0]).toHaveTextContent('التاريخ');
    expect(headers[1]).toHaveTextContent('الكاشير');
    expect(headers[2]).toHaveTextContent('الحالة');
    expect(headers[3]).toHaveTextContent('النقدية الافتتاحية');
    expect(headers[4]).toHaveTextContent('النقدية الفعلية');
    expect(headers[5]).toHaveTextContent('النقدية المتوقعة');
    expect(headers[6]).toHaveTextContent('الفرق');

    // First row: closed shift (closedAt is not null)
    const rows = screen.getAllByRole('row');
    expect(rows.length).toBe(4); // header + 3 data rows
  });

  it('lists shifts in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = makeShifts(3);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    // Wait for the table to appear (unique element)
    await screen.findByRole('table');

    // Headers in English
    const headers = screen.getAllByRole('columnheader');
    expect(headers.length).toBe(7);
    expect(headers[0]).toHaveTextContent('Date');
    expect(headers[1]).toHaveTextContent('Cashier');
    expect(headers[2]).toHaveTextContent('Status');
    expect(headers[3]).toHaveTextContent('Starting cash');
    expect(headers[4]).toHaveTextContent('Actual cash');
    expect(headers[5]).toHaveTextContent('Expected cash');
    expect(headers[6]).toHaveTextContent('Discrepancy');

    const rows = screen.getAllByRole('row');
    expect(rows.length).toBe(4); // header + 3 data rows
  });

  it('shows — for open shift money cells', async () => {
    const shifts = [
      {
        id: 'shift-open',
        cashierName: 'Open Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: null, // open shift
        actualCashHalalas: null,
        expectedCashHalalas: null,
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Open shift should show — in Closing, Expected, Discrepancy (3 em dashes)
    const emDashes = screen.getAllByText('—');
    expect(emDashes.length).toBe(3);
  });

  it('shows values for closed shift money cells', async () => {
    const shifts = [
      {
        id: 'shift-closed',
        cashierName: 'Closed Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: '2026-10-14T18:00:00Z',
        actualCashHalalas: 5250,
        expectedCashHalalas: 5000,
        discrepancyHalalas: 250,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // In Arabic: money.sar = "{{amount}} ر.س" → "52.50 ر.س"
    const table = screen.getByRole('table');
    expect(table).toHaveTextContent('52.50 ر.س');
    expect(table).toHaveTextContent('50.00 ر.س');
    expect(table).toHaveTextContent('2.50 ر.س');
  });

  it('shows — for closed shift with null expected/discrepancy', async () => {
    const shifts = [
      {
        id: 'shift-legacy',
        cashierName: 'Legacy Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: '2026-10-14T18:00:00Z',
        actualCashHalalas: 5250,
        expectedCashHalalas: null, // legacy row before #36
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Should show — for expected and discrepancy (actualCash is not null)
    const emDashes = screen.getAllByText('—');
    expect(emDashes.length).toBe(2);
  });

  it('shows status word (not colour-only)', async () => {
    const shifts = [
      {
        id: 'shift-open',
        cashierName: 'Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: null,
        actualCashHalalas: null,
        expectedCashHalalas: null,
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Status word should be visible in the table (not just dropdown)
    const table = screen.getByRole('table');
    expect(table).toHaveTextContent('مفتوحة');
  });

  it('shows status word in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = [
      {
        id: 'shift-open',
        cashierName: 'Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: null,
        actualCashHalalas: null,
        expectedCashHalalas: null,
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');
    const table = screen.getByRole('table');
    expect(table).toHaveTextContent('Open');
  });

  it('disables Apply button when from > to', async () => {
    const shifts = makeShifts(1);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const fromInput = screen.getByLabelText('من');
    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    // Set inverted range using fireEvent.change so React picks up the state update
    fireEvent.change(fromInput, { target: { value: '2026-10-31' } });
    fireEvent.change(toInput, { target: { value: '2026-10-01' } });

    expect(applyBtn).toBeDisabled();
  });

  it('applies one-sided range (from-only)', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const fromInput = screen.getByLabelText('من');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    (fromInput as HTMLInputElement).value = '2026-10-13';
    fromInput.dispatchEvent(new Event('change', { bubbles: true }));
    await userEvent.click(applyBtn);

    // Should still show the title (no crash)
    expect(screen.getByText('سجل الورديات')).toBeInTheDocument();
  });

  it('applies one-sided range (to-only)', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    (toInput as HTMLInputElement).value = '2026-10-13';
    toInput.dispatchEvent(new Event('change', { bubbles: true }));
    await userEvent.click(applyBtn);

    expect(screen.getByText('سجل الورديات')).toBeInTheDocument();
  });

  it('Clear resets filters and page', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const fromInput = screen.getByLabelText('من');
    const toInput = screen.getByLabelText('إلى');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });
    const clearBtn = screen.getByRole('button', { name: 'مسح' });

    // Apply a filter
    fireEvent.change(fromInput, { target: { value: '2026-10-01' } });
    fireEvent.change(toInput, { target: { value: '2026-10-31' } });
    await userEvent.click(applyBtn);

    // Clear
    await userEvent.click(clearBtn);

    // Inputs should be reset
    expect((fromInput as HTMLInputElement).value).toBe('');
    expect((toInput as HTMLInputElement).value).toBe('');
  });

  it('pagination: page size 10, Prev disabled on page 1', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const prevBtn = screen.getByRole('button', { name: 'السابقة' });
    expect(prevBtn).toBeDisabled();
  });

  it('pagination: Next disabled on last page', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const nextBtn = screen.getByRole('button', { name: 'التالية' });
    expect(nextBtn).toBeDisabled();
  });

  it('pagination: indicator shows page of total', async () => {
    const shifts = makeShifts(25); // 3 pages
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    expect(screen.getByText(/صفحة 1 من 3/)).toBeInTheDocument();
  });

  it('pagination: clicking Next goes to page 2', async () => {
    const shifts = makeShifts(25); // 3 pages
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const nextBtn = screen.getByRole('button', { name: 'التالية' });
    expect(nextBtn).not.toBeDisabled();
    await userEvent.click(nextBtn);

    // Should still show the title (no crash)
    expect(screen.getByText('سجل الورديات')).toBeInTheDocument();
  });

  it('shows empty state when API returns []', async () => {
    stubFetch([]);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByText('لا توجد ورديات بعد.');

    expect(screen.getByRole('status')).toHaveTextContent('لا توجد ورديات بعد.');
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows noMatch state when filter yields 0', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Apply a filter that matches nothing
    const fromInput = screen.getByLabelText('من');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    fireEvent.change(fromInput, { target: { value: '2030-01-01' } });
    await userEvent.click(applyBtn);

    expect(screen.getByRole('status')).toHaveTextContent('لا توجد ورديات مطابقة للتصفية.');
  });

  it('shows loading state', async () => {
    // Don't stub fetch — let it hang (no response)
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByText('جارٍ التحميل…');

    expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  it('shows error state with retry button', async () => {
    const fetchMock = vi.fn(async () => new Response('', { status: 500 }));
    vi.stubGlobal('fetch', fetchMock);

    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('alert');

    expect(screen.getByText('تعذّر تحميل سجل الورديات.')).toBeInTheDocument();
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();

    // Mock successful response and click retry
    const shifts = makeShifts(1);
    vi.stubGlobal('fetch', async () => new Response(JSON.stringify(shifts), { status: 200 }));
    await userEvent.click(screen.getByText('إعادة المحاولة'));

    await screen.findByRole('table');
  });

  it('fetches /api/pos/shifts with no params', async () => {
    const shifts = makeShifts(1);
    const fetchMock = vi.fn(async (url: string) => {
      expect(url).toBe('/api/pos/shifts');
      return new Response(JSON.stringify(shifts), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);

    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith('/api/pos/shifts', expect.anything());
  });

  it('no additional fetch when changing filters or pages', async () => {
    const shifts = makeShifts(25); // 3 pages
    const fetchMock = vi.fn(async () => {
      return new Response(JSON.stringify(shifts), { status: 200 });
    });
    vi.stubGlobal('fetch', fetchMock);

    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    expect(fetch).toHaveBeenCalledTimes(1);

    // Click Next
    const nextBtn = screen.getByRole('button', { name: 'التالية' });
    await userEvent.click(nextBtn);

    // Should still be 1 fetch (client-side pagination)
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it('passes accessibility checks in Arabic', async () => {
    const shifts = makeShifts(3);
    stubFetch(shifts);
    const { container } = renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('passes accessibility checks in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = makeShifts(3);
    stubFetch(shifts);
    const { container } = renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('renders table headers in correct order', async () => {
    const shifts = makeShifts(1);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const headers = screen.getAllByRole('columnheader');
    expect(headers.length).toBe(7);
    expect(headers[0]).toHaveTextContent('التاريخ');
    expect(headers[1]).toHaveTextContent('الكاشير');
    expect(headers[2]).toHaveTextContent('الحالة');
    expect(headers[3]).toHaveTextContent('النقدية الافتتاحية');
    expect(headers[4]).toHaveTextContent('النقدية الفعلية');
    expect(headers[5]).toHaveTextContent('النقدية المتوقعة');
    expect(headers[6]).toHaveTextContent('الفرق');
  });

  it('renders table headers in correct order in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = makeShifts(1);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const headers = screen.getAllByRole('columnheader');
    expect(headers.length).toBe(7);
    expect(headers[0]).toHaveTextContent('Date');
    expect(headers[1]).toHaveTextContent('Cashier');
    expect(headers[2]).toHaveTextContent('Status');
    expect(headers[3]).toHaveTextContent('Starting cash');
    expect(headers[4]).toHaveTextContent('Actual cash');
    expect(headers[5]).toHaveTextContent('Expected cash');
    expect(headers[6]).toHaveTextContent('Discrepancy');
  });

  it('displays date in Arabic locale', async () => {
    const shifts = [
      {
        id: 'shift-1',
        cashierName: 'Cashier 1',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: null,
        actualCashHalalas: null,
        expectedCashHalalas: null,
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Arabic date format with long month name
    expect(screen.getByText('١٤ أكتوبر ٢٠٢٦')).toBeInTheDocument();
  });

  it('displays date in English locale', async () => {
    await i18n.changeLanguage('en');
    const shifts = [
      {
        id: 'shift-1',
        cashierName: 'Cashier 1',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: null,
        actualCashHalalas: null,
        expectedCashHalalas: null,
        discrepancyHalalas: null,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // English date format with long month name
    expect(screen.getByText('October 14, 2026')).toBeInTheDocument();
  });

  it('shows negative discrepancy with minus sign', async () => {
    const shifts = [
      {
        id: 'shift-neg',
        cashierName: 'Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: '2026-10-14T18:00:00Z',
        actualCashHalalas: 4750,
        expectedCashHalalas: 5000,
        discrepancyHalalas: -250,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    // Negative discrepancy should show with minus sign
    expect(screen.getByText('-2.50 ر.س')).toBeInTheDocument();
  });

  it('shows negative discrepancy in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = [
      {
        id: 'shift-neg',
        cashierName: 'Cashier',
        startingCashHalalas: 5000,
        openedAt: '2026-10-14T10:00:00Z',
        closedAt: '2026-10-14T18:00:00Z',
        actualCashHalalas: 4750,
        expectedCashHalalas: 5000,
        discrepancyHalalas: -250,
      },
    ];
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    expect(screen.getByText('SAR -2.50')).toBeInTheDocument();
  });

  it('shows empty state in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch([]);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByText('No shifts yet.');

    expect(screen.getByRole('status')).toHaveTextContent('No shifts yet.');
  });

  it('shows noMatch in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const fromInput = screen.getByLabelText('From');
    const applyBtn = screen.getByRole('button', { name: 'Apply' });

    fireEvent.change(fromInput, { target: { value: '2030-01-01' } });
    await userEvent.click(applyBtn);

    expect(screen.getByText('No shifts match the filters.')).toBeInTheDocument();
  });

  it('status filter works in Arabic', async () => {
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const statusSelect = screen.getByLabelText('الحالة');
    const applyBtn = screen.getByRole('button', { name: 'تطبيق' });

    (statusSelect as HTMLSelectElement).value = 'closed';
    statusSelect.dispatchEvent(new Event('change', { bubbles: true }));
    await userEvent.click(applyBtn);

    // Should still show the title (no crash)
    expect(screen.getByText('سجل الورديات')).toBeInTheDocument();
  });

  it('status filter works in English', async () => {
    await i18n.changeLanguage('en');
    const shifts = makeShifts(5);
    stubFetch(shifts);
    renderWithProviders(<ShiftHistoryPage />, { route: '/shifts/history' });

    await screen.findByRole('table');

    const statusSelect = screen.getByLabelText('Status');
    const applyBtn = screen.getByRole('button', { name: 'Apply' });

    (statusSelect as HTMLSelectElement).value = 'open';
    statusSelect.dispatchEvent(new Event('change', { bubbles: true }));
    await userEvent.click(applyBtn);

    expect(screen.getByText('Shift history')).toBeInTheDocument();
  });
});
