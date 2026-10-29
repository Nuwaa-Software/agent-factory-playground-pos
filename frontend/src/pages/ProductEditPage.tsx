import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { apiGet, apiPatch, ApiError } from '../api/client';
import { useProducts } from '../api/products';
import { formatSar } from '../lib/money';

interface Category {
  id: string;
  nameEn: string;
  nameAr: string;
}

type FormState = 'form' | 'success';

export default function ProductEditPage() {
  const { t, i18n } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const products = useProducts();

  // Find the product from the list (may be undefined during loading)
  const product = products.isSuccess ? products.data.find((p) => p.id === id) : null;

  // Form state
  const [state, setState] = useState<FormState>('form');
  const [price, setPrice] = useState('');
  const [stock, setStock] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [categories, setCategories] = useState<Category[]>([]);
  const [inFlight, setInFlight] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  // Validation errors
  const [priceError, setPriceError] = useState(false);
  const [stockError, setStockError] = useState(false);

  // Load categories
  useEffect(() => {
    apiGet<Category[]>('/api/inventory/categories')
      .then((data) => setCategories(data))
      .catch(() => {});
  }, []);

  /* eslint-disable react-hooks/set-state-in-effect */
  // Prefill form from the product data (syncing external data to state)
  useEffect(() => {
    if (product) {
      setPrice(formatSar(product.priceHalalas));
      setStock(String(product.stock));
      setCategoryId(product.categoryId ?? '');
    }
  }, [product]);
  /* eslint-enable react-hooks/set-state-in-effect */

  // Early returns after all hooks
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

  const validate = (): boolean => {
    let valid = true;
    setPriceError(false);
    setStockError(false);

    const priceTrimmed = price.trim();
    if (priceTrimmed === '' || !/^\d+(\.\d+)?$/.test(priceTrimmed) || parseFloat(priceTrimmed) < 0) {
      setPriceError(true);
      valid = false;
    }
    const stockTrimmed = stock.trim();
    if (stockTrimmed === '' || !/^\d+$/.test(stockTrimmed)) {
      setStockError(true);
      valid = false;
    }
    return valid;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    if (!validate()) return;

    setInFlight(true);
    try {
      const priceHalalas = Math.round(parseFloat(price.trim()) * 100);
      const stockInt = parseInt(stock.trim(), 10);
      await apiPatch(`/api/inventory/products/${id}`, {
        id,
        priceHalalas,
        stock: stockInt,
        categoryId: categoryId || undefined,
      });
      setState('success');
    } catch (err) {
      if (err instanceof ApiError) {
        setErrorMessage(t('product.saveError'));
      } else {
        setErrorMessage(t('product.saveError'));
      }
    } finally {
      setInFlight(false);
    }
  };

  const handleBackToList = () => {
    window.location.href = '/products';
  };

  if (state === 'success') {
    return (
      <section aria-labelledby="product-updated-title" className="max-w-md">
        <h1 id="product-updated-title" tabIndex={-1} role="status" className="mb-4 text-xl font-semibold text-green-700">
          {t('product.updated')}
        </h1>
        <div className="flex flex-col gap-2">
          <button
            type="button"
            onClick={handleBackToList}
            className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
          >
            {t('product.backToList')}
          </button>
        </div>
      </section>
    );
  }

  return (
    <section aria-labelledby="product-edit-title" className="max-w-md">
      <h1 id="product-edit-title" className="mb-4 text-xl font-semibold">
        {t('productEdit.title')}
      </h1>
      <div className="mb-6 flex flex-col gap-2 rounded-md border border-slate-200 p-3">
        <p>
          <span className="font-medium">{t('product.nameEn')}:</span>{' '}
          {product?.nameEn}
        </p>
        <p>
          <span className="font-medium">{t('product.nameAr')}:</span>{' '}
          {product?.nameAr}
        </p>
        <p>
          <span className="font-medium">{t('product.sku')}:</span>{' '}
          <bdi>{product?.sku}</bdi>
        </p>
      </div>
      <p className="mb-4 text-sm text-slate-500">
        {t('product.readOnlyNote')}
      </p>
      {errorMessage && (
        <div role="alert" className="mb-4 text-sm text-red-700">
          {errorMessage}
        </div>
      )}
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        {/* Price */}
        <div className="flex flex-col gap-1">
          <label htmlFor="price">{t('product.price')}</label>
          <input
            id="price"
            type="text"
            dir="ltr"
            value={price}
            onChange={(e) => { setPrice(e.target.value); setPriceError(false); }}
            aria-invalid={priceError}
            aria-describedby={priceError ? 'price-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {priceError && (
            <p id="price-error" role="alert" className="text-sm text-red-700">
              {t('product.errMoney')}
            </p>
          )}
        </div>

        {/* Stock */}
        <div className="flex flex-col gap-1">
          <label htmlFor="stock">{t('product.stock')}</label>
          <input
            id="stock"
            type="text"
            dir="ltr"
            value={stock}
            onChange={(e) => { setStock(e.target.value); setStockError(false); }}
            aria-invalid={stockError}
            aria-describedby={stockError ? 'stock-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {stockError && (
            <p id="stock-error" role="alert" className="text-sm text-red-700">
              {t('product.errStock')}
            </p>
          )}
        </div>

        {/* Category */}
        <div className="flex flex-col gap-1">
          <label htmlFor="category">{t('product.category')}</label>
          <select
            id="category"
            value={categoryId}
            onChange={(e) => setCategoryId(e.target.value)}
            disabled={categories.length === 0}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          >
            <option value="">{t('product.categoryNone')}</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {i18n.language === 'ar' ? cat.nameAr : cat.nameEn}
              </option>
            ))}
          </select>
        </div>

        {/* Submit */}
        <button
          type="submit"
          disabled={inFlight}
          className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {inFlight ? t('product.saving') : t('product.save')}
        </button>
      </form>
    </section>
  );
}
