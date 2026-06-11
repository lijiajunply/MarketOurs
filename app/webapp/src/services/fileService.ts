import { apiClient } from './apiClient';

export const fileService = {
  getUploadKey: () =>
    apiClient.post<{ key: string; expiresIn: number }>('/File/upload/key'),

  uploadImage: (file: File, key?: string) => {
    const formData = new FormData();
    formData.append('file', file);
    const endpoint = key ? `/File/upload/image?key=${encodeURIComponent(key)}` : '/File/upload/image';
    return apiClient.post<string>(endpoint, formData);
  },

  uploadAvatar: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post<string>('/File/upload/avatar', formData);
  },

  uploadImages: (files: File[], key?: string) => {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));
    const endpoint = key ? `/File/upload/images?key=${encodeURIComponent(key)}` : '/File/upload/images';
    return apiClient.post<string[]>(endpoint, formData);
  },

  uploadStream: (
    files: File[],
    key?: string,
    onProgress?: (fraction: number) => void,
  ) => {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));
    const endpoint = key ? `/File/upload/stream?key=${encodeURIComponent(key)}` : '/File/upload/stream';
    return apiClient.postStream<string[]>(endpoint, formData, onProgress);
  },
};
