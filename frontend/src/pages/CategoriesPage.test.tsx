import { screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from '../test/axe';
import i18n from '../i18n';
import type { Category } from '../api/categories';
import { COMPANY_ID, renderWithProviders, signIn, stubFetch } from '../test/render';
import CategoriesPage from './CategoriesPage';

const CATEGORIES: Category[] = [
  { id: 'c1', nameEn: 'Beverages', nameAr: 'المشروبات', description: 'Drinks and beverages', parentId: null },
  { id: 'c2', nameEn: 'Coffee', nameAr: 'القهوة', description: 'Coffee products', parentId: 'c1' },
];

describe('CategoriesPage', () => {
  beforeEach(() => signIn('Viewer'));

  it('lists the categories in Arabic with parent names', async () => {
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);
    const coffeeRow = (await screen.findByText('القهوة')).closest('tr')!;
    expect(within(coffeeRow).getByText('المشروبات')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الوصف' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الاسم' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'الفئة الأم' })).toBeInTheDocument();
  });

  it('shows no-parent label for categories without a parent', async () => {
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);
    const table = await screen.findByRole('table');
    expect(within(table).getByText('— لا يوجد —')).toBeInTheDocument();
  });

  it('lists the categories in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);
    expect(await screen.findByText('Coffee')).toBeInTheDocument();
    expect(screen.getByText('Drinks and beverages')).toBeInTheDocument();
    expect(screen.queryByText('القهوة')).not.toBeInTheDocument();
  });

  it('shows no-parent label in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);
    const table = await screen.findByRole('table');
    expect(within(table).getByText('— none —')).toBeInTheDocument();
  });

  it('asks the API as the signed-in company and role', async () => {
    signIn('Viewer');
    const fetchMock = stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);
    await screen.findByText('القهوة');
    expect(fetchMock).toHaveBeenCalledWith('/api/inventory/categories', {
      headers: { Accept: 'application/json', 'X-Company-Id': COMPANY_ID, 'X-Role': 'Viewer' },
    });
  });

  it('says so when there are no categories', async () => {
    stubFetch([]);
    renderWithProviders(<CategoriesPage />);
    expect(await screen.findByText('لا توجد فئات بعد.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows an error and loads again on retry', async () => {
    const fetchMock = stubFetch({ title: 'boom' }, 500);
    renderWithProviders(<CategoriesPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر تحميل الفئات.');
    fetchMock.mockImplementation(async () => new Response(JSON.stringify(CATEGORIES), { status: 200 }));
    await userEvent.click(screen.getByRole('button', { name: 'إعادة المحاولة' }));
    expect(await screen.findByText('القهوة')).toBeInTheDocument();
  });

  it.each(['ar', 'en'])('has no accessibility violations (%s)', async (lang) => {
    await i18n.changeLanguage(lang);
    stubFetch(CATEGORIES);
    const { container } = renderWithProviders(<CategoriesPage />);
    await screen.findByRole('table');
    expect(await axe(container)).toHaveNoViolations();
  });

  // ── Create tests (use mocked apiPost) ───────────────────────────────

  beforeEach(() => {
    signIn('Admin');
    vi.clearAllMocks();
    i18n.changeLanguage('ar');
  });

  it('creates a category successfully in Arabic', async () => {
    stubFetch(CATEGORIES);
    const createdCategory: Category = { id: 'c3', nameEn: 'Tea', nameAr: 'الشاي', description: null, parentId: null };
    const apiPostSpy = vi.spyOn(await import('../api/client'), 'apiPost').mockResolvedValue(createdCategory);

    renderWithProviders(<CategoriesPage />);

    // Click Create button
    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    // Fill form
    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'Tea');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'الشاي');

    // Submit
    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    // Assert apiPost called with correct body (no description/parentId)
    expect(apiPostSpy).toHaveBeenCalledWith(
      '/api/inventory/categories',
      { nameEn: 'Tea', nameAr: 'الشاي' },
    );

    // Assert new row appears and success message shown
    await screen.findByText('الشاي');
    expect(screen.getByRole('status')).toHaveTextContent('تم إنشاء الفئة.');
  });

  it('creates a category with parent selected in Arabic', async () => {
    stubFetch(CATEGORIES);
    const createdCategory: Category = { id: 'c3', nameEn: 'Tea', nameAr: 'الشاي', description: 'A drink', parentId: 'c1' };
    const apiPostSpy = vi.spyOn(await import('../api/client'), 'apiPost').mockResolvedValue(createdCategory);

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'Tea');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'الشاي');
    await userEvent.type(screen.getByLabelText('الوصف'), 'A drink');

    // Select parent
    const select = screen.getByLabelText('الفئة الأم');
    await userEvent.selectOptions(select, 'c1');

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    expect(apiPostSpy).toHaveBeenCalledWith(
      '/api/inventory/categories',
      { nameEn: 'Tea', nameAr: 'الشاي', description: 'A drink', parentId: 'c1' },
    );
  });

  it('shows validation error for empty nameEn in Arabic', async () => {
    stubFetch(CATEGORIES);
    const apiPostSpy = vi.spyOn(await import('../api/client'), 'apiPost').mockResolvedValue({} as Category);

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    // Fill only nameAr
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'الشاي');

    // Submit with empty nameEn
    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    expect(screen.getByText('هذا الحقل مطلوب.')).toBeInTheDocument();
    expect(screen.getByLabelText('الاسم (إنجليزي)')).toHaveAttribute('aria-invalid', 'true');
    expect(apiPostSpy).not.toHaveBeenCalled();
  });

  it('shows validation error for empty nameEn in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    const apiPostSpy = vi.spyOn(await import('../api/client'), 'apiPost').mockResolvedValue({} as Category);

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'Tea');

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    expect(screen.getByText('This field is required.')).toBeInTheDocument();
    expect(screen.getByLabelText('Name (English)')).toHaveAttribute('aria-invalid', 'true');
    expect(apiPostSpy).not.toHaveBeenCalled();
  });

  it('disables button and shows saving text in Arabic while in-flight', async () => {
    stubFetch(CATEGORIES);
    vi.spyOn(await import('../api/client'), 'apiPost').mockReturnValue(new Promise(() => {}));

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'Tea');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'الشاي');

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    expect(screen.getByRole('button', { name: 'جارٍ الحفظ…' })).toBeDisabled();
  });

  it('disables button and shows saving text in English while in-flight', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    vi.spyOn(await import('../api/client'), 'apiPost').mockReturnValue(new Promise(() => {}));

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    await userEvent.type(screen.getByLabelText('Name (English)'), 'Tea');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'الشاي');

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  });

  it('shows error on non-2xx in Arabic', async () => {
    stubFetch(CATEGORIES);
    vi.spyOn(await import('../api/client'), 'apiPost').mockRejectedValue(new (await import('../api/client')).ApiError(500));

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    await userEvent.type(screen.getByLabelText('الاسم (إنجليزي)'), 'Tea');
    await userEvent.type(screen.getByLabelText('الاسم (عربي)'), 'الشاي');

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('تعذّر حفظ التغيير.');
  });

  it('shows error on non-2xx in English', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    vi.spyOn(await import('../api/client'), 'apiPost').mockRejectedValue(new (await import('../api/client')).ApiError(400));

    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    await userEvent.type(screen.getByLabelText('Name (English)'), 'Tea');
    await userEvent.type(screen.getByLabelText('Name (Arabic)'), 'الشاي');

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    await waitFor(() => screen.getByRole('alert'));
    expect(screen.getByRole('alert')).toHaveTextContent('The change could not be saved.');
  });

  it('axe passes in Arabic with form open', async () => {
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'إنشاء فئة' }));

    const container = screen.getByRole('heading', { name: 'فئة جديدة' }).closest('section')!;
    expect(await axe(container)).toHaveNoViolations();
  });

  it('axe passes in English with form open', async () => {
    await i18n.changeLanguage('en');
    stubFetch(CATEGORIES);
    renderWithProviders(<CategoriesPage />);

    await userEvent.click(screen.getByRole('button', { name: 'Create category' }));

    const container = screen.getByRole('heading', { name: 'New category' }).closest('section')!;
    expect(await axe(container)).toHaveNoViolations();
  });
});
