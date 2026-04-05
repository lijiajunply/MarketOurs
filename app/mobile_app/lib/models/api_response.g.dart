// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'api_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ApiResponse<T> _$ApiResponseFromJson<T>(
  Map<String, dynamic> json,
  T Function(Object? json) fromJsonT,
) => ApiResponse<T>(
  code: (json['code'] as num?)?.toInt(),
  errorCode: (json['errorCode'] as num?)?.toInt(),
  message: json['message'] as String?,
  detail: json['detail'] as String?,
  data: _$nullableGenericFromJson(json['data'], fromJsonT),
  requestId: json['requestId'] as String?,
  timestamp: json['timestamp'] as String?,
);

Map<String, dynamic> _$ApiResponseToJson<T>(
  ApiResponse<T> instance,
  Object? Function(T value) toJsonT,
) => <String, dynamic>{
  'code': instance.code,
  'errorCode': instance.errorCode,
  'message': instance.message,
  'detail': instance.detail,
  'data': _$nullableGenericToJson(instance.data, toJsonT),
  'requestId': instance.requestId,
  'timestamp': instance.timestamp,
};

T? _$nullableGenericFromJson<T>(
  Object? input,
  T Function(Object? json) fromJson,
) => input == null ? null : fromJson(input);

Object? _$nullableGenericToJson<T>(
  T? input,
  Object? Function(T value) toJson,
) => input == null ? null : toJson(input);
