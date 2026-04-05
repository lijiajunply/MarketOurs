import { apiClient } from './apiClient';

export const fileService = {
  uploadImage: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post<string>('/File/upload/image', formData);
  },

  uploadImages: (files: File[]) => {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));
    return apiClient.post<string[]>('/File/upload/images', formData);
  },
};
