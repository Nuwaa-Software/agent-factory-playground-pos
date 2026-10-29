import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

export interface Category {
  id: string;
  nameEn: string;
  nameAr: string;
  description: string | null;
  parentId: string | null;
}

export function useCategories() {
  return useQuery({ queryKey: ['categories'], queryFn: () => apiGet<Category[]>('/api/inventory/categories') });
}
