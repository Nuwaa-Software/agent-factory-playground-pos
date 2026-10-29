import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';

// Mock the API client before importing SettingsPage
vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client');
  return {
    ...actual,
    apiGet: vi.fn(),
    apiPost: vi.fn(),
    apiPatch: vi.fn(),
  };
});

import SettingsPage from './SettingsPage';
import * as client from '../api/client';

const settingsData = {
  taxRatePercent: 15,
  receiptFooterText: 'Thank you for your purchase.',
  lowStockThreshold: 10,
};

describe('SettingsPage', () => {
  beforeEach(() => {
    signIn('Admin');
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  // ── Loading state ──────────────────────────────────────────────

  it('shows loading in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  it('shows loading in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    expect(screen.getByRole('status')).toHaveTextContent('Loading…');
  });

  // ── Load error ─────────────────────────────────────────────────

  it('shows load error in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('fail'));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    await waitFor(() => screen.getByRole('alert'), { timeout: 5000 });
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ الإعدادات.');
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();
  });

  it('shows load error in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('fail'));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    await waitFor(() => screen.getByRole('alert'), { timeout: 5000 });
    expect(screen.getByRole('alert')).toHaveTextContent('The settings could not be saved.');
    expect(screen.getByText('Try again')).toBeInTheDocument();
  });

  // ── GET 403 on load (Cashier) ────────────────────────────────

  it('shows forbidden on GET 403 in Arabic', async () => {
    signIn('Cashier');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(403));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    await waitFor(() => screen.getByRole('alert'), { timeout: 5000 });
    expect(screen.getByRole('alert')).toHaveTextContent('ليس لديك صلاحية تغيير الإعدادات.');
  });

  it('shows forbidden on GET 403 in English', async () => {
    signIn('Cashier');
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(403));
    renderWithProviders(<SettingsPage />, { route: '/settings' });
    await waitFor(() => screen.getByRole('alert'), { timeout: 5000 });
    expect(screen.getByRole('alert')).toHaveTextContent('You do not have permission to change settings.');
  });

  // ── Pre-filled fields in Arabic ───────────────────────────────

  it('pre-fills fields in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    expect(screen.getByLabelText('معدل الضريبة (%)')).toHaveValue(15);
    expect(screen.getByLabelText('حد المخزون المنخفض')).toHaveValue(10);
    expect(screen.getByLabelText('تذييل الإيصال')).toHaveValue('Thank you for your purchase.');
  });

  // ── Pre-filled fields in English ──────────────────────────────

  it('pre-fills fields in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    expect(screen.getByLabelText('Tax rate (%)')).toHaveValue(15);
    expect(screen.getByLabelText('Low-stock threshold')).toHaveValue(10);
    expect(screen.getByLabelText('Receipt footer')).toHaveValue('Thank you for your purchase.');
  });

  // ── Edit one field, Save in Arabic ────────────────────────────

  it('saves changed fields only in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    // Change only tax rate
    const taxInput = screen.getByLabelText('معدل الضريبة (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم حفظ الإعدادات.');
    expect(client.apiPatch).toHaveBeenCalledWith(
      '/api/company/settings',
      { taxRatePercent: 20 },
    );
    expect(screen.getByRole('status')).toHaveTextContent('تم حفظ الإعدادات.');
  });

  // ── Edit one field, Save in English ───────────────────────────

  it('saves changed fields only in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    // Change only low stock threshold
    const stockInput = screen.getByLabelText('Low-stock threshold');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '50');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await screen.findByText('Settings saved.');
    expect(client.apiPatch).toHaveBeenCalledWith(
      '/api/company/settings',
      { lowStockThreshold: 50 },
    );
    expect(screen.getByRole('status')).toHaveTextContent('Settings saved.');
  });

  // ── PATCH 403 in Arabic ──────────────────────────────────────

  it('shows forbidden on PATCH 403 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(403));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const taxInput = screen.getByLabelText('معدل الضريبة (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('ليس لديك صلاحية تغيير الإعدادات.');
  });

  // ── PATCH 403 in English ─────────────────────────────────────

  it('shows forbidden on PATCH 403 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(403));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const taxInput = screen.getByLabelText('Tax rate (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('You do not have permission to change settings.');
  });

  // ── PATCH 400 in Arabic ──────────────────────────────────────

  it('shows error on PATCH 400 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(400));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const taxInput = screen.getByLabelText('معدل الضريبة (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ الإعدادات.');
  });

  // ── PATCH 400 in English ─────────────────────────────────────

  it('shows error on PATCH 400 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(400));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const taxInput = screen.getByLabelText('Tax rate (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The settings could not be saved.');
  });

  // ── Footer has maxLength=500 and hint ────────────────────────

  it('footer input has maxLength=500 and hint visible', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const footerInput = screen.getByLabelText('تذييل الإيصال');
    expect(footerInput).toHaveAttribute('maxlength', '500');
    expect(screen.getByText('الحد الأقصى 500 حرف.')).toBeInTheDocument();
  });

  it('footer input has maxLength=500 and hint visible in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const footerInput = screen.getByLabelText('Receipt footer');
    expect(footerInput).toHaveAttribute('maxlength', '500');
    expect(screen.getByText('Maximum 500 characters.')).toBeInTheDocument();
  });

  // ── Labels present in Arabic ─────────────────────────────────

  it('has labels in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    expect(screen.getByLabelText('معدل الضريبة (%)')).toBeInTheDocument();
    expect(screen.getByLabelText('حد المخزون المنخفض')).toBeInTheDocument();
    expect(screen.getByLabelText('تذييل الإيصال')).toBeInTheDocument();
  });

  // ── Labels present in English ────────────────────────────────

  it('has labels in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    expect(screen.getByLabelText('Tax rate (%)')).toBeInTheDocument();
    expect(screen.getByLabelText('Low-stock threshold')).toBeInTheDocument();
    expect(screen.getByLabelText('Receipt footer')).toBeInTheDocument();
  });

  // ── Button disabled while saving in Arabic ───────────────────

  it('disables button and shows saving text in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const taxInput = screen.getByLabelText('معدل الضريبة (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByRole('button', { name: 'جارٍ الحفظ…' })).toBeDisabled();
  });

  // ── Button disabled while saving in English ──────────────────

  it('disables button and shows saving text in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    (client.apiPatch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const taxInput = screen.getByLabelText('Tax rate (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '20');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  });

  // ── axe in Arabic ────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    const container = renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── axe in English ───────────────────────────────────────────

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    const container = renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── Tax rate validation in Arabic ────────────────────────────

  it('shows validation error for tax rate > 100 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const taxInput = screen.getByLabelText('معدل الضريبة (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '150');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('أدخل مبلغًا صحيحًا (مثل 10.50).')).toBeInTheDocument();
    expect(screen.getByLabelText('معدل الضريبة (%)')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Tax rate validation in English ───────────────────────────

  it('shows validation error for tax rate > 100 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const taxInput = screen.getByLabelText('Tax rate (%)');
    await userEvent.clear(taxInput);
    await userEvent.type(taxInput, '150');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('Enter a valid amount (e.g. 10.50).')).toBeInTheDocument();
    expect(screen.getByLabelText('Tax rate (%)')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Low stock validation in Arabic ───────────────────────────

  it('shows validation error for low stock > 10000 in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const stockInput = screen.getByLabelText('حد المخزون المنخفض');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '20000');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('أدخل عددًا صحيحًا موجبًا.')).toBeInTheDocument();
    expect(screen.getByLabelText('حد المخزون المنخفض')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Low stock validation in English ──────────────────────────

  it('shows validation error for low stock > 10000 in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const stockInput = screen.getByLabelText('Low-stock threshold');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '20000');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('Enter a whole non-negative number.')).toBeInTheDocument();
    expect(screen.getByLabelText('Low-stock threshold')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Footer max length validation in Arabic ───────────────────

  it('shows footerMax error when footer > 500 chars in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'إعدادات الشركة' });
    const footerInput = screen.getByLabelText('تذييل الإيصال');
    // Use fireEvent.change to bypass maxLength enforcement in jsdom
    await userEvent.type(footerInput, 'a'.repeat(500));
    // Now use fireEvent to set 501 chars (bypasses maxLength)
    const { fireEvent } = await import('@testing-library/react');
    fireEvent.change(footerInput, { target: { value: 'a'.repeat(501) } });

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(footerInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Footer max length validation in English ──────────────────

  it('shows footerMax error when footer > 500 chars in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(settingsData);
    renderWithProviders(<SettingsPage />, { route: '/settings' });

    await screen.findByRole('heading', { name: 'Company settings' });
    const footerInput = screen.getByLabelText('Receipt footer');
    // Use fireEvent.change to bypass maxLength enforcement in jsdom
    await userEvent.type(footerInput, 'a'.repeat(500));
    // Now use fireEvent to set 501 chars (bypasses maxLength)
    const { fireEvent } = await import('@testing-library/react');
    fireEvent.change(footerInput, { target: { value: 'a'.repeat(501) } });

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(footerInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });
});
