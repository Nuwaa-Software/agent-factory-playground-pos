import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

/** `ProductResponse` of the API (backend/src/MiniPos.Api/Features/Common/ProductResponse.cs). */
export interface Product {
  id: string;
  nameEn: string;
  nameAr: string;
  sku: string;
  priceHalalas: number;
  stock: number;
  categoryId?: string | null;
}

export function useProducts() {
  return useQuery({ queryKey: ['products'], queryFn: () => apiGet<Product[]>('/api/inventory/products') });
}
