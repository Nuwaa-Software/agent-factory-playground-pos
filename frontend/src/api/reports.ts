import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

export interface DailySalesResponse {
  date: string;
  saleCount: number;
  netTotal: number;
  vatTotal: number;
  grossTotal: number;
}

export function useDailySales(date: string) {
  return useQuery({
    queryKey: ['dailySales', date],
    queryFn: () => apiGet<DailySalesResponse>(`/api/pos/reports/daily-sales?date=${date}`),
  });
}
