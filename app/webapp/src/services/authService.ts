import { apiClient } from './apiClient';
import type {
  LoginRequest,
  TokenDto,
  UserCreateDto,
  UserDto,
  RefreshRequest,
  VerifyCodeRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../types';

export const authService = {
  login: async (data: LoginRequest) => {
    const response = await apiClient.post<TokenDto>('/Auth/login', data);
    if (response.data?.accessToken) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
    }
    return response;
  },

  register: (data: UserCreateDto) => apiClient.post<UserDto>('/Auth/register', data),

  refresh: async (data: RefreshRequest) => {
    const response = await apiClient.post<TokenDto>('/Auth/refresh', data);
    if (response.data?.accessToken) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
    }
    return response;
  },

  logout: (deviceType: string = 'Web') => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    return apiClient.post<void>(`/Auth/logout?deviceType=${deviceType}`);
  },

  getInfo: () => apiClient.get<UserDto>('/Auth/info'),

  verifyEmail: (token: string) => apiClient.get<void>(`/Auth/verify-email?token=${token}`),

  verifyPhone: (data: VerifyCodeRequest) => apiClient.post<void>('/Auth/verify-phone', data),

  forgotPassword: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/forgot-password', data),

  resetPassword: (data: ResetPasswordRequest) => apiClient.post<void>('/Auth/reset-password', data),

  resendVerification: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/resend-verification', data),

  sendEmailCode: () => apiClient.post<void>('/Auth/send-email-code'),

  sendPhoneCode: () => apiClient.post<void>('/Auth/send-phone-code'),

  verifyEmailCode: (data: VerifyCodeRequest) => apiClient.post<void>('/Auth/verify-email-code', data),
};
