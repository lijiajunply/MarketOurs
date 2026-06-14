import 'package:json_annotation/json_annotation.dart';
import 'user.dart';

part 'post.g.dart';

@JsonSerializable()
class PostTagDto {
  final String id;
  final String? name;
  final String? color;
  final bool? isActive;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  PostTagDto({
    required this.id,
    this.name,
    this.color,
    this.isActive,
    this.createdAt,
    this.updatedAt,
  });

  factory PostTagDto.fromJson(Map<String, dynamic> json) =>
      _$PostTagDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PostTagDtoToJson(this);
}

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
  final String? tagId;
  final PostTagDto? tag;
  final int? likes;
  final int? dislikes;
  final bool? isLiked;
  final bool? isDisliked;
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
    this.tagId,
    this.tag,
    this.likes,
    this.dislikes,
    this.isLiked,
    this.isDisliked,
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
  final String? uploadKey;
  final String? tagId;

  PostCreateDto({
    required this.title,
    required this.content,
    this.images,
    required this.userId,
    this.uploadKey,
    this.tagId,
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
  final String? uploadKey;
  final String? tagId;

  PostUpdateDto({
    required this.title,
    required this.content,
    this.images,
    this.isReview,
    this.uploadKey,
    this.tagId,
  });

  factory PostUpdateDto.fromJson(Map<String, dynamic> json) =>
      _$PostUpdateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PostUpdateDtoToJson(this);
}
