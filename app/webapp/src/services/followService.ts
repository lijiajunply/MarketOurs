import { apiClient } from './apiClient';
import type {
  FollowToggleResult,
  PagedResult,
  UserSimpleDto,
} from '../types';

export const followService = {
  toggleFollow: (userId: string) =>
    apiClient.post<FollowToggleResult>(`/Follow/users/${userId}`),

  getFollowers: (userId: string, pageIndex?: number, pageSize?: number) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    return apiClient.get<PagedResult<UserSimpleDto>>(`/Follow/users/${userId}/followers?${params.toString()}`);
  },

  getFollowing: (userId: string, pageIndex?: number, pageSize?: number) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    return apiClient.get<PagedResult<UserSimpleDto>>(`/Follow/users/${userId}/following?${params.toString()}`);
  },

  blockUser: (userId: string) =>
    apiClient.post(`/Follow/block/${userId}`),

  unblockUser: (userId: string) =>
    apiClient.delete(`/Follow/block/${userId}`),

  getBlocked: (pageIndex?: number, pageSize?: number) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    return apiClient.get<PagedResult<UserSimpleDto>>(`/Follow/block?${params.toString()}`);
  },
};
