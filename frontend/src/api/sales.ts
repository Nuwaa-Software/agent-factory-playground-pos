import { useQuery } from '@tanstack/react-query';
import { apiGet, apiPost } from './client';

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

/** POST /api/pos/sales — create a new sale. */
export async function createSale(body: CreateSaleRequest): Promise<SaleResponse> {
  return apiPost<SaleResponse>('/api/pos/sales', body);
}