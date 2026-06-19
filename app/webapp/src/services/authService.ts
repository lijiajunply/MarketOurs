import { apiClient, BASE_URL } from './apiClient';
import { getAccessToken } from './authSession';
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
  UnbindThirdPartyRequest,
} from '../types';

export const authService = {
  login: (data: LoginRequest) => apiClient.post<TokenDto>('/Auth/login', data),

  sendLoginCode: (data: SendCodeRequest) => apiClient.post<void>('/Auth/send-login-code', data),

  loginByCode: (data: LoginByCodeRequest) => apiClient.post<TokenDto>('/Auth/login-by-code', data),

  register: (data: UserCreateDto) => apiClient.post<string>('/Auth/register', data),

  sendRegistrationCode: (regToken: string, captchaToken?: string) =>
    apiClient.post<void>(`/Auth/send-registration-code?regToken=${regToken}${captchaToken ? `&captchaToken=${encodeURIComponent(captchaToken)}` : ''}`),

  verifyRegistration: (data: VerifyRegistrationRequest) => apiClient.post<UserDto>('/Auth/verify-registration', data),

  refresh: (data: RefreshRequest) => apiClient.post<TokenDto>('/Auth/refresh', data),

  logout: (deviceType: string = 'Web') => apiClient.post<void>(`/Auth/logout?deviceType=${deviceType}`),

  getInfo: () => apiClient.get<UserDto>('/Auth/info'),

  verifyEmail: (code: string) => apiClient.get<void>(`/Auth/verify-email?code=${code}`),

  verifyPhone: (code: string) => apiClient.post<void>(`/Auth/verify-phone?code=${code}`),

  forgotPassword: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/forgot-password', data),

  resetPassword: (data: ResetPasswordRequest) => apiClient.post<void>('/Auth/reset-password', data),

  resendVerification: (data: ForgotPasswordRequest) => apiClient.post<void>('/Auth/resend-verification', data),

  sendEmailCode: (purpose: 'verification' | 'unbind-third-party' | 'third-party-unbind' = 'verification') =>
    apiClient.post<void>(`/Auth/send-email-code?purpose=${encodeURIComponent(purpose)}`),

  sendPhoneCode: () => apiClient.post<void>('/Auth/send-phone-code'),

  verifyEmailCode: (data: VerifyCodeRequest) => apiClient.post<void>('/Auth/verify-email-code', data),

  unbindThirdParty: (data: UnbindThirdPartyRequest) => apiClient.post<void>('/Auth/unbind-third-party', data),

  getExternalLoginUrl: (provider: string, returnUrl: string, purpose: 'login' | 'bind' = 'login') => {
    let url = `${BASE_URL}/Auth/external-login?provider=${provider}&returnUrl=${encodeURIComponent(returnUrl)}&purpose=${purpose}`;
    if (purpose === 'bind') {
      const token = getAccessToken();
      if (token) {
        url += `&access_token=${encodeURIComponent(token)}`;
      }
    }
    return url;
  },

  getCaptchaChallenge: () => apiClient.get<{ token: string; backgroundImage: string; puzzleImage: string; puzzleWidth: number; puzzleHeight: number }>('/Auth/captcha-challenge'),

  verifyCaptcha: (data: { token: string; x: number }) => apiClient.post<string>('/Auth/verify-captcha', data),
};
