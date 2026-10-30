import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import { renderWithProviders, signIn } from '../test/render';

// Mock the API client before importing ProductEditPage
vi.mock('../api/client', async () => {
  const actual = await vi.importActual<typeof import('../api/client')>('../api/client');
  return {
    ...actual,
    apiGet: vi.fn(),
    apiPatch: vi.fn(),
  };
});

import ProductEditPage from './ProductEditPage';
import * as client from '../api/client';
import { Routes, Route } from 'react-router-dom';

const products = [
  { id: 'p1', nameEn: 'Test Product', nameAr: 'منتج تجريبي', sku: 'ABC-123', priceHalalas: 5000, stock: 100 },
];

const categories = [
  { id: 'c1', nameEn: 'Electronics', nameAr: 'إلكترونيات' },
  { id: 'c2', nameEn: 'Food', nameAr: 'طعام' },
];

function renderEditPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/products/:id/edit" element={<ProductEditPage />} />
    </Routes>,
    { route: '/products/p1/edit' },
  );
}

describe('ProductEditPage', () => {
  beforeEach(() => {
    signIn('Admin');
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  // ── Loading state in Arabic ────────────────────────────────────

  it('shows loading state in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderEditPage();
    expect(screen.getByRole('status')).toHaveTextContent('جارٍ التحميل…');
  });

  // ── Loading state in English ───────────────────────────────────

  it('shows loading state in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderEditPage();
    expect(screen.getByRole('status')).toHaveTextContent('Loading…');
  });

  // ── Not-found: id not in list in Arabic ────────────────────────

  it('shows loadError when product not found (id not in list) in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderWithProviders(
      <Routes>
        <Route path="/products/:id/edit" element={<ProductEditPage />} />
      </Routes>,
      { route: '/products/nonexistent/edit' },
    );
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات.');
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();
  });

  // ── Not-found: id not in list in English ───────────────────────

  it('shows loadError when product not found (id not in list) in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderWithProviders(
      <Routes>
        <Route path="/products/:id/edit" element={<ProductEditPage />} />
      </Routes>,
      { route: '/products/nonexistent/edit' },
    );
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The products could not be loaded.');
    expect(screen.getByText('Try again')).toBeInTheDocument();
  });

  // ── Not-found: list fetch fails in Arabic ──────────────────────

  it('shows loadError when list fetch fails in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات.');
    expect(screen.getByText('إعادة المحاولة')).toBeInTheDocument();
  });

  // ── Not-found: list fetch fails in English ─────────────────────

  it('shows loadError when list fetch fails in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The products could not be loaded.');
    expect(screen.getByText('Try again')).toBeInTheDocument();
  });

  // ── Retry button navigates to /products in Arabic ──────────────

  it('retry button navigates to /products in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByRole('alert'));
    const retryButton = screen.getByText('إعادة المحاولة');
    await userEvent.click(retryButton);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  // ── Retry button navigates to /products in English ─────────────

  it('retry button navigates to /products in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByRole('alert'));
    const retryButton = screen.getByText('Try again');
    await userEvent.click(retryButton);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  // ── Found: product info in Arabic ──────────────────────────────

  it('shows product info as read-only text in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('ABC-123')).toBeInTheDocument();
  });

  // ── Found: product info in English ─────────────────────────────

  it('shows product info as read-only text in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('ABC-123')).toBeInTheDocument();
  });

  // ── Valid submit → apiPatch called with correct body in Arabic ─

  it('valid submit sends apiPatch with correct body in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    // Change price and stock
    const priceInput = screen.getByLabelText(/السعر/);
    const stockInput = screen.getByLabelText(/المخزون/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    // Submit
    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const callArgs = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(callArgs[0]).toBe('/api/inventory/products/p1');
    const body = callArgs[1] as Record<string, unknown>;
    expect(body).toEqual({ id: 'p1', priceHalalas: 5500, stock: 200 });
    // Verify no nameEn/sku in body
    expect(body).not.toHaveProperty('nameEn');
    expect(body).not.toHaveProperty('sku');

    // Success state
    await waitFor(() => screen.getByRole('status'));
    expect(screen.getByRole('status')).toHaveTextContent('تم تحديث المنتج.');
    expect(screen.getByText('العودة للقائمة')).toBeInTheDocument();
  });

  // ── Valid submit → apiPatch called with correct body in English ─

  it('valid submit sends apiPatch with correct body in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const priceInput = screen.getByLabelText(/Price/);
    const stockInput = screen.getByLabelText(/Stock/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const callArgs = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(callArgs[0]).toBe('/api/inventory/products/p1');
    const body = callArgs[1] as Record<string, unknown>;
    expect(body).toEqual({ id: 'p1', priceHalalas: 5500, stock: 200 });
    expect(body).not.toHaveProperty('nameEn');
    expect(body).not.toHaveProperty('sku');

    await waitFor(() => screen.getByRole('status'));
    expect(screen.getByRole('status')).toHaveTextContent('Product updated.');
    expect(screen.getByText('Back to list')).toBeInTheDocument();
  });

  // ── Invalid price shows error in Arabic ────────────────────────

  it('invalid price shows errMoney with aria-invalid in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const priceInput = screen.getByLabelText(/السعر/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '-5');

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    expect(screen.getByText('أدخل مبلغًا صحيحًا (مثل 10.50).')).toBeInTheDocument();
    expect(priceInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Invalid price shows error in English ───────────────────────

  it('invalid price shows errMoney with aria-invalid in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const priceInput = screen.getByLabelText(/Price/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '-5');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    expect(screen.getByText('Enter a valid amount (e.g. 10.50).')).toBeInTheDocument();
    expect(priceInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Invalid stock shows error in Arabic ────────────────────────

  it('invalid stock shows errStock with aria-invalid in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const stockInput = screen.getByLabelText(/المخزون/);
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '-1');

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    expect(screen.getByText('أدخل عددًا صحيحًا موجبًا.')).toBeInTheDocument();
    expect(stockInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Invalid stock shows error in English ───────────────────────

  it('invalid stock shows errStock with aria-invalid in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const stockInput = screen.getByLabelText(/Stock/);
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '-1');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    expect(screen.getByText('Enter a whole non-negative number.')).toBeInTheDocument();
    expect(stockInput).toHaveAttribute('aria-invalid', 'true');
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Submit failure shows error in Arabic ───────────────────────

  it('submit failure shows saveError in Arabic, inputs keep values', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const priceInput = screen.getByLabelText(/السعر/);
    const stockInput = screen.getByLabelText(/المخزون/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ المنتج. حاول مرة أخرى.');
    expect(priceInput).toHaveValue('55.00');
    expect(stockInput).toHaveValue('200');
  });

  // ── Submit failure shows error in English ──────────────────────

  it('submit failure shows saveError in English, inputs keep values', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockRejectedValue(new client.ApiError(500));
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const priceInput = screen.getByLabelText(/Price/);
    const stockInput = screen.getByLabelText(/Stock/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The product could not be saved. Try again.');
    expect(priceInput).toHaveValue('55.00');
    expect(stockInput).toHaveValue('200');
  });

  // ── "No category" omits categoryId in Arabic ───────────────────

  it('"No category" omits categoryId in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const categorySelect = screen.getByLabelText(/التصنيف/);
    // Default is "بدون تصنيف" (empty value)
    expect(categorySelect).toHaveValue('');

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const body = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0][1] as Record<string, unknown>;
    expect(body.categoryId).toBeUndefined();
  });

  // ── "No category" omits categoryId in English ──────────────────

  it('"No category" omits categoryId in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const categorySelect = screen.getByLabelText(/Category/);
    expect(categorySelect).toHaveValue('');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const body = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0][1] as Record<string, unknown>;
    expect(body.categoryId).toBeUndefined();
  });

  // ── Selecting a category sends its id in Arabic ────────────────

  it('selecting a category sends its id in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const categorySelect = screen.getByLabelText(/التصنيف/);
    await userEvent.selectOptions(categorySelect, ['c1']);

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const body = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0][1] as Record<string, unknown>;
    expect(body.categoryId).toBe('c1');
  });

  // ── Selecting a category sends its id in English ───────────────

  it('selecting a category sends its id in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'p1' });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const categorySelect = screen.getByLabelText(/Category/);
    await userEvent.selectOptions(categorySelect, ['c2']);

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    await waitFor(() => expect(client.apiPatch).toHaveBeenCalled());
    const body = (client.apiPatch as ReturnType<typeof vi.fn>).mock.calls[0][1] as Record<string, unknown>;
    expect(body.categoryId).toBe('c2');
  });

  // ── Structural: labels present in Arabic ───────────────────────

  it('has labels for all fields in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    expect(screen.getByLabelText(/السعر/)).toBeInTheDocument();
    expect(screen.getByLabelText(/المخزون/)).toBeInTheDocument();
    expect(screen.getByLabelText(/التصنيف/)).toBeInTheDocument();
  });

  // ── Structural: labels present in English ──────────────────────

  it('has labels for all fields in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    expect(screen.getByLabelText(/Price/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Stock/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Category/)).toBeInTheDocument();
  });

  // ── Structural: price and stock have dir="ltr" in Arabic ───────

  it('price and stock inputs have dir="ltr" in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const priceInput = screen.getByLabelText(/السعر/);
    const stockInput = screen.getByLabelText(/المخزون/);
    expect(priceInput).toHaveAttribute('dir', 'ltr');
    expect(stockInput).toHaveAttribute('dir', 'ltr');
  });

  // ── Structural: price and stock have dir="ltr" in English ──────

  it('price and stock inputs have dir="ltr" in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const priceInput = screen.getByLabelText(/Price/);
    const stockInput = screen.getByLabelText(/Stock/);
    expect(priceInput).toHaveAttribute('dir', 'ltr');
    expect(stockInput).toHaveAttribute('dir', 'ltr');
  });

  // ── Structural: category is a select with "No category" option in Arabic ──

  it('category is a labelled select with empty option in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const categorySelect = screen.getByRole('combobox');
    expect(categorySelect).toBeInTheDocument();
    expect(screen.getByText('بدون تصنيف')).toBeInTheDocument();
  });

  // ── Structural: category is a select with "No category" option in English ──

  it('category is a labelled select with empty option in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const categorySelect = screen.getByRole('combobox');
    expect(categorySelect).toBeInTheDocument();
    expect(screen.getByText('No category')).toBeInTheDocument();
  });

  // ── Structural: submit is teal primary and disabled while saving in Arabic ──

  it('submit is teal primary and shows saving while in flight in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));

    const priceInput = screen.getByLabelText(/السعر/);
    const stockInput = screen.getByLabelText(/المخزون/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    const submitBtn = screen.getByRole('button', { name: /حفظ/ });
    await userEvent.click(submitBtn);

    // Button should be disabled and show saving text
    expect(screen.getByRole('button', { name: /جارٍ الحفظ…/ })).toBeDisabled();
  });

  // ── Structural: submit is teal primary and disabled while saving in English ──

  it('submit is teal primary and shows saving while in flight in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    (client.apiPatch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));

    const priceInput = screen.getByLabelText(/Price/);
    const stockInput = screen.getByLabelText(/Stock/);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '55.00');
    await userEvent.clear(stockInput);
    await userEvent.type(stockInput, '200');

    const submitBtn = screen.getByRole('button', { name: /Save/ });
    await userEvent.click(submitBtn);

    expect(screen.getByRole('button', { name: /Saving…/ })).toBeDisabled();
  });

  // ── axe in Arabic ──────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    const container = renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── axe in English ─────────────────────────────────────────────

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    const container = renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── Categories failure does not break the page in Arabic ───────

  it('categories rejection does not break the page in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async () => {
      throw new client.ApiError(500);
    });
    renderEditPage();
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات.');
  });

  // ── Does not call apiPatch on initial render in Arabic ─────────

  it('does not call apiPatch on initial render in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('لا يمكن تعديل الاسم أو الرمز.'));
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── Does not call apiPatch on initial render in English ────────

  it('does not call apiPatch on initial render in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async (url: string) => {
      if (url === '/api/inventory/products') return products;
      return categories;
    });
    renderEditPage();
    await waitFor(() => screen.getByText('Name and SKU cannot be changed.'));
    expect(client.apiPatch).not.toHaveBeenCalled();
  });
});
