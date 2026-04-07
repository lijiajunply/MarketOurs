import type { ApiResponse } from '../types';

export const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5053';

async function request<T>(
  path: string,
  method: string = 'GET',
  body?: any,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = localStorage.getItem('accessToken');
  const headers = new Headers(options.headers);

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  if (body && !(body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  const config: RequestInit = {
    ...options,
    method,
    headers,
  };

  if (body) {
    config.body = body instanceof FormData ? body : JSON.stringify(body);
  }

  const response = await fetch(`${BASE_URL}${path}`, config);

  if (response.status === 401) {
    // Handle unauthorized, maybe redirect to login or refresh token
    // For now just throw error
  }

  const data = await response.json();

  if (!response.ok) {
    throw data;
  }

  return data as ApiResponse<T>;
}

export const apiClient = {
  get: <T>(path: string, options?: RequestInit) => request<T>(path, 'GET', undefined, options),
  post: <T>(path: string, body?: any, options?: RequestInit) => request<T>(path, 'post', body, options),
  put: <T>(path: string, body?: any, options?: RequestInit) => request<T>(path, 'PUT', body, options),
  delete: <T>(path: string, options?: RequestInit) => request<T>(path, 'DELETE', undefined, options),
};
