import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

export interface CompanySettings {
  taxRatePercent: number;
  receiptFooterText: string;
  lowStockThreshold: number;
}

export function useCompanySettings() {
  return useQuery({
    queryKey: ['companySettings'],
    queryFn: () => apiGet<CompanySettings>('/api/company/settings'),
  });
}
