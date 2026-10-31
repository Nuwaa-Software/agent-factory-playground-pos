import { useQuery } from '@tanstack/react-query';
import { apiGet, apiPost } from './client';

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

export interface CreateCategoryInput {
  nameEn: string;
  nameAr: string;
  description?: string;
  parentId?: string;
}

export function createCategory(input: CreateCategoryInput): Promise<Category> {
  const body: Record<string, unknown> = { nameEn: input.nameEn, nameAr: input.nameAr };
  if (input.description !== undefined) body.description = input.description;
  if (input.parentId !== undefined) body.parentId = input.parentId;
  return apiPost<Category>('/api/inventory/categories', body);
}
