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
  });

  factory UserDto.fromJson(Map<String, dynamic> json) => _$UserDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserDtoToJson(this);
}

@JsonSerializable()
class UserCreateDto {
  final String account;
  final String password;
  final String name;
  final String? role;

  UserCreateDto({
    required this.account,
    required this.password,
    required this.name,
    this.role,
  });

  factory UserCreateDto.fromJson(Map<String, dynamic> json) => _$UserCreateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserCreateDtoToJson(this);
}

@JsonSerializable()
class UserUpdateDto {
  final String? name;
  final String? avatar;
  final String? info;

  UserUpdateDto({
    this.name,
    this.avatar,
    this.info,
  });

  factory UserUpdateDto.fromJson(Map<String, dynamic> json) => _$UserUpdateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$UserUpdateDtoToJson(this);
}
