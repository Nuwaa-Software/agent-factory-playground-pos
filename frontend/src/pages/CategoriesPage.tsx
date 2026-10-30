import { useTranslation } from 'react-i18next';
import { useCategories } from '../api/categories';

export default function CategoriesPage() {
  const { t, i18n } = useTranslation();
  const categories = useCategories();

  return (
    <section aria-labelledby="categories-title">
      <h1 id="categories-title" className="mb-4 text-xl font-semibold">
        {t('categories.title')}
      </h1>
      {categories.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : categories.isError ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('categories.loadError')}</p>
          <button
            type="button"
            onClick={() => void categories.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : categories.data.length === 0 ? (
        <p>{t('categories.empty')}</p>
      ) : (
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-slate-300">
              <th scope="col" className="p-2 text-start">{t('categories.name')}</th>
              <th scope="col" className="p-2 text-start">{t('categories.description')}</th>
              <th scope="col" className="p-2 text-start">{t('categories.parent')}</th>
            </tr>
          </thead>
          <tbody>
            {categories.data.map((c) => {
              const parent = c.parentId ? categories.data.find((p) => p.id === c.parentId) : undefined;
              return (
                <tr key={c.id} className="border-b border-slate-100">
                  <td className="p-2">
                    <bdi>{i18n.language === 'ar' ? c.nameAr : c.nameEn}</bdi>
                  </td>
                  <td className="p-2">{c.description ?? ''}</td>
                  <td className="p-2">
                    {parent ? (
                      <bdi>{i18n.language === 'ar' ? parent.nameAr : parent.nameEn}</bdi>
                    ) : (
                      t('categories.noParent')
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}
