import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';

// Mock the API client before importing CustomersPage
vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client');
  return {
    ...actual,
    apiGet: vi.fn(),
    apiPost: vi.fn(),
    apiPatch: vi.fn(),
  };
});

// Mock useCustomers hook
vi.mock('../api/customers', () => ({
  useCustomers: vi.fn(),
}));

import CustomersPage from './CustomersPage';
import * as client from '../api/client';
import * as customersApi from '../api/customers';

const CUSTOMERS = [
  { id: 'c1', name: 'Ahmed Ali', phone: '+966501234567', creditBalanceHalalas: 15000 },
  { id: 'c2', name: 'Sara Mohammed', phone: null, creditBalanceHalalas: -500 },
];

const CREATED_CUSTOMER = { id: 'c3', name: 'New Customer', phone: '+966509876543', creditBalanceHalalas: 0 };

describe('CustomersPage', () => {
  beforeEach(() => {
    signIn('Admin');
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  // ── List rendering ────────────────────────────────────────────────

  it('lists customers in Arabic with credit formatted', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('العملاء')).toBeInTheDocument();
    expect(screen.getByText('Ahmed Ali')).toBeInTheDocument();
    expect(screen.getByText('+966501234567')).toBeInTheDocument();
    expect(screen.getByText('150.00 ر.س')).toBeInTheDocument();
  });

  it('lists customers in English with credit formatted', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('Customers')).toBeInTheDocument();
    expect(screen.getByText('Ahmed Ali')).toBeInTheDocument();
    expect(screen.getByText('+966501234567')).toBeInTheDocument();
    expect(screen.getByText('SAR 150.00')).toBeInTheDocument();
  });

  it('shows empty state in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: [],
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('لا يوجد عملاء بعد.')).toBeInTheDocument();
  });

  it('shows empty state in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: [],
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('No customers yet.')).toBeInTheDocument();
  });

  it('shows error and retry in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: true,
      data: [],
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر تحميل العملاء.');
  });

  it('shows error and retry in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: true,
      data: [],
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByRole('alert')).toHaveTextContent('The customers could not be loaded.');
  });

  // ── Create ────────────────────────────────────────────────────────

  it('submits valid form in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(CREATED_CUSTOMER);
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });
    await userEvent.type(screen.getByLabelText('الاسم'), 'عميل تجريبي');
    await userEvent.type(screen.getByLabelText('الهاتف'), '+966501234567');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم إنشاء العميل.');
    expect(client.apiPost).toHaveBeenCalledWith('/api/customers', {
      name: 'عميل تجريبي',
      phone: '+966501234567',
    });
  });

  it('submits valid form in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(CREATED_CUSTOMER);
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    await userEvent.click(screen.getByRole('button', { name: 'New customer' }));

    await screen.findByRole('heading', { name: 'New customer' });
    await userEvent.type(screen.getByLabelText('Name'), 'Test Customer');
    await userEvent.type(screen.getByLabelText('Phone'), '+966501234567');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await screen.findByText('Customer created.');
    expect(client.apiPost).toHaveBeenCalledWith('/api/customers', {
      name: 'Test Customer',
      phone: '+966501234567',
    });
  });

  it('omits phone when blank on create', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(CREATED_CUSTOMER);
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });
    await userEvent.type(screen.getByLabelText('الاسم'), 'بدون هاتف');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم إنشاء العميل.');
    expect(client.apiPost).toHaveBeenCalledWith('/api/customers', {
      name: 'بدون هاتف',
    });
  });

  // ── Edit ──────────────────────────────────────────────────────────

  it('submits edit with exact PATCH body', async () => {
    const updatedCustomer = { ...CUSTOMERS[0], name: 'Ahmed Updated', phone: '+966509999999' };
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue(updatedCustomer);
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    const editButtons = screen.getAllByRole('button', { name: 'تعديل العميل' });
    await userEvent.click(editButtons[0]);

    await screen.findByRole('heading', { name: 'تعديل العميل' });
    // Pre-filled values
    expect(screen.getByLabelText('الاسم')).toHaveValue('Ahmed Ali');
    expect(screen.getByLabelText('الهاتف')).toHaveValue('+966501234567');

    await userEvent.clear(screen.getByLabelText('الاسم'));
    await userEvent.type(screen.getByLabelText('الاسم'), 'Ahmed Updated');
    await userEvent.clear(screen.getByLabelText('الهاتف'));
    await userEvent.type(screen.getByLabelText('الهاتف'), '+966509999999');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم تحديث العميل.');
    expect(client.apiPatch).toHaveBeenCalledWith('/api/customers/c1', {
      name: 'Ahmed Updated',
      phone: '+966509999999',
    });
  });

  it('shows updated row after edit in English', async () => {
    await i18n.changeLanguage('en');
    const updatedCustomer = { ...CUSTOMERS[0], name: 'Updated Name' };
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue(updatedCustomer);
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    const editButtons = screen.getAllByRole('button', { name: 'Edit customer' });
    await userEvent.click(editButtons[0]);

    await screen.findByRole('heading', { name: 'Edit customer' });
    await userEvent.clear(screen.getByLabelText('Name'));
    await userEvent.type(screen.getByLabelText('Name'), 'Updated Name');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await screen.findByText('Customer updated.');
  });

  // ── Error handling ────────────────────────────────────────────────

  it('shows notFound on 404 in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(404));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });
    await userEvent.type(screen.getByLabelText('الاسم'), 'عميل');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('لم يتم العثور على العميل.');
  });

  it('shows notFound on 404 in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(404));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    await userEvent.click(screen.getByRole('button', { name: 'New customer' }));

    await screen.findByRole('heading', { name: 'New customer' });
    await userEvent.type(screen.getByLabelText('Name'), 'Customer');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The customer was not found.');
  });

  it('shows error on 500 in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });
    await userEvent.type(screen.getByLabelText('الاسم'), 'عميل');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر حفظ التغيير.');
  });

  it('shows error on 500 in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    await userEvent.click(screen.getByRole('button', { name: 'New customer' }));

    await screen.findByRole('heading', { name: 'New customer' });
    await userEvent.type(screen.getByLabelText('Name'), 'Customer');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The change could not be saved.');
  });

  // ── Validation ────────────────────────────────────────────────────

  it('shows errRequired for empty name in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(await screen.findByText('هذا الحقل مطلوب.')).toBeInTheDocument();
    expect(screen.getByLabelText('الاسم')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  it('shows errRequired for empty name in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    await userEvent.click(screen.getByRole('button', { name: 'New customer' }));

    await screen.findByRole('heading', { name: 'New customer' });

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('This field is required.')).toBeInTheDocument();
    expect(screen.getByLabelText('Name')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  // ── Saving state ──────────────────────────────────────────────────

  it('disables button and shows saving text in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    await userEvent.click(screen.getByRole('button', { name: 'عميل جديد' }));

    await screen.findByRole('heading', { name: 'عميل جديد' });
    await userEvent.type(screen.getByLabelText('الاسم'), 'عميل');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByRole('button', { name: 'جارٍ الحفظ…' })).toBeDisabled();
  });

  it('disables button and shows saving text in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    (client.apiPost as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    await userEvent.click(screen.getByRole('button', { name: 'New customer' }));

    await screen.findByRole('heading', { name: 'New customer' });
    await userEvent.type(screen.getByLabelText('Name'), 'Customer');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  });

  // ── Accessibility ─────────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    const container = renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByRole('table');
    expect(await axe(container.container)).toHaveNoViolations();
  });

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    const container = renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByRole('table');
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── Credit balance formatting ─────────────────────────────────────

  it('formats negative credit balance in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('-5.00 ر.س')).toBeInTheDocument();
  });

  it('formats negative credit balance in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    expect(await screen.findByText('SAR -5.00')).toBeInTheDocument();
  });

  // ── Phone null renders empty cell ─────────────────────────────────

  it('shows empty cell for null phone in Arabic', async () => {
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('العملاء');
    const rows = screen.getAllByRole('row');
    // Second row (Sara) should have an empty phone cell
    expect(rows[2].querySelectorAll('td')[1]).toHaveTextContent('');
  });

  it('shows empty cell for null phone in English', async () => {
    await i18n.changeLanguage('en');
    (customersApi.useCustomers as ReturnType<typeof vi.fn>).mockReturnValue({
      isPending: false,
      isError: false,
      data: CUSTOMERS,
      refetch: vi.fn(),
      setData: vi.fn(),
    });
    renderWithProviders(<CustomersPage />, { route: '/customers' });

    await screen.findByText('Customers');
    const rows = screen.getAllByRole('row');
    expect(rows[2].querySelectorAll('td')[1]).toHaveTextContent('');
  });
});
