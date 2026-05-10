import { apiClient } from './apiClient';
import type {
  CommentDto,
  CommentCreateDto,
  CommentUpdateDto,
  PagedResult,
  LikeToggleResult,
} from '../types';

export const commentService = {
  getComments: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<CommentDto>>(`/Comment?${params.toString()}`);
  },

  createComment: (data: CommentCreateDto) =>
    apiClient.post<CommentDto>('/Comment', data),

  getComment: (id: string) =>
    apiClient.get<CommentDto>(`/Comment/${id}`),

  updateComment: (id: string, data: CommentUpdateDto) =>
    apiClient.put<CommentDto>(`/Comment/${id}`, data),

  deleteComment: (id: string) =>
    apiClient.delete<void>(`/Comment/${id}`),

  replyComment: (id: string, data: CommentCreateDto) =>
    apiClient.post<CommentDto>(`/Comment/${id}/reply`, data),

  likeComment: (id: string) =>
    apiClient.post<LikeToggleResult>(`/Comment/${id}/like`),

  dislikeComment: (id: string) =>
    apiClient.post<LikeToggleResult>(`/Comment/${id}/dislike`),

  searchComments: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<CommentDto>>(`/Comment/search?${params.toString()}`);
  },
};
