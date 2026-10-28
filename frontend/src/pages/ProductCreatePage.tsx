import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { apiGet, apiPost, ApiError } from '../api/client';
import { formatSar } from '../lib/money';

interface Category {
  id: string;
  nameEn: string;
  nameAr: string;
}

interface ProductResponse {
  id: string;
  nameEn: string;
  nameAr: string;
  sku: string;
  priceHalalas: number;
  stock: number;
  categoryId?: string | null;
}

type FormState = 'form' | 'success';

export default function ProductCreatePage() {
  const { t, i18n } = useTranslation();
  const [state, setState] = useState<FormState>('form');
  const [nameEn, setNameEn] = useState('');
  const [nameAr, setNameAr] = useState('');
  const [sku, setSku] = useState('');
  const [price, setPrice] = useState('');
  const [stock, setStock] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [categories, setCategories] = useState<Category[]>([]);
  const [inFlight, setInFlight] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [createdProduct, setCreatedProduct] = useState<ProductResponse | null>(null);

  // Validation errors
  const [nameEnError, setNameEnError] = useState(false);
  const [nameArError, setNameArError] = useState(false);
  const [skuError, setSkuError] = useState(false);
  const [priceError, setPriceError] = useState(false);
  const [stockError, setStockError] = useState(false);

  // Load categories
  useEffect(() => {
    apiGet<Category[]>('/api/inventory/categories')
      .then((data) => setCategories(data))
      .catch(() => {});
  }, []);

  const validate = (): boolean => {
    let valid = true;
    setNameEnError(false);
    setNameArError(false);
    setSkuError(false);
    setPriceError(false);
    setStockError(false);

    if (nameEn.trim() === '') {
      setNameEnError(true);
      valid = false;
    }
    if (nameAr.trim() === '') {
      setNameArError(true);
      valid = false;
    }
    if (sku.trim() === '' || !/^[A-Z0-9-]+$/.test(sku)) {
      setSkuError(true);
      valid = false;
    }
    const priceTrimmed = price.trim();
    if (priceTrimmed === '' || !/^\d+(\.\d+)?$/.test(priceTrimmed) || parseFloat(priceTrimmed) < 0) {
      setPriceError(true);
      valid = false;
    }
    if (stock.trim() === '' || !/^\d+$/.test(stock)) {
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
      const result = await apiPost<ProductResponse>('/api/inventory/products', {
        nameEn: nameEn.trim(),
        nameAr: nameAr.trim(),
        sku: sku.trim(),
        priceHalalas,
        stock: stockInt,
        categoryId: categoryId || undefined,
      });
      setCreatedProduct(result);
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

  const handleNewProduct = () => {
    setNameEn('');
    setNameAr('');
    setSku('');
    setPrice('');
    setStock('');
    setCategoryId('');
    setState('form');
  };

  const handleBackToList = () => {
    window.location.href = '/products';
  };

  if (state === 'success' && createdProduct) {
    return (
      <section aria-labelledby="product-created-title" className="max-w-md">
        <h1 id="product-created-title" tabIndex={-1} role="status" className="mb-4 text-xl font-semibold text-green-700">
          {t('product.created')}
        </h1>
        <div className="mb-6 flex flex-col gap-2 rounded-md border border-slate-200 p-3">
          <p>
            <span className="font-medium">{t('product.nameEn')}:</span>{' '}
            {createdProduct.nameEn}
          </p>
          <p>
            <span className="font-medium">{t('product.nameAr')}:</span>{' '}
            {createdProduct.nameAr}
          </p>
          <p>
            <span className="font-medium">{t('product.sku')}:</span>{' '}
            <bdi>{createdProduct.sku}</bdi>
          </p>
          <p>
            <span className="font-medium">{t('product.price')}:</span>{' '}
            {t('money.sar', { amount: formatSar(createdProduct.priceHalalas) })}
          </p>
          <p>
            <span className="font-medium">{t('product.stock')}:</span>{' '}
            {createdProduct.stock}
          </p>
        </div>
        <div className="flex flex-col gap-2">
          <button
            type="button"
            onClick={handleBackToList}
            className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
          >
            {t('product.backToList')}
          </button>
          <button
            type="button"
            onClick={handleNewProduct}
            className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('product.newProduct')}
          </button>
        </div>
      </section>
    );
  }

  return (
    <section aria-labelledby="product-create-title" className="max-w-md">
      <h1 id="product-create-title" className="mb-4 text-xl font-semibold">
        {t('productCreate.title')}
      </h1>
      {errorMessage && (
        <div role="alert" className="mb-4 text-sm text-red-700">
          {errorMessage}
        </div>
      )}
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        {/* Name (English) */}
        <div className="flex flex-col gap-1">
          <label htmlFor="name-en">{t('product.nameEn')}</label>
          <input
            id="name-en"
            value={nameEn}
            onChange={(e) => { setNameEn(e.target.value); setNameEnError(false); }}
            aria-invalid={nameEnError}
            aria-describedby={nameEnError ? 'name-en-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {nameEnError && (
            <p id="name-en-error" role="alert" className="text-sm text-red-700">
              {t('product.errRequired')}
            </p>
          )}
        </div>

        {/* Name (Arabic) */}
        <div className="flex flex-col gap-1">
          <label htmlFor="name-ar">{t('product.nameAr')}</label>
          <input
            id="name-ar"
            dir="rtl"
            value={nameAr}
            onChange={(e) => { setNameAr(e.target.value); setNameArError(false); }}
            aria-invalid={nameArError}
            aria-describedby={nameArError ? 'name-ar-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {nameArError && (
            <p id="name-ar-error" role="alert" className="text-sm text-red-700">
              {t('product.errRequired')}
            </p>
          )}
        </div>

        {/* SKU */}
        <div className="flex flex-col gap-1">
          <label htmlFor="sku">{t('product.sku')}</label>
          <input
            id="sku"
            dir="ltr"
            value={sku}
            onChange={(e) => { setSku(e.target.value); setSkuError(false); }}
            aria-invalid={skuError}
            aria-describedby={skuError ? 'sku-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {skuError && (
            <p id="sku-error" role="alert" className="text-sm text-red-700">
              {t('product.errSku')}
            </p>
          )}
        </div>

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
