import { apiClient } from './apiClient';
import type {
  NotificationDto,
  PushSettingsDto,
  PagedResult,
} from '../types';

export const notificationService = {
  getNotifications: (pageIndex?: number, pageSize?: number) => {
    const params = new URLSearchParams();
    if (pageIndex !== undefined) params.append('PageIndex', pageIndex.toString());
    if (pageSize !== undefined) params.append('PageSize', pageSize.toString());
    return apiClient.get<PagedResult<NotificationDto>>(`/Notification?${params.toString()}`);
  },

  getUnreadCount: () =>
    apiClient.get<number>('/Notification/unread-count'),

  markAsRead: (id: string) =>
    apiClient.post<void>(`/Notification/${id}/read`),

  markAllAsRead: () =>
    apiClient.post<void>('/Notification/read-all'),

  getSettings: () =>
    apiClient.get<PushSettingsDto>('/Notification/settings'),

  updateSettings: (settings: PushSettingsDto) =>
    apiClient.put<void>('/Notification/settings', settings),
};
