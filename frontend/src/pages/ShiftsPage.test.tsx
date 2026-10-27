import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';

// Mock the API client before importing ShiftsPage
vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client');
  return {
    ...actual,
    apiGet: vi.fn(),
    apiPost: vi.fn(),
    apiPatch: vi.fn(),
  };
});

import ShiftsPage from './ShiftsPage';
import * as client from '../api/client';

const openShift = {
  id: 'shift-1',
  cashierName: 'Ahmed',
  startingCashHalalas: 5000,
  openedAt: '2026-10-27T08:00:00Z',
  closedAt: null,
  actualCashHalalas: null,
  expectedCashHalalas: null,
  discrepancyHalalas: null,
};

const closedShift = {
  ...openShift,
  id: 'shift-2',
  closedAt: '2026-10-27T18:00:00Z',
  actualCashHalalas: 5500,
};

describe('ShiftsPage', () => {
  beforeEach(() => {
    signIn();
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  // ── Loading state ──────────────────────────────────────────────

  it('shows loading in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  it('shows loading in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    expect(screen.getByRole('status')).toHaveTextContent('Loading…');
  });

  // ── Load error ─────────────────────────────────────────────────

  it('shows load error in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('fail'));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل الوردية.');
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();
  });

  it('shows load error in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('fail'));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The shift could not be loaded.');
    expect(screen.getByText('Try again')).toBeInTheDocument();
  });

  // ── Open form (no open shift) ──────────────────────────────────

  it('shows open form in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'فتح وردية' });
    expect(screen.getByLabelText('اسم الكاشير')).toBeInTheDocument();
    expect(screen.getByLabelText('النقدية الافتتاحية (ر.س)')).toBeInTheDocument();
  });

  it('shows open form in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'Open shift' });
    expect(screen.getByLabelText('Cashier name')).toBeInTheDocument();
    expect(screen.getByLabelText('Starting cash (SAR)')).toBeInTheDocument();
  });

  // ── Open flow ──────────────────────────────────────────────────

  it('opens a shift in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue({ ...openShift, id: 'shift-new', cashierName: 'سارة' });
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    await userEvent.type(screen.getByLabelText('اسم الكاشير'), 'سارة');
    await userEvent.type(screen.getByLabelText('النقدية الافتتاحية (ر.س)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'فتح الوردية' }));

    await screen.findByText('تم فتح الوردية.');
    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/pos/shifts/open',
      { cashierName: 'سارة', startingCashHalalas: 5000 },
    );
    const container = screen.getByRole('heading', { name: 'إغلاق الوردية' }).closest('section')!;
    expect(container.textContent).toContain('سارة');
    expect(container.textContent).toContain('50.00 ر.س');
  });

  it('opens a shift in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue({ ...openShift, id: 'shift-new', cashierName: 'Sara' });
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Open shift' });
    await userEvent.type(screen.getByLabelText('Cashier name'), 'Sara');
    await userEvent.type(screen.getByLabelText('Starting cash (SAR)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'Open shift' }));

    await screen.findByText('Shift opened.');
    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/pos/shifts/open',
      { cashierName: 'Sara', startingCashHalalas: 5000 },
    );
    const container = screen.getByRole('heading', { name: 'Close shift' }).closest('section')!;
    expect(container.textContent).toContain('Sara');
    expect(container.textContent).toContain('SAR 50.00');
  });

  // ── Close form (open shift exists) ─────────────────────────────

  it('shows close form in Arabic when open shift exists', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    expect(screen.getByText(/الكاشير/)).toBeInTheDocument();
    expect(screen.getByText(/Ahmed/)).toBeInTheDocument();
    expect(screen.getByLabelText('النقدية الفعلية (ر.س)')).toBeInTheDocument();
  });

  it('shows close form in English when open shift exists', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'Close shift' });
    expect(screen.getByText(/Cashier/)).toBeInTheDocument();
    expect(screen.getByText(/Ahmed/)).toBeInTheDocument();
    expect(screen.getByLabelText('Actual cash (SAR)')).toBeInTheDocument();
  });

  // ── Close flow ─────────────────────────────────────────────────

  it('closes a shift in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ ...closedShift, id: 'shift-2' });
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    await userEvent.type(screen.getByLabelText('النقدية الفعلية (ر.س)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'إغلاق الوردية' }));

    await screen.findByText('تم إغلاق الوردية.');
    expect(client.apiPatch).toHaveBeenCalledWith(
      `/api/pos/shifts/${openShift.id}/close`,
      { id: openShift.id, actualCashHalalas: 5500 },
    );
    expect(screen.getByText('50.00 ر.س')).toBeInTheDocument();
    expect(screen.getByText('55.00 ر.س')).toBeInTheDocument();
  });

  it('closes a shift in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ ...closedShift, id: 'shift-2' });
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Close shift' });
    await userEvent.type(screen.getByLabelText('Actual cash (SAR)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'Close shift' }));

    await screen.findByText('Shift closed.');
    expect(client.apiPatch).toHaveBeenCalledWith(
      `/api/pos/shifts/${openShift.id}/close`,
      { id: openShift.id, actualCashHalalas: 5500 },
    );
    expect(screen.getByText('SAR 50.00')).toBeInTheDocument();
    expect(screen.getByText('SAR 55.00')).toBeInTheDocument();
  });

  // ── Close 409 / 404 ────────────────────────────────────────────

  it('shows alreadyClosed on 409 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(409));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    await userEvent.type(screen.getByLabelText('النقدية الفعلية (ر.س)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'إغلاق الوردية' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert').textContent).toContain('الوردية مغلقة بالفعل.');
  });

  it('shows alreadyClosed on 409 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(409));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Close shift' });
    await userEvent.type(screen.getByLabelText('Actual cash (SAR)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'Close shift' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert').textContent).toContain('The shift is already closed.');
  });

  it('shows notFound on 404 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(404));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    await userEvent.type(screen.getByLabelText('النقدية الفعلية (ر.س)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'إغلاق الوردية' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert').textContent).toContain('لم يتم العثور على الوردية.');
  });

  it('shows notFound on 404 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(404));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Close shift' });
    await userEvent.type(screen.getByLabelText('Actual cash (SAR)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'Close shift' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert').textContent).toContain('The shift was not found.');
  });

  // ── Validation ─────────────────────────────────────────────────

  it('shows errRequired for empty cashier name in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    await userEvent.type(screen.getByLabelText('النقدية الافتتاحية (ر.س)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'فتح الوردية' }));

    expect(screen.getByText('هذا الحقل مطلوب.')).toBeInTheDocument();
    expect(screen.getByLabelText('اسم الكاشير')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  it('shows errRequired for empty cashier name in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Open shift' });
    await userEvent.type(screen.getByLabelText('Starting cash (SAR)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'Open shift' }));

    expect(screen.getByText('This field is required.')).toBeInTheDocument();
    expect(screen.getByLabelText('Cashier name')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  it('shows errMoney for invalid cash in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    await userEvent.type(screen.getByLabelText('اسم الكاشير'), 'سارة');
    await userEvent.type(screen.getByLabelText('النقدية الافتتاحية (ر.س)'), 'abc');
    await userEvent.click(screen.getByRole('button', { name: 'فتح الوردية' }));

    expect(screen.getByText('أدخل مبلغًا صحيحًا (مثل 10.50).')).toBeInTheDocument();
  });

  it('shows errMoney for invalid cash in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Open shift' });
    await userEvent.type(screen.getByLabelText('Cashier name'), 'Sara');
    await userEvent.type(screen.getByLabelText('Starting cash (SAR)'), 'abc');
    await userEvent.click(screen.getByRole('button', { name: 'Open shift' }));

    expect(screen.getByText('Enter a valid amount (e.g. 10.50).')).toBeInTheDocument();
  });

  // ── Open failure (5xx) ─────────────────────────────────────────

  it('shows openError on 5xx in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    const ApiError = class extends Error {
      readonly status: number;
      constructor(status: number) { super(); this.status = status; }
    };
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new ApiError(500));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    await userEvent.type(screen.getByLabelText('اسم الكاشير'), 'سارة');
    await userEvent.type(screen.getByLabelText('النقدية الافتتاحية (ر.س)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'فتح الوردية' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر فتح الوردية. حاول مرة أخرى.');
  });

  it('shows openError on 5xx in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    const ApiError = class extends Error {
      readonly status: number;
      constructor(status: number) { super(); this.status = status; }
    };
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new ApiError(500));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Open shift' });
    await userEvent.type(screen.getByLabelText('Cashier name'), 'Sara');
    await userEvent.type(screen.getByLabelText('Starting cash (SAR)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'Open shift' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The shift could not be opened. Try again.');
  });

  // ── Close failure (5xx) ────────────────────────────────────────

  it('shows closeError on 5xx in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    const ApiError = class extends Error {
      readonly status: number;
      constructor(status: number) { super(); this.status = status; }
    };
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new ApiError(500));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    await userEvent.type(screen.getByLabelText('النقدية الفعلية (ر.س)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'إغلاق الوردية' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر إغلاق الوردية. حاول مرة أخرى.');
  });

  it('shows closeError on 5xx in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    const ApiError = class extends Error {
      readonly status: number;
      constructor(status: number) { super(); this.status = status; }
    };
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new ApiError(500));
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Close shift' });
    await userEvent.type(screen.getByLabelText('Actual cash (SAR)'), '55.00');
    await userEvent.click(screen.getByRole('button', { name: 'Close shift' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The shift could not be closed. Try again.');
  });

  // ── Button disabled while in-flight ─────────────────────────────

  it('disables button and shows opening text in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    // Stub apiPost to never resolve so the button stays disabled
    (client.apiPost as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    await userEvent.type(screen.getByLabelText('اسم الكاشير'), 'سارة');
    await userEvent.type(screen.getByLabelText('النقدية الافتتاحية (ر.س)'), '50.00');
    const button = screen.getByRole('button', { name: 'فتح الوردية' });
    await userEvent.click(button);

    expect(button).toBeDisabled();
    expect(screen.getByText('جارٍ الفتح…')).toBeInTheDocument();
  });

  it('disables button and shows closing text in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([openShift]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'إغلاق الوردية' });
    // Stub apiPatch to never resolve so the button stays disabled
    (client.apiPatch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    const button = screen.getByRole('button', { name: 'إغلاق الوردية' });
    await userEvent.type(screen.getByLabelText('النقدية الفعلية (ر.س)'), '55.00');
    await userEvent.click(button);

    await waitFor(() => expect(button).toBeDisabled());
    expect(screen.getByText('جارٍ الإغلاق…')).toBeInTheDocument();
  });

  // ── Accessibility ──────────────────────────────────────────────

  it('passes axe in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    const { container } = renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'فتح وردية' });
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('passes axe in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    const { container } = renderWithProviders(<ShiftsPage />, { route: '/shifts' });
    await screen.findByRole('heading', { name: 'Open shift' });
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  // ── Labels and aria attributes ─────────────────────────────────

  it('has labels with htmlFor in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'فتح وردية' });
    const nameLabel = screen.getByText('اسم الكاشير');
    expect(nameLabel).toHaveAttribute('for', 'cashier-name');

    const cashLabel = screen.getByText('النقدية الافتتاحية (ر.س)');
    expect(cashLabel).toHaveAttribute('for', 'starting-cash');

    const nameInput = screen.getByLabelText('اسم الكاشير');
    expect(nameInput).not.toHaveAttribute('dir');

    const cashInput = screen.getByLabelText('النقدية الافتتاحية (ر.س)');
    expect(cashInput).toHaveAttribute('dir', 'ltr');
  });

  it('has labels with htmlFor in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderWithProviders(<ShiftsPage />, { route: '/shifts' });

    await screen.findByRole('heading', { name: 'Open shift' });
    const nameLabel = screen.getByText('Cashier name');
    expect(nameLabel).toHaveAttribute('for', 'cashier-name');

    const cashLabel = screen.getByText('Starting cash (SAR)');
    expect(cashLabel).toHaveAttribute('for', 'starting-cash');

    const nameInput = screen.getByLabelText('Cashier name');
    expect(nameInput).not.toHaveAttribute('dir');

    const cashInput = screen.getByLabelText('Starting cash (SAR)');
    expect(cashInput).toHaveAttribute('dir', 'ltr');
  });
});
