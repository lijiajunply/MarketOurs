import '../models/api_response.dart';
import '../models/notification.dart';
import '../models/paged_result.dart';
import 'api_service.dart';

class NotificationService {
  final ApiService _apiService;

  NotificationService(this._apiService);

  Future<PagedResult<NotificationDto>?> getNotifications({int pageIndex = 1, int pageSize = 10}) async {
    final response = await _apiService.get('/Notification', queryParameters: {
      'PageIndex': pageIndex,
      'PageSize': pageSize,
    });
    final apiRes = ApiResponse<PagedResult<NotificationDto>>.fromJson(
      response.data,
      (json) => PagedResult<NotificationDto>.fromJson(
        json as Map<String, dynamic>,
        (item) => NotificationDto.fromJson(item as Map<String, dynamic>),
      ),
    );
    return apiRes.data;
  }

  Future<int> getUnreadCount() async {
    final response = await _apiService.get('/Notification/unread-count');
    final apiRes = ApiResponse<int>.fromJson(response.data, (json) => json as int);
    return apiRes.data ?? 0;
  }

  Future<bool> markAsRead(String id) async {
    final response = await _apiService.post('/Notification/$id/read', data: {});
    final apiRes = ApiResponse<void>.fromJson(response.data, (json) => null);
    return apiRes.success;
  }

  Future<bool> markAllAsRead() async {
    final response = await _apiService.post('/Notification/read-all', data: {});
    final apiRes = ApiResponse<void>.fromJson(response.data, (json) => null);
    return apiRes.success;
  }

  Future<PushSettingsDto?> getSettings() async {
    final response = await _apiService.get('/Notification/settings');
    final apiRes = ApiResponse<PushSettingsDto>.fromJson(
      response.data,
      (json) => PushSettingsDto.fromJson(json as Map<String, dynamic>),
    );
    return apiRes.data;
  }

  Future<bool> updateSettings(PushSettingsDto settings) async {
    final response = await _apiService.put('/Notification/settings', data: settings.toJson());
    final apiRes = ApiResponse<void>.fromJson(response.data, (json) => null);
    return apiRes.success;
  }
}
