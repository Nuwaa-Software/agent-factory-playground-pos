import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

export interface ShiftHistoryItem {
  id: string;
  cashierName: string;
  startingCashHalalas: number;
  openedAt: string;
  closedAt: string | null;
  actualCashHalalas: number | null;
  expectedCashHalalas: number | null;
  discrepancyHalalas: number | null;
}

export function useShiftHistory() {
  return useQuery({
    queryKey: ['shiftHistory'],
    queryFn: () => apiGet<ShiftHistoryItem[]>('/api/pos/shifts'),
  });
}
