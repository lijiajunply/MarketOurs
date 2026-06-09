import '../models/api_response.dart';
import '../models/user.dart';
import 'api_service.dart';

class FollowService {
  final _api = ApiService().dio;

  Map<String, dynamic> _asMap(dynamic data) {
    if (data is! Map<String, dynamic>) {
      throw const FormatException('关注服务响应格式异常');
    }
    return data;
  }

  Future<ApiResponse<FollowToggleResult>> toggleFollow(String userId) async {
    final response = await _api.post('/Follow/users/$userId');
    return ApiResponse<FollowToggleResult>.fromJson(
      _asMap(response.data),
      (json) => FollowToggleResult.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<dynamic>> getFollowers(
    String userId, {
    int pageIndex = 1,
    int pageSize = 20,
  }) async {
    final response = await _api.get(
      '/Follow/users/$userId/followers',
      queryParameters: {'PageIndex': pageIndex, 'PageSize': pageSize},
    );
    return ApiResponse<dynamic>.fromJson(
      _asMap(response.data),
      (json) => json,
    );
  }

  Future<ApiResponse<dynamic>> getFollowing(
    String userId, {
    int pageIndex = 1,
    int pageSize = 20,
  }) async {
    final response = await _api.get(
      '/Follow/users/$userId/following',
      queryParameters: {'PageIndex': pageIndex, 'PageSize': pageSize},
    );
    return ApiResponse<dynamic>.fromJson(
      _asMap(response.data),
      (json) => json,
    );
  }

  Future<ApiResponse<dynamic>> blockUser(String userId) async {
    final response = await _api.post('/Follow/block/$userId');
    return ApiResponse<dynamic>.fromJson(
      _asMap(response.data),
      (json) => json,
    );
  }

  Future<ApiResponse<dynamic>> unblockUser(String userId) async {
    final response = await _api.delete('/Follow/block/$userId');
    return ApiResponse<dynamic>.fromJson(
      _asMap(response.data),
      (json) => json,
    );
  }

  Future<ApiResponse<dynamic>> getBlocked({
    int pageIndex = 1,
    int pageSize = 20,
  }) async {
    final response = await _api.get(
      '/Follow/block',
      queryParameters: {'PageIndex': pageIndex, 'PageSize': pageSize},
    );
    return ApiResponse<dynamic>.fromJson(
      _asMap(response.data),
      (json) => json,
    );
  }
}
