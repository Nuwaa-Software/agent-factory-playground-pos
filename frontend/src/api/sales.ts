import { useQuery } from '@tanstack/react-query';
import { apiGet, apiGetText } from './client';

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

/** Minimal `SaleResponse` shape from the API (backend/src/MiniPos.Api/Features/Sales/GetSale/Request.cs). */
export interface SaleResponse {
  id: string;
  number: number;
  qrCode: string;
}

export function useSales(page: number = 1) {
  return useQuery({
    queryKey: ['sales', page],
    queryFn: () => apiGet<SalesListResponse>(`/api/pos/sales?page=${page}&pageSize=20`),
  });
}

/** Load a sale's JSON and its receipt text in parallel. */
export function useReceipt(id: string) {
  return useQuery({
    queryKey: ['receipt', id],
    queryFn: async () => {
      const [sale, receiptText] = await Promise.all([
        apiGet<SaleResponse>(`/api/pos/sales/${id}`),
        apiGetText(`/api/pos/sales/${id}/receipt`),
      ]);
      return { sale, receiptText };
    },
  });
}