import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';

// Mock the API client before importing ProductCreatePage
vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client');
  return {
    ...actual,
    apiGet: vi.fn(),
    apiPost: vi.fn(),
    apiPatch: vi.fn(),
  };
});

import ProductCreatePage from './ProductCreatePage';
import * as client from '../api/client';

const categories = [
  { id: 'cat-1', nameEn: 'Electronics', nameAr: 'إلكترونيات' },
  { id: 'cat-2', nameEn: 'Food', nameAr: 'طعام' },
];

const createdProduct = {
  id: 'prod-1',
  nameEn: 'Test Product',
  nameAr: 'منتج تجريبي',
  sku: 'ABC-123',
  priceHalalas: 5000,
  stock: 100,
};

describe('ProductCreatePage', () => {
  beforeEach(() => {
    signIn('Admin');
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  // ── Loading / categories ───────────────────────────────────────

  it('loads categories in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });
    await screen.findByRole('heading', { name: 'منتج جديد' });
  });

  // ── Valid submit in Arabic ─────────────────────────────────────

  it('submits valid form in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج تجريبي');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Test Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '50.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '100');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم إنشاء المنتج.');
    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/inventory/products',
      {
        nameEn: 'منتج تجريبي',
        nameAr: 'Test Product',
        sku: 'ABC-123',
        priceHalalas: 5000,
        stock: 100,
      },
    );
  });

  // ── Valid submit in English ────────────────────────────────────

  it('submits valid form in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Test Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج تجريبي');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '50.00');
    await userEvent.type(screen.getByLabelText('Stock'), '100');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await screen.findByText('Product created.');
    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/inventory/products',
      {
        nameEn: 'Test Product',
        nameAr: 'منتج تجريبي',
        sku: 'ABC-123',
        priceHalalas: 5000,
        stock: 100,
      },
    );
  });

  // ── SKU validation in Arabic ───────────────────────────────────

  it('shows errSku for invalid SKU in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'abc');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('استخدم أحرفًا إنجليزية كبيرة وأرقامًا وشرطات فقط (مثل ABC-123).')).toBeInTheDocument();
    expect(screen.getByLabelText('رمز المنتج (SKU)')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  // ── SKU validation in English ──────────────────────────────────

  it('shows errSku for invalid SKU in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'abc');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '10.00');
    await userEvent.type(screen.getByLabelText('Stock'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('Use uppercase letters, numbers and dashes only (e.g. ABC-123).')).toBeInTheDocument();
    expect(screen.getByLabelText('SKU')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  // ── Empty required field in Arabic ─────────────────────────────

  it('shows errRequired for empty nameEn in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('هذا الحقل مطلوب.')).toBeInTheDocument();
    expect(screen.getByLabelText('الاسم (إنجليزي)')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  // ── Empty required field in English ────────────────────────────

  it('shows errRequired for empty nameEn in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '10.00');
    await userEvent.type(screen.getByLabelText('Stock'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('This field is required.')).toBeInTheDocument();
    expect(screen.getByLabelText('Name (English)')).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPost).not.toHaveBeenCalled();
  });

  // ── Submit failure in Arabic ───────────────────────────────────

  it('shows saveError on 5xx in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ المنتج. حاول مرة أخرى.');
  });

  // ── Submit failure in English ──────────────────────────────────

  it('shows saveError on 5xx in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(400));
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '10.00');
    await userEvent.type(screen.getByLabelText('Stock'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The product could not be saved. Try again.');
  });

  // ── Button disabled while saving in Arabic ─────────────────────

  it('disables button and shows saving text in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByRole('button', { name: 'جارٍ الحفظ…' })).toBeDisabled();
  });

  // ── Button disabled while saving in English ────────────────────

  it('disables button and shows saving text in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '10.00');
    await userEvent.type(screen.getByLabelText('Stock'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  });

  // ── Labels and dir attributes in Arabic ────────────────────────

  it('has correct labels and dir attributes in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    expect(screen.getByLabelText('الاسم (إنجليزي)')).toBeInTheDocument();
    expect(screen.getByLabelText('الاسم (عربي)')).toHaveAttribute('dir', 'rtl');
    expect(screen.getByLabelText('رمز المنتج (SKU)')).toHaveAttribute('dir', 'ltr');
    expect(screen.getByLabelText('السعر (ر.س)')).toHaveAttribute('dir', 'ltr');
    expect(screen.getByLabelText('المخزون')).toHaveAttribute('dir', 'ltr');
  });

  // ── Labels and dir attributes in English ───────────────────────

  it('has correct labels and dir attributes in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    expect(screen.getByLabelText('Name (English)')).toBeInTheDocument();
    expect(screen.getByLabelText('Name (Arabic)')).toHaveAttribute('dir', 'rtl');
    expect(screen.getByLabelText('SKU')).toHaveAttribute('dir', 'ltr');
    expect(screen.getByLabelText('Price (SAR)')).toHaveAttribute('dir', 'ltr');
    expect(screen.getByLabelText('Stock')).toHaveAttribute('dir', 'ltr');
  });

  // ── Category select in Arabic ──────────────────────────────────

  it('shows category options in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    const select = screen.getByLabelText('التصنيف');
    expect(select).toBeInTheDocument();
    expect(screen.getByText('بدون تصنيف')).toBeInTheDocument();
    expect(screen.getByText('إلكترونيات')).toBeInTheDocument();
  });

  // ── Category select in English ─────────────────────────────────

  it('shows category options in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    const select = screen.getByLabelText('Category');
    expect(select).toBeInTheDocument();
    expect(screen.getByText('No category')).toBeInTheDocument();
    expect(screen.getByText('Electronics')).toBeInTheDocument();
  });

  // ── Success state in Arabic ────────────────────────────────────

  it('shows success with product summary in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '50.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '100');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    await screen.findByText('تم إنشاء المنتج.');
    expect(screen.getByRole('status')).toHaveTextContent('تم إنشاء المنتج.');
    expect(screen.getByText('العودة للقائمة')).toBeInTheDocument();
    expect(screen.getByText('منتج جديد')).toBeInTheDocument();
  });

  // ── Success state in English ───────────────────────────────────

  it('shows success with product summary in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '50.00');
    await userEvent.type(screen.getByLabelText('Stock'), '100');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await screen.findByText('Product created.');
    expect(screen.getByRole('status')).toHaveTextContent('Product created.');
    expect(screen.getByText('Back to list')).toBeInTheDocument();
    expect(screen.getByText('New product')).toBeInTheDocument();
  });

  // ── axe in Arabic ──────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    const container = renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── axe in English ─────────────────────────────────────────────

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    const container = renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── Price validation in Arabic ─────────────────────────────────

  it('shows errMoney for invalid price in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), 'abc');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('أدخل مبلغًا صحيحًا (مثل 10.50).')).toBeInTheDocument();
  });

  // ── Price validation in English ────────────────────────────────

  it('shows errMoney for invalid price in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), 'abc');
    await userEvent.type(screen.getByLabelText('Stock'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('Enter a valid amount (e.g. 10.50).')).toBeInTheDocument();
  });

  // ── Stock validation in Arabic ─────────────────────────────────

  it('shows errStock for invalid stock in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '-5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(screen.getByText('أدخل عددًا صحيحًا موجبًا.')).toBeInTheDocument();
  });

  // ── Stock validation in English ────────────────────────────────

  it('shows errStock for invalid stock in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'New product' });
    await userEvent.type(screen.getByLabelText('Name (English)'), 'Product');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'منتج');
    await userEvent.type(screen.getByLabelText('SKU'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('Price (SAR)'), '10.00');
    await userEvent.type(screen.getByLabelText('Stock'), '-5');

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByText('Enter a whole non-negative number.')).toBeInTheDocument();
  });

  // ── No categoryId when none selected in Arabic ─────────────────

  it('does not send categoryId when none selected in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/inventory/products',
      expect.objectContaining({ categoryId: undefined }),
    );
  });

  // ── With category selected in Arabic ───────────────────────────

  it('sends categoryId when selected in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(categories);
    (client.apiPost as ReturnType<typeof vi.fn>).mockResolvedValue(createdProduct);
    renderWithProviders(<ProductCreatePage />, { route: '/products/new' });

    await screen.findByRole('heading', { name: 'منتج جديد' });
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'منتج');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'Product');
    await userEvent.type(screen.getByLabelText('رمز المنتج (SKU)'), 'ABC-123');
    await userEvent.type(screen.getByLabelText('السعر (ر.س)'), '10.00');
    await userEvent.type(screen.getByLabelText('المخزون'), '5');

    const select = screen.getByLabelText('التصنيف') as HTMLSelectElement;
    select.value = 'cat-1';
    select.dispatchEvent(new Event('change', { bubbles: true }));

    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }));

    expect(client.apiPost).toHaveBeenCalledWith(
      '/api/inventory/products',
      expect.objectContaining({ categoryId: 'cat-1' }),
    );
  });
});
