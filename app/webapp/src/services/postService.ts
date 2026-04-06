import { apiClient } from './apiClient';
import type {
  PostDto,
  PostCreateDto,
  PostUpdateDto,
  PagedResult,
  CommentDto,
} from '../types';

export const postService = {
  getPosts: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<PostDto>>(`/Post?${params.toString()}`);
  },

  getHotPosts: (count: number = 10) =>
    apiClient.get<PostDto[]>(`/Post/hot?count=${count}`),

  getPost: (id: string, options?: RequestInit) =>
    apiClient.get<PostDto>(`/Post/${id}`, options),

  createPost: (data: PostCreateDto) =>
    apiClient.post<PostDto>('/Post', data),

  updatePost: (id: string, data: PostUpdateDto) =>
    apiClient.put<PostDto>(`/Post/${id}`, data),

  deletePost: (id: string) =>
    apiClient.delete<void>(`/Post/${id}`),

  likePost: (id: string) =>
    apiClient.post<void>(`/Post/${id}/like`),

  dislikePost: (id: string) =>
    apiClient.post<void>(`/Post/${id}/dislike`),

  getPostComments: (id: string, type: string) =>
    apiClient.get<CommentDto[]>(`/Post/${id}/comments/${type}`),

  searchPosts: (pageIndex?: number, pageSize?: number, keyword?: string) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    if (keyword) params.append('Keyword', keyword);
    return apiClient.get<PagedResult<PostDto>>(`/Post/search?${params.toString()}`);
  },
};
