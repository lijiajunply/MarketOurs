import { apiClient } from './apiClient';
import type {
  LogEntry,
  LogStatistics,
  LogDistribution,
  PaginatedResponse,
  BlacklistStats,
  AddIpRequest,
  RemoveIpRequest,
  IpCheckResult,
} from '../types';

export const adminService = {
  // Logs
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
    apiClient.post<any>(`/Logs/cleanup?days=${days}`),

  getLogDistribution: (timeRange: string = 'today') =>
    apiClient.get<LogDistribution[]>(`/Logs/distribution?timeRange=${timeRange}`),

  // IpBlacklist
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

  // Cache
  cleanCache: () =>
    apiClient.get<string>('/clean'),
};
