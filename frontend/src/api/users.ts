import { useQuery } from '@tanstack/react-query';
import { apiGet } from './client';

/** `UserResponse` of the API (backend/src/MiniPos.Api/Features/Common/UserResponse.cs). */
export interface User {
  id: string;
  nameEn: string;
  nameAr: string;
  role: 0 | 1 | 2;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
  modifiedBy: string | null;
  modifiedAt: string | null;
}

export function useUsers() {
  return useQuery({ queryKey: ['users'], queryFn: () => apiGet<User[]>('/api/users') });
}
