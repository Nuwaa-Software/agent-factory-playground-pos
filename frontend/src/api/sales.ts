import { useQuery } from '@tanstack/react-query';
import { apiGet, apiGetText, apiPost } from './client';

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

/** `SaleResponse` of the API (backend/src/MiniPos.Api/Features/Sales/CreateSale/Response.cs). */
export interface SaleResponse {
  id: string;
  number: number;
  createdAt: string;
  lines: Array<{
    productId: string;
    nameEn: string;
    nameAr: string;
    quantity: number;
    unitPriceHalalas: number;
    lineNetHalalas: number;
  }>;
  netHalalas: number;
  vatHalalas: number;
  totalHalalas: number;
  qrCode: string;
  voided: boolean;
  voidedAt: string | null;
  paymentType: number;
  customerId: string | null;
}

export interface CreateSaleRequest {
  lines: Array<{ productId: string; quantity: number }>;
  paymentType?: number;
  customerId?: string;
}

export function useSales(page: number, from?: string, to?: string) {
  const params = new URLSearchParams({ page: String(page), pageSize: '20' });
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  return useQuery({
    queryKey: ['sales', page, from, to],
    queryFn: () => apiGet<SalesListResponse>(`/api/pos/sales?${params.toString()}`),
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

/** POST /api/pos/sales — create a new sale. */
export async function createSale(body: CreateSaleRequest): Promise<SaleResponse> {
  return apiPost<SaleResponse>('/api/pos/sales', body);
}
