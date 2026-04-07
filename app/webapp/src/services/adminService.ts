import { apiClient } from './apiClient';
import type {
  AddIpRequest,
  AdminOverviewDto,
  AdminSettingsDto,
  BlacklistStats,
  IpCheckResult,
  LogDistribution,
  LogEntry,
  LogStatistics,
  PaginatedResponse,
  PagedResult,
  PostDto,
  RemoveIpRequest,
  UpdateUserStatusRequest,
  UserDto,
} from '../types';

export const adminService = {
  getLogs: (
    pageIndex: number = 1,
    pageSize: number = 10,
    searchTerm?: string,
    levelFilter?: string,
    timeRange?: string
  ) => {
    const params = new URLSearchParams({
      pageIndex: pageIndex.toString(),
      pageSize: pageSize.toString(),
    });
    if (searchTerm) params.append('searchTerm', searchTerm);
    if (levelFilter) params.append('levelFilter', levelFilter);
    if (timeRange) params.append('timeRange', timeRange);
    return apiClient.get<PaginatedResponse<LogEntry>>(`/Logs?${params.toString()}`);
  },

  getLogStatistics: () =>
    apiClient.get<LogStatistics>('/Logs/statistics'),

  cleanupLogs: (days: number = 7) =>
    apiClient.post<unknown>(`/Logs/cleanup?days=${days}`),

  getLogDistribution: (timeRange: string = 'today') =>
    apiClient.get<LogDistribution[]>(`/Logs/distribution?timeRange=${timeRange}`),

  getBlacklistStats: () =>
    apiClient.get<BlacklistStats>('/IpBlacklist/stats'),

  addIpToBlacklist: (data: AddIpRequest) =>
    apiClient.post<string>('/IpBlacklist/add', data),

  removeIpFromBlacklist: (data: RemoveIpRequest) =>
    apiClient.post<string>('/IpBlacklist/remove', data),

  refreshBlacklist: () =>
    apiClient.post<string>('/IpBlacklist/refresh'),

  checkIp: (ip: string) =>
    apiClient.get<IpCheckResult>(`/IpBlacklist/check/${ip}`),

  cleanCache: () =>
    apiClient.get<string>('/clean'),

  getOverview: () =>
    apiClient.get<AdminOverviewDto>('/Admin/overview'),

  getSettings: () =>
    apiClient.get<AdminSettingsDto>('/Admin/settings'),

  updateSettings: (data: AdminSettingsDto) =>
    apiClient.put<AdminSettingsDto>('/Admin/settings', data),

  updateUserStatus: (id: string, data: UpdateUserStatusRequest) =>
    apiClient.put<UserDto>(`/Admin/users/${id}/status`, data),

  getUsers: (pageIndex: number = 1, pageSize: number = 10, keyword?: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
    });
    if (keyword) {
      params.append('Keyword', keyword);
    }

    return apiClient.get<PagedResult<UserDto>>(`/User?${params.toString()}`);
  },

  searchUsers: (pageIndex: number = 1, pageSize: number = 10, keyword: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
      Keyword: keyword,
    });
    return apiClient.get<PagedResult<UserDto>>(`/User/search?${params.toString()}`);
  },

  deleteUser: (id: string) =>
    apiClient.delete<void>(`/User/${id}`),

  getPosts: (pageIndex: number = 1, pageSize: number = 10, keyword?: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
    });
    if (keyword) {
      params.append('Keyword', keyword);
    }

    return apiClient.get<PagedResult<PostDto>>(`/Post?${params.toString()}`);
  },

  searchPosts: (pageIndex: number = 1, pageSize: number = 10, keyword: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
      Keyword: keyword,
    });
    return apiClient.get<PagedResult<PostDto>>(`/Post/search?${params.toString()}`);
  },

  deletePost: (id: string) =>
    apiClient.delete<void>(`/Post/${id}`),
};
