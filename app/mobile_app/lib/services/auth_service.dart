import '../models/api_response.dart';
import '../models/auth.dart';
import '../models/user.dart';
import 'api_service.dart';

class AuthService {
  final _api = ApiService().dio;

  Future<ApiResponse<TokenDto>> login(LoginRequest request) async {
    final response = await _api.post('/Auth/login', data: request.toJson());
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> sendLoginCode(SendCodeRequest request) async {
    final response = await _api.post('/Auth/send-login-code', data: request.toJson());
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<TokenDto>> loginByCode(LoginByCodeRequest request) async {
    final response = await _api.post('/Auth/login-by-code', data: request.toJson());
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<UserDto>> register(UserCreateDto request) async {
    final response = await _api.post('/Auth/register', data: request.toJson());
    return ApiResponse<UserDto>.fromJson(
      response.data,
      (json) => UserDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<TokenDto>> refresh(RefreshRequest request) async {
    final response = await _api.post('/Auth/refresh', data: request.toJson());
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> logout({String deviceType = 'Web'}) async {
    final response = await _api.post('/Auth/logout', queryParameters: {'deviceType': deviceType});
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<UserDto>> getInfo() async {
    final response = await _api.get('/Auth/info');
    return ApiResponse<UserDto>.fromJson(
      response.data,
      (json) => UserDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> verifyEmail(String token) async {
    final response = await _api.get('/Auth/verify-email', queryParameters: {'token': token});
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> forgotPassword(ForgotPasswordRequest request) async {
    final response = await _api.post('/Auth/forgot-password', data: request.toJson());
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> resetPassword(ResetPasswordRequest request) async {
    final response = await _api.post('/Auth/reset-password', data: request.toJson());
    return ApiResponse.fromJson(response.data, (json) => json);
  }
}
