import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';
import type { Product } from './products';

export function useLowStock(threshold?: number) {
  const queryKey = ['lowStock', threshold];
  const path = threshold ? `/api/pos/catalog/low-stock?threshold=${threshold}` : '/api/pos/catalog/low-stock';
  return useQuery({ queryKey, queryFn: () => apiGet<Product[]>(path) });
}
