import 'package:json_annotation/json_annotation.dart';
import 'user.dart';

part 'post.g.dart';

@JsonSerializable()
class PostDto {
  final String id;
  final String? title;
  final String? content;
  final List<String>? images;
  final DateTime? createdAt;
  final DateTime? updatedAt;
  final String? userId;
  final UserSimpleDto? author;
  final int? likes;
  final int? dislikes;
  final int? watch;
  final int? commentsCount;
  final bool? isReview;

  PostDto({
    required this.id,
    this.title,
    this.content,
    this.images,
    this.createdAt,
    this.updatedAt,
    this.userId,
    this.author,
    this.likes,
    this.dislikes,
    this.watch,
    this.commentsCount,
    this.isReview,
  });

  factory PostDto.fromJson(Map<String, dynamic> json) =>
      _$PostDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PostDtoToJson(this);
}

@JsonSerializable()
class PostCreateDto {
  final String title;
  final String content;
  final List<String>? images;
  final String userId;

  PostCreateDto({
    required this.title,
    required this.content,
    this.images,
    required this.userId,
  });

  factory PostCreateDto.fromJson(Map<String, dynamic> json) =>
      _$PostCreateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PostCreateDtoToJson(this);
}

@JsonSerializable()
class PostUpdateDto {
  final String title;
  final String content;
  final List<String>? images;
  final bool? isReview;

  PostUpdateDto({
    required this.title,
    required this.content,
    this.images,
    this.isReview,
  });

  factory PostUpdateDto.fromJson(Map<String, dynamic> json) =>
      _$PostUpdateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PostUpdateDtoToJson(this);
}
