import { useTranslation } from 'react-i18next';
import { useProducts } from '../api/products';
import { formatSar } from '../lib/money';

export default function ProductsPage() {
  const { t, i18n } = useTranslation();
  const products = useProducts();

  return (
    <section aria-labelledby="products-title">
      <h1 id="products-title" className="mb-4 text-xl font-semibold">
        {t('products.title')}
      </h1>
      {products.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : products.isError ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('products.loadError')}</p>
          <button
            type="button"
            onClick={() => void products.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : products.data.length === 0 ? (
        <p>{t('products.empty')}</p>
      ) : (
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-slate-300">
              <th scope="col" className="p-2 text-start">{t('products.name')}</th>
              <th scope="col" className="p-2 text-start">{t('products.sku')}</th>
              <th scope="col" className="p-2 text-end">{t('products.price')}</th>
              <th scope="col" className="p-2 text-end">{t('products.stock')}</th>
            </tr>
          </thead>
          <tbody>
            {products.data.map((p) => (
              <tr key={p.id} className="border-b border-slate-100">
                <td className="p-2">{i18n.language === 'ar' ? p.nameAr : p.nameEn}</td>
                <td className="p-2">
                  <bdi>{p.sku}</bdi>
                </td>
                <td className="p-2 text-end">{t('money.sar', { amount: formatSar(p.priceHalalas) })}</td>
                <td className="p-2 text-end">{p.stock}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
