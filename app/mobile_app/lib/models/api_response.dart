import 'package:json_annotation/json_annotation.dart';

part 'api_response.g.dart';

@JsonSerializable(genericArgumentFactories: true)
class ApiResponse<T> {
  final int? code;
  final int? errorCode;
  final String? message;
  final String? detail;
  final T? data;
  final String? requestId;
  final String? timestamp;

  ApiResponse({
    this.code,
    this.errorCode,
    this.message,
    this.detail,
    this.data,
    this.requestId,
    this.timestamp,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) => _$ApiResponseFromJson(json, fromJsonT);

  Map<String, dynamic> toJson(Object? Function(T value) toJsonT) =>
      _$ApiResponseToJson(this, toJsonT);
}
