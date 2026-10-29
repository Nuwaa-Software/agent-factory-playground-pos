import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

/** `CustomerResponse` of the API (backend/src/MiniPos.Api/Features/Common/CustomerResponse.cs). */
export interface Customer {
  id: string;
  name: string;
  phone: string | null;
  creditBalanceHalalas: number;
}

export function useCustomers() {
  return useQuery({ queryKey: ['customers'], queryFn: () => apiGet<Customer[]>('/api/customers') });
}
