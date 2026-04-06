import { apiClient } from './apiClient';
import type {
  UserDto,
  UserCreateDto,
  UserUpdateDto,
  PagedResult,
} from '../types';

export const userService = {
  getUsers: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<UserDto>>(`/User?${params.toString()}`);
  },

  createUser: (data: UserCreateDto) =>
    apiClient.post<UserDto>('/User', data),

  getUser: (id: string) =>
    apiClient.get<UserDto>(`/User/${id}`),

  updateUser: (id: string, data: UserUpdateDto) =>
    apiClient.put<UserDto>(`/User/${id}`, data),

  deleteUser: (id: string) =>
    apiClient.delete<void>(`/User/${id}`),

  getProfile: () =>
    apiClient.get<UserDto>('/User/profile'),

  updateProfile: (data: UserUpdateDto) =>
    apiClient.put<UserDto>('/User/profile', data),

  changePassword: (data: any) =>
    apiClient.put<void>('/User/password', data),

  searchUsers: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<UserDto>>(`/User/search?${params.toString()}`);
  },
};
