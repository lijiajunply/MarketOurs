import { apiClient, BASE_URL } from './apiClient';
import type {
  LoginRequest,
  TokenDto,
  UserCreateDto,
  UserDto,
  RefreshRequest,
  VerifyCodeRequest,
  VerifyRegistrationRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  SendCodeRequest,
  LoginByCodeRequest,
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

  sendLoginCode: (data: SendCodeRequest) => apiClient.post<void>('/Auth/send-login-code', data),

  loginByCode: async (data: LoginByCodeRequest) => {
    const response = await apiClient.post<TokenDto>('/Auth/login-by-code', data);
    if (response.data?.accessToken) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
    }
    return response;
  },

  register: (data: UserCreateDto) => apiClient.post<string>('/Auth/register', data),

  sendRegistrationCode: (regToken: string) => apiClient.post<void>(`/Auth/send-registration-code?regToken=${regToken}`),

  verifyRegistration: (data: VerifyRegistrationRequest) => apiClient.post<UserDto>('/Auth/verify-registration', data),

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

  verifyEmail: (code: string) => apiClient.get<void>(`/Auth/verify-email?code=${code}`),

  verifyPhone: (code: string) => apiClient.post<void>(`/Auth/verify-phone?code=${code}`),

  forgotPassword: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/forgot-password', data),

  resetPassword: (data: ResetPasswordRequest) => apiClient.post<void>('/Auth/reset-password', data),

  resendVerification: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/resend-verification', data),

  sendEmailCode: () => apiClient.post<void>('/Auth/send-email-code'),

  sendPhoneCode: () => apiClient.post<void>('/Auth/send-phone-code'),

  verifyEmailCode: (data: VerifyCodeRequest) => apiClient.post<void>('/Auth/verify-email-code', data),

  getExternalLoginUrl: (provider: string, returnUrl: string, purpose: 'login' | 'bind' = 'login') => {
    return `${BASE_URL}/Auth/external-login?provider=${provider}&returnUrl=${encodeURIComponent(returnUrl)}&purpose=${purpose}`;
  },
};
