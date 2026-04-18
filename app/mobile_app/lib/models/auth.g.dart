// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'auth.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TokenDto _$TokenDtoFromJson(Map<String, dynamic> json) => TokenDto(
  accessToken: json['accessToken'] as String?,
  refreshToken: json['refreshToken'] as String?,
);

Map<String, dynamic> _$TokenDtoToJson(TokenDto instance) => <String, dynamic>{
  'accessToken': instance.accessToken,
  'refreshToken': instance.refreshToken,
};

LoginRequest _$LoginRequestFromJson(Map<String, dynamic> json) => LoginRequest(
  account: json['account'] as String,
  password: json['password'] as String,
  deviceType: json['deviceType'] as String?,
);

Map<String, dynamic> _$LoginRequestToJson(LoginRequest instance) =>
    <String, dynamic>{
      'account': instance.account,
      'password': instance.password,
      'deviceType': instance.deviceType,
    };

RefreshRequest _$RefreshRequestFromJson(Map<String, dynamic> json) =>
    RefreshRequest(
      refreshToken: json['refreshToken'] as String,
      deviceType: json['deviceType'] as String?,
    );

Map<String, dynamic> _$RefreshRequestToJson(RefreshRequest instance) =>
    <String, dynamic>{
      'refreshToken': instance.refreshToken,
      'deviceType': instance.deviceType,
    };

ForgotPasswordRequest _$ForgotPasswordRequestFromJson(
  Map<String, dynamic> json,
) => ForgotPasswordRequest(account: json['account'] as String);

Map<String, dynamic> _$ForgotPasswordRequestToJson(
  ForgotPasswordRequest instance,
) => <String, dynamic>{'account': instance.account};

ResetPasswordRequest _$ResetPasswordRequestFromJson(
  Map<String, dynamic> json,
) => ResetPasswordRequest(
  token: json['token'] as String,
  newPassword: json['newPassword'] as String,
);

Map<String, dynamic> _$ResetPasswordRequestToJson(
  ResetPasswordRequest instance,
) => <String, dynamic>{
  'token': instance.token,
  'newPassword': instance.newPassword,
};

VerifyCodeRequest _$VerifyCodeRequestFromJson(Map<String, dynamic> json) =>
    VerifyCodeRequest(code: json['code'] as String);

Map<String, dynamic> _$VerifyCodeRequestToJson(VerifyCodeRequest instance) =>
    <String, dynamic>{'code': instance.code};

VerifyRegistrationRequest _$VerifyRegistrationRequestFromJson(
  Map<String, dynamic> json,
) => VerifyRegistrationRequest(
  registrationToken: json['registrationToken'] as String,
  code: json['code'] as String,
);

Map<String, dynamic> _$VerifyRegistrationRequestToJson(
  VerifyRegistrationRequest instance,
) => <String, dynamic>{
  'registrationToken': instance.registrationToken,
  'code': instance.code,
};

SendCodeRequest _$SendCodeRequestFromJson(Map<String, dynamic> json) =>
    SendCodeRequest(account: json['account'] as String);

Map<String, dynamic> _$SendCodeRequestToJson(SendCodeRequest instance) =>
    <String, dynamic>{'account': instance.account};

LoginByCodeRequest _$LoginByCodeRequestFromJson(Map<String, dynamic> json) =>
    LoginByCodeRequest(
      account: json['account'] as String,
      code: json['code'] as String,
      deviceType: json['deviceType'] as String?,
    );

Map<String, dynamic> _$LoginByCodeRequestToJson(LoginByCodeRequest instance) =>
    <String, dynamic>{
      'account': instance.account,
      'code': instance.code,
      'deviceType': instance.deviceType,
    };
