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
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('ABC-123')).toBeInTheDocument();
    expect(screen.getByText('لا يمكن تعديل الاسم أو الرمز.')).toBeInTheDocument();
  });

  // ── Found: product info in English ─────────────────────────────

  it('shows product info as read-only text in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(screen.getByText('منتج تجريبي')).toBeInTheDocument();
    expect(screen.getByText('ABC-123')).toBeInTheDocument();
    expect(screen.getByText('Name and SKU cannot be changed.')).toBeInTheDocument();
  });

  // ── No textbox role (no inputs) in Arabic ──────────────────────

  it('has no textbox role in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
  });

  // ── No textbox role (no inputs) in English ─────────────────────

  it('has no textbox role in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
  });

  // ── No PATCH calls in Arabic ───────────────────────────────────

  it('does not call apiPatch in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── No PATCH calls in English ──────────────────────────────────

  it('does not call apiPatch in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(client.apiPatch).not.toHaveBeenCalled();
  });

  // ── No form or save button in Arabic ───────────────────────────

  it('has no form or save button in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(screen.queryByRole('form')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'حفظ' })).not.toBeInTheDocument();
  });

  // ── No form or save button in English ──────────────────────────

  it('has no form or save button in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(screen.queryByRole('form')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });

  // ── Back to list button in Arabic ──────────────────────────────

  it('shows back to list button in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(screen.getByText('العودة للقائمة')).toBeInTheDocument();
  });

  // ── Back to list button in English ─────────────────────────────

  it('shows back to list button in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(screen.getByText('Back to list')).toBeInTheDocument();
  });

  // ── Categories failure does not break the page in Arabic ───────

  it('categories rejection does not break the page in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockImplementation(async () => {
      throw new client.ApiError(500);
    });
    renderEditPage();
    // Should still show loading, then error for products list
    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات.');
  });

  // ── axe in Arabic ──────────────────────────────────────────────

  it('axe passes in Arabic', async () => {
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    const container = renderEditPage();
    await waitFor(() => screen.getByText('منتج تجريبي'));
    expect(await axe(container.container)).toHaveNoViolations();
  });

  // ── axe in English ─────────────────────────────────────────────

  it('axe passes in English', async () => {
    await i18n.changeLanguage('en');
    (client.apiGet as ReturnType<typeof vi.fn>).mockResolvedValue(products);
    const container = renderEditPage();
    await waitFor(() => screen.getByText('Test Product'));
    expect(await axe(container.container)).toHaveNoViolations();
  });
});
