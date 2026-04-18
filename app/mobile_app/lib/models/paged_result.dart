import 'package:json_annotation/json_annotation.dart';

part 'paged_result.g.dart';

@JsonSerializable(genericArgumentFactories: true)
class PagedResult<T> {
  final List<T> items;
  final int totalCount;
  final int pageIndex;
  final int pageSize;
  final int totalPages;
  final bool hasPreviousPage;
  final bool hasNextPage;

  PagedResult({
    required this.items,
    required this.totalCount,
    required this.pageIndex,
    required this.pageSize,
    required this.totalPages,
    required this.hasPreviousPage,
    required this.hasNextPage,
  });

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) => _$PagedResultFromJson(json, fromJsonT);

  Map<String, dynamic> toJson(Object? Function(T value) toJsonT) =>
      _$PagedResultToJson(this, toJsonT);
}
