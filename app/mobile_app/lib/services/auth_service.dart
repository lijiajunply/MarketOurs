import '../models/api_response.dart';
import '../models/auth.dart';
import '../models/user.dart';
import 'api_service.dart';

class AuthService {
  final _api = ApiService().dio;

  Future<ApiResponse<TokenDto>> login(LoginRequest request) async {
    final response = await _api.post(
      '/Auth/login',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> sendLoginCode(SendCodeRequest request) async {
    final response = await _api.post(
      '/Auth/send-login-code',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<TokenDto>> loginByCode(LoginByCodeRequest request) async {
    final response = await _api.post(
      '/Auth/login-by-code',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<String>> register(UserCreateDto request) async {
    final response = await _api.post(
      '/Auth/register',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse<String>.fromJson(
      response.data,
      (json) => json as String,
    );
  }

  Future<ApiResponse> sendRegistrationCode(String regToken, {String? captchaToken}) async {
    var queryParams = <String, dynamic>{'regToken': regToken};
    if (captchaToken != null) {
      queryParams['captchaToken'] = captchaToken;
    }
    final response = await _api.post(
      '/Auth/send-registration-code',
      queryParameters: queryParams,
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<UserDto>> verifyRegistration(
    VerifyRegistrationRequest request,
  ) async {
    final response = await _api.post(
      '/Auth/verify-registration',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse<UserDto>.fromJson(
      response.data,
      (json) => UserDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<TokenDto>> refresh(RefreshRequest request) async {
    final response = await _api.post(
      '/Auth/refresh',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse<TokenDto>.fromJson(
      response.data,
      (json) => TokenDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> logout({String deviceType = 'Web'}) async {
    final response = await _api.post(
      '/Auth/logout',
      queryParameters: {'deviceType': deviceType},
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<UserDto>> getInfo() async {
    final response = await _api.get('/Auth/info');
    return ApiResponse<UserDto>.fromJson(
      response.data,
      (json) => UserDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> verifyEmail({String? token, String? code}) async {
    final response = await _api.get(
      '/Auth/verify-email',
      queryParameters: {'token': token, 'code': code}
        ..removeWhere((_, value) => value == null),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> verifyPhone({String? token, required String code}) async {
    final response = await _api.post(
      '/Auth/verify-phone',
      queryParameters: {'token': token, 'code': code}
        ..removeWhere((_, value) => value == null),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> forgotPassword(ForgotPasswordRequest request) async {
    final response = await _api.post(
      '/Auth/forgot-password',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> resetPassword(ResetPasswordRequest request) async {
    final response = await _api.post(
      '/Auth/reset-password',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> resendVerification(ForgotPasswordRequest request) async {
    final response = await _api.post(
      '/Auth/resend-verification',
      data: request.toJson(),
      options: ApiService.anonymousOptions(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> sendEmailCode({String purpose = 'verification'}) async {
    final encodedPurpose = Uri.encodeQueryComponent(purpose);
    final response = await _api.post(
      '/Auth/send-email-code?purpose=$encodedPurpose',
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> sendPhoneCode() async {
    final response = await _api.post('/Auth/send-phone-code');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> verifyEmailCode(VerifyCodeRequest request) async {
    final response = await _api.post(
      '/Auth/verify-email-code',
      data: request.toJson(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> unbindThirdParty(UnbindThirdPartyRequest request) async {
    final response = await _api.post(
      '/Auth/unbind-third-party',
      data: request.toJson(),
    );
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  String buildExternalLoginUrl({
    required String provider,
    required String returnUrl,
    String purpose = 'login',
    String? accessToken,
    String deviceType = 'Mobile',
  }) {
    final encodedReturn = Uri.encodeQueryComponent(returnUrl);
    var url =
        '${ApiService.baseUrl}/Auth/external-login?provider=$provider&returnUrl=$encodedReturn&purpose=$purpose&deviceType=$deviceType';
    // 绑定操作需要传递 access_token，因为 WebView 跳转不会携带 Authorization 头
    if (purpose == 'bind' && accessToken != null && accessToken.isNotEmpty) {
      url += '&access_token=${Uri.encodeQueryComponent(accessToken)}';
    }
    return url;
  }

  static String get oauthCallbackPath => '/oauth-callback';

  static String get oauthCallbackUrl =>
      '${ApiService.baseUrl}$oauthCallbackPath';

  static String get mobileOAuthCallbackScheme => 'marketours';

  static String get mobileOAuthCallbackHost => 'oauth-callback';

  static String get mobileOAuthCallbackUrl =>
      '$mobileOAuthCallbackScheme://$mobileOAuthCallbackHost';

  static bool isOAuthCallback(Uri uri) {
    final isMobileCallback =
        uri.scheme == mobileOAuthCallbackScheme &&
        uri.host == mobileOAuthCallbackHost;
    final isWebCallback = uri.toString().startsWith(oauthCallbackUrl);
    return isMobileCallback || isWebCallback;
  }

  Future<CaptchaChallenge> getCaptchaChallenge() async {
    final response = await _api.get(
      '/Auth/captcha-challenge',
      options: ApiService.anonymousOptions(),
    );
    return CaptchaChallenge.fromJson(
      response.data['data'] as Map<String, dynamic>,
    );
  }

  Future<String> verifyCaptcha({
    required String token,
    required int x,
  }) async {
    final response = await _api.post(
      '/Auth/verify-captcha',
      data: {'token': token, 'x': x},
      options: ApiService.anonymousOptions(),
    );
    return response.data['data'] as String;
  }
}

class CaptchaChallenge {
  final String token;
  final String backgroundImage;
  final String puzzleImage;
  final int puzzleWidth;
  final int puzzleHeight;

  CaptchaChallenge({
    required this.token,
    required this.backgroundImage,
    required this.puzzleImage,
    required this.puzzleWidth,
    required this.puzzleHeight,
  });

  factory CaptchaChallenge.fromJson(Map<String, dynamic> json) {
    return CaptchaChallenge(
      token: json['token'] as String,
      backgroundImage: json['backgroundImage'] as String,
      puzzleImage: json['puzzleImage'] as String,
      puzzleWidth: json['puzzleWidth'] as int,
      puzzleHeight: json['puzzleHeight'] as int,
    );
  }
}
