import 'package:json_annotation/json_annotation.dart';

part 'comment.g.dart';

@JsonSerializable()
class CommentDto {
  final String id;
  final String? content;
  final List<String>? images;
  final int? likes;
  final int? dislikes;
  final DateTime? createdAt;
  final DateTime? updatedAt;
  final String? userId;
  final String? postId;
  final String? parentCommentId;
  final List<CommentDto>? repliedComments;

  CommentDto({
    required this.id,
    this.content,
    this.images,
    this.likes,
    this.dislikes,
    this.createdAt,
    this.updatedAt,
    this.userId,
    this.postId,
    this.parentCommentId,
    this.repliedComments,
  });

  factory CommentDto.fromJson(Map<String, dynamic> json) => _$CommentDtoFromJson(json);
  Map<String, dynamic> toJson() => _$CommentDtoToJson(this);
}

@JsonSerializable()
class CommentCreateDto {
  final String content;
  final List<String>? images;
  final String userId;
  final String postId;
  final String? parentCommentId;

  CommentCreateDto({
    required this.content,
    this.images,
    required this.userId,
    required this.postId,
    this.parentCommentId,
  });

  factory CommentCreateDto.fromJson(Map<String, dynamic> json) => _$CommentCreateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$CommentCreateDtoToJson(this);
}

@JsonSerializable()
class CommentUpdateDto {
  final String content;
  final List<String>? images;

  CommentUpdateDto({
    required this.content,
    this.images,
  });

  factory CommentUpdateDto.fromJson(Map<String, dynamic> json) => _$CommentUpdateDtoFromJson(json);
  Map<String, dynamic> toJson() => _$CommentUpdateDtoToJson(this);
}
