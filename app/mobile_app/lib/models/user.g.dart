// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UserDto _$UserDtoFromJson(Map<String, dynamic> json) => UserDto(
  id: json['id'] as String,
  email: json['email'] as String?,
  phone: json['phone'] as String?,
  name: json['name'] as String?,
  role: json['role'] as String?,
  avatar: json['avatar'] as String?,
  info: json['info'] as String?,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  lastLoginAt: json['lastLoginAt'] == null
      ? null
      : DateTime.parse(json['lastLoginAt'] as String),
  isActive: json['isActive'] as bool?,
  isEmailVerified: json['isEmailVerified'] as bool?,
  isPhoneVerified: json['isPhoneVerified'] as bool?,
  pushSettings: json['pushSettings'] as String?,
  githubId: json['githubId'] as String?,
  googleId: json['googleId'] as String?,
  weixinId: json['weixinId'] as String?,
  oursId: json['oursId'] as String?,
);

Map<String, dynamic> _$UserDtoToJson(UserDto instance) => <String, dynamic>{
  'id': instance.id,
  'email': instance.email,
  'phone': instance.phone,
  'name': instance.name,
  'role': instance.role,
  'avatar': instance.avatar,
  'info': instance.info,
  'createdAt': instance.createdAt?.toIso8601String(),
  'lastLoginAt': instance.lastLoginAt?.toIso8601String(),
  'isActive': instance.isActive,
  'isEmailVerified': instance.isEmailVerified,
  'isPhoneVerified': instance.isPhoneVerified,
  'pushSettings': instance.pushSettings,
  'githubId': instance.githubId,
  'googleId': instance.googleId,
  'weixinId': instance.weixinId,
  'oursId': instance.oursId,
};

UserSimpleDto _$UserSimpleDtoFromJson(Map<String, dynamic> json) =>
    UserSimpleDto(
      id: json['id'] as String?,
      name: json['name'] as String?,
      avatar: json['avatar'] as String?,
    );

Map<String, dynamic> _$UserSimpleDtoToJson(UserSimpleDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'avatar': instance.avatar,
    };

UserCreateDto _$UserCreateDtoFromJson(Map<String, dynamic> json) =>
    UserCreateDto(
      account: json['account'] as String,
      password: json['password'] as String,
      name: json['name'] as String,
      avatar: json['avatar'] as String?,
      role: json['role'] as String?,
    );

Map<String, dynamic> _$UserCreateDtoToJson(UserCreateDto instance) =>
    <String, dynamic>{
      'account': instance.account,
      'password': instance.password,
      'name': instance.name,
      'avatar': instance.avatar,
      'role': instance.role,
    };

UserUpdateDto _$UserUpdateDtoFromJson(Map<String, dynamic> json) =>
    UserUpdateDto(
      name: json['name'] as String?,
      email: json['email'] as String?,
      phone: json['phone'] as String?,
      avatar: json['avatar'] as String?,
      info: json['info'] as String?,
      githubId: json['githubId'] as String?,
      googleId: json['googleId'] as String?,
      weixinId: json['weixinId'] as String?,
      oursId: json['oursId'] as String?,
    );

Map<String, dynamic> _$UserUpdateDtoToJson(UserUpdateDto instance) =>
    <String, dynamic>{
      'name': instance.name,
      'email': instance.email,
      'phone': instance.phone,
      'avatar': instance.avatar,
      'info': instance.info,
      'githubId': instance.githubId,
      'googleId': instance.googleId,
      'weixinId': instance.weixinId,
      'oursId': instance.oursId,
    };
