import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

/** `SaleSummary` of the API (backend/src/MiniPos.Api/Features/Sales/ListSales/Request.cs). */
export interface SaleSummary {
  id: string;
  number: number;
  createdAt: string;
  netHalalas: number;
  vatHalalas: number;
  totalHalalas: number;
}

export interface SalesListResponse {
  sales: SaleSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function useSales(page: number = 1) {
  return useQuery({
    queryKey: ['sales', page],
    queryFn: () => apiGet<SalesListResponse>(`/api/pos/sales?page=${page}&pageSize=20`),
  });
}