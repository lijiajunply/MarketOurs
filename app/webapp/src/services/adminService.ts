import { apiClient } from './apiClient';
import type {
  AddIpRequest,
  AdminOverviewDto,
  AdminResetPasswordRequest,
  BlacklistStats,
  CommentDto,
  CommentUpdateDto,
  IpCheckResult,
  LogDistribution,
  LogEntry,
  LogStatistics,
  PaginatedResponse,
  PagedResult,
  PostDto,
  PostTagCreateDto,
  PostTagDto,
  PostTagUpdateDto,
  PostUpdateDto,
  RemoveIpRequest,
  UpdateUserStatusRequest,
  UserCreateDto,
  UserDto,
  UserUpdateDto,
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

  updateUserStatus: (id: string, data: UpdateUserStatusRequest) =>
    apiClient.put<UserDto>(`/Admin/users/${id}/status`, data),

  createUser: (data: UserCreateDto) =>
    apiClient.post<UserDto>(`/User`, data),

  updateUser: (id: string, data: UserUpdateDto) =>
    apiClient.put<UserDto>(`/User/${id}`, data),

  resetUserPassword: (id: string, data: AdminResetPasswordRequest) =>
    apiClient.put<void>(`/User/${id}/password`, data),

  getUserById: (id: string) =>
    apiClient.get<UserDto>(`/User/${id}`),

  updatePost: (id: string, data: PostUpdateDto) =>
    apiClient.put<PostDto>(`/Post/${id}`, data),

  updatePostReview: (id: string, data: { isReview: boolean }) =>
    apiClient.put<PostDto>(`/Post/${id}/review`, data),

  updatePostTag: (id: string, data: { tagId: string | null }) =>
    apiClient.post<PostDto>(`/Post/${id}/tag`, data),

  getPostTags: () =>
    apiClient.get<PostTagDto[]>('/PostTag/admin'),

  createPostTag: (data: PostTagCreateDto) =>
    apiClient.post<PostTagDto>('/PostTag', data),

  updatePostTagDefinition: (id: string, data: PostTagUpdateDto) =>
    apiClient.put<PostTagDto>(`/PostTag/${id}`, data),

  deactivatePostTag: (id: string) =>
    apiClient.delete<PostTagDto>(`/PostTag/${id}`),

  updateCommentReview: (id: string, data: { isReview: boolean }) =>
    apiClient.put<CommentDto>(`/Comment/${id}/review`, data),

  updateComment: (id: string, data: CommentUpdateDto) =>
    apiClient.put<CommentDto>(`/Comment/${id}`, data),

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

  getComments: (pageIndex: number = 1, pageSize: number = 10, keyword?: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
    });
    if (keyword) {
      params.append('Keyword', keyword);
    }

    return apiClient.get<PagedResult<CommentDto>>(`/Comment?${params.toString()}`);
  },

  searchComments: (pageIndex: number = 1, pageSize: number = 10, keyword: string) => {
    const params = new URLSearchParams({
      PageIndex: pageIndex.toString(),
      PageSize: pageSize.toString(),
      Keyword: keyword,
    });
    return apiClient.get<PagedResult<CommentDto>>(`/Comment/search?${params.toString()}`);
  },

  deleteComment: (id: string) =>
    apiClient.delete<void>(`/Comment/${id}`),
};
