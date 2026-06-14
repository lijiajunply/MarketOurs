// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'post.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

PostTagDto _$PostTagDtoFromJson(Map<String, dynamic> json) => PostTagDto(
  id: json['id'] as String,
  name: json['name'] as String?,
  color: json['color'] as String?,
  isActive: json['isActive'] as bool?,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$PostTagDtoToJson(PostTagDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'color': instance.color,
      'isActive': instance.isActive,
      'createdAt': instance.createdAt?.toIso8601String(),
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };

PostDto _$PostDtoFromJson(Map<String, dynamic> json) => PostDto(
  id: json['id'] as String,
  title: json['title'] as String?,
  content: json['content'] as String?,
  images: (json['images'] as List<dynamic>?)?.map((e) => e as String).toList(),
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
  userId: json['userId'] as String?,
  author: json['author'] == null
      ? null
      : UserSimpleDto.fromJson(json['author'] as Map<String, dynamic>),
  tagId: json['tagId'] as String?,
  tag: json['tag'] == null
      ? null
      : PostTagDto.fromJson(json['tag'] as Map<String, dynamic>),
  likes: (json['likes'] as num?)?.toInt(),
  dislikes: (json['dislikes'] as num?)?.toInt(),
  isLiked: json['isLiked'] as bool?,
  isDisliked: json['isDisliked'] as bool?,
  watch: (json['watch'] as num?)?.toInt(),
  commentsCount: (json['commentsCount'] as num?)?.toInt(),
  isReview: json['isReview'] as bool?,
);

Map<String, dynamic> _$PostDtoToJson(PostDto instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'content': instance.content,
  'images': instance.images,
  'createdAt': instance.createdAt?.toIso8601String(),
  'updatedAt': instance.updatedAt?.toIso8601String(),
  'userId': instance.userId,
  'author': instance.author,
  'tagId': instance.tagId,
  'tag': instance.tag,
  'likes': instance.likes,
  'dislikes': instance.dislikes,
  'isLiked': instance.isLiked,
  'isDisliked': instance.isDisliked,
  'watch': instance.watch,
  'commentsCount': instance.commentsCount,
  'isReview': instance.isReview,
};

PostCreateDto _$PostCreateDtoFromJson(Map<String, dynamic> json) =>
    PostCreateDto(
      title: json['title'] as String,
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      userId: json['userId'] as String,
      uploadKey: json['uploadKey'] as String?,
      tagId: json['tagId'] as String?,
    );

Map<String, dynamic> _$PostCreateDtoToJson(PostCreateDto instance) =>
    <String, dynamic>{
      'title': instance.title,
      'content': instance.content,
      'images': instance.images,
      'userId': instance.userId,
      'uploadKey': instance.uploadKey,
      'tagId': instance.tagId,
    };

PostUpdateDto _$PostUpdateDtoFromJson(Map<String, dynamic> json) =>
    PostUpdateDto(
      title: json['title'] as String,
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      isReview: json['isReview'] as bool?,
      uploadKey: json['uploadKey'] as String?,
      tagId: json['tagId'] as String?,
    );

Map<String, dynamic> _$PostUpdateDtoToJson(PostUpdateDto instance) =>
    <String, dynamic>{
      'title': instance.title,
      'content': instance.content,
      'images': instance.images,
      'isReview': instance.isReview,
      'uploadKey': instance.uploadKey,
      'tagId': instance.tagId,
    };
