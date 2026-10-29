import { screen, within } from '@testing-library/react';
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
});
