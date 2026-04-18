import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';

@JsonSerializable()
class UserDto {
  final String id;
  final String? email;
  final String? phone;
  final String? name;
  final String? role;
  final String? avatar;
  final String? info;
  final DateTime? createdAt;
  final DateTime? lastLoginAt;
  final bool? isActive;
  final bool? isEmailVerified;
  final bool? isPhoneVerified;
  final String? pushSettings;
  final String? githubId;
  final String? googleId;
  final String? weixinId;
  final String? oursId;

  UserDto({
    required this.id,
    this.email,
    this.phone,
    this.name,
    this.role,
    this.avatar,
    this.info,
    this.createdAt,
    this.lastLoginAt,
    this.isActive,
    this.isEmailVerified,
    this.isPhoneVerified,
    this.pushSettings,
    this.githubId,
    this.googleId,
    this.weixinId,
    this.oursId,
  });

  factory UserDto.fromJson(Map<String, dynamic> json) =>
      _$UserDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserDtoToJson(this);
}

@JsonSerializable()
class UserSimpleDto {
  final String? id;
  final String? name;
  final String? avatar;

  UserSimpleDto({this.id, this.name, this.avatar});

  factory UserSimpleDto.fromJson(Map<String, dynamic> json) =>
      _$UserSimpleDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserSimpleDtoToJson(this);
}

@JsonSerializable()
class UserCreateDto {
  final String account;
  final String password;
  final String name;
  final String? avatar;
  final String? role;

  UserCreateDto({
    required this.account,
    required this.password,
    required this.name,
    this.avatar,
    this.role,
  });

  factory UserCreateDto.fromJson(Map<String, dynamic> json) =>
      _$UserCreateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserCreateDtoToJson(this);
}

@JsonSerializable()
class UserUpdateDto {
  final String? name;
  final String? email;
  final String? phone;
  final String? avatar;
  final String? info;
  final String? githubId;
  final String? googleId;
  final String? weixinId;
  final String? oursId;

  UserUpdateDto({
    this.name,
    this.email,
    this.phone,
    this.avatar,
    this.info,
    this.githubId,
    this.googleId,
    this.weixinId,
    this.oursId,
  });

  factory UserUpdateDto.fromJson(Map<String, dynamic> json) =>
      _$UserUpdateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserUpdateDtoToJson(this);
}
