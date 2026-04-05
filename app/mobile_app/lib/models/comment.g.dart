// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'comment.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CommentDto _$CommentDtoFromJson(Map<String, dynamic> json) => CommentDto(
  id: json['id'] as String,
  content: json['content'] as String?,
  images: (json['images'] as List<dynamic>?)?.map((e) => e as String).toList(),
  likes: (json['likes'] as num?)?.toInt(),
  dislikes: (json['dislikes'] as num?)?.toInt(),
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
  userId: json['userId'] as String?,
  postId: json['postId'] as String?,
  parentCommentId: json['parentCommentId'] as String?,
  repliedComments: (json['repliedComments'] as List<dynamic>?)
      ?.map((e) => CommentDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);

Map<String, dynamic> _$CommentDtoToJson(CommentDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'content': instance.content,
      'images': instance.images,
      'likes': instance.likes,
      'dislikes': instance.dislikes,
      'createdAt': instance.createdAt?.toIso8601String(),
      'updatedAt': instance.updatedAt?.toIso8601String(),
      'userId': instance.userId,
      'postId': instance.postId,
      'parentCommentId': instance.parentCommentId,
      'repliedComments': instance.repliedComments,
    };

CommentCreateDto _$CommentCreateDtoFromJson(Map<String, dynamic> json) =>
    CommentCreateDto(
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      userId: json['userId'] as String,
      postId: json['postId'] as String,
      parentCommentId: json['parentCommentId'] as String?,
    );

Map<String, dynamic> _$CommentCreateDtoToJson(CommentCreateDto instance) =>
    <String, dynamic>{
      'content': instance.content,
      'images': instance.images,
      'userId': instance.userId,
      'postId': instance.postId,
      'parentCommentId': instance.parentCommentId,
    };

CommentUpdateDto _$CommentUpdateDtoFromJson(Map<String, dynamic> json) =>
    CommentUpdateDto(
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
    );

Map<String, dynamic> _$CommentUpdateDtoToJson(CommentUpdateDto instance) =>
    <String, dynamic>{'content': instance.content, 'images': instance.images};
