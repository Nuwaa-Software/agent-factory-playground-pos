import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { apiGet } from '../api/client';
import { useProducts } from '../api/products';

interface Category {
  id: string;
  nameEn: string;
  nameAr: string;
}

export default function ProductEditPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const products = useProducts();

  // Fire-and-forget categories fetch (groundwork for #105 — must not affect state)
  useEffect(() => {
    apiGet<Category[]>('/api/inventory/categories').catch(() => {});
  }, []);

  if (products.isPending) {
    return <p role="status">{t('common.loading')}</p>;
  }

  if (products.isError || !products.data.some((p) => p.id === id)) {
    return (
      <div className="flex flex-col items-start gap-2">
        <p role="alert">{t('products.loadError')}</p>
        <Link
          to="/products"
          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('common.retry')}
        </Link>
      </div>
    );
  }

  const product = products.data.find((p) => p.id === id)!;

  return (
    <section aria-labelledby="product-edit-title" className="max-w-md">
      <h1 id="product-edit-title" className="mb-4 text-xl font-semibold">
        {t('productEdit.title')}
      </h1>
      <div className="mb-6 flex flex-col gap-2 rounded-md border border-slate-200 p-3">
        <p>
          <span className="font-medium">{t('product.nameEn')}:</span>{' '}
          {product.nameEn}
        </p>
        <p>
          <span className="font-medium">{t('product.nameAr')}:</span>{' '}
          {product.nameAr}
        </p>
        <p>
          <span className="font-medium">{t('product.sku')}:</span>{' '}
          <bdi>{product.sku}</bdi>
        </p>
      </div>
      <p className="mb-4 text-sm text-slate-500">
        {t('product.readOnlyNote')}
      </p>
      <Link
        to="/products"
        className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
      >
        {t('product.backToList')}
      </Link>
    </section>
  );
}
