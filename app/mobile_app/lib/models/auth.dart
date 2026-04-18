import 'package:json_annotation/json_annotation.dart';

part 'auth.g.dart';

@JsonSerializable()
class TokenDto {
  final String? accessToken;
  final String? refreshToken;

  TokenDto({this.accessToken, this.refreshToken});

  factory TokenDto.fromJson(Map<String, dynamic> json) =>
      _$TokenDtoFromJson(json);

  Map<String, dynamic> toJson() => _$TokenDtoToJson(this);
}

@JsonSerializable()
class LoginRequest {
  final String account;
  final String password;
  final String? deviceType;

  LoginRequest({
    required this.account,
    required this.password,
    this.deviceType,
  });

  factory LoginRequest.fromJson(Map<String, dynamic> json) =>
      _$LoginRequestFromJson(json);

  Map<String, dynamic> toJson() => _$LoginRequestToJson(this);
}

@JsonSerializable()
class RefreshRequest {
  final String refreshToken;
  final String? deviceType;

  RefreshRequest({required this.refreshToken, this.deviceType});

  factory RefreshRequest.fromJson(Map<String, dynamic> json) =>
      _$RefreshRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RefreshRequestToJson(this);
}

@JsonSerializable()
class ForgotPasswordRequest {
  final String account;

  ForgotPasswordRequest({required this.account});

  factory ForgotPasswordRequest.fromJson(Map<String, dynamic> json) =>
      _$ForgotPasswordRequestFromJson(json);

  Map<String, dynamic> toJson() => _$ForgotPasswordRequestToJson(this);
}

@JsonSerializable()
class ResetPasswordRequest {
  final String token;
  final String newPassword;

  ResetPasswordRequest({required this.token, required this.newPassword});

  factory ResetPasswordRequest.fromJson(Map<String, dynamic> json) =>
      _$ResetPasswordRequestFromJson(json);

  Map<String, dynamic> toJson() => _$ResetPasswordRequestToJson(this);
}

@JsonSerializable()
class VerifyCodeRequest {
  final String code;

  VerifyCodeRequest({required this.code});

  factory VerifyCodeRequest.fromJson(Map<String, dynamic> json) =>
      _$VerifyCodeRequestFromJson(json);

  Map<String, dynamic> toJson() => _$VerifyCodeRequestToJson(this);
}

@JsonSerializable()
class VerifyRegistrationRequest {
  final String registrationToken;
  final String code;

  VerifyRegistrationRequest({
    required this.registrationToken,
    required this.code,
  });

  factory VerifyRegistrationRequest.fromJson(Map<String, dynamic> json) =>
      _$VerifyRegistrationRequestFromJson(json);

  Map<String, dynamic> toJson() => _$VerifyRegistrationRequestToJson(this);
}

@JsonSerializable()
class SendCodeRequest {
  final String account;

  SendCodeRequest({required this.account});

  factory SendCodeRequest.fromJson(Map<String, dynamic> json) =>
      _$SendCodeRequestFromJson(json);

  Map<String, dynamic> toJson() => _$SendCodeRequestToJson(this);
}

@JsonSerializable()
class LoginByCodeRequest {
  final String account;
  final String code;
  final String? deviceType;
  LoginByCodeRequest({
    required this.account,
    required this.code,
    this.deviceType,
  });

  factory LoginByCodeRequest.fromJson(Map<String, dynamic> json) =>
      _$LoginByCodeRequestFromJson(json);

  Map<String, dynamic> toJson() => _$LoginByCodeRequestToJson(this);
}
