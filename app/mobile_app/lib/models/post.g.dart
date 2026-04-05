// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'post.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

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
  likes: (json['likes'] as num?)?.toInt(),
  dislikes: (json['dislikes'] as num?)?.toInt(),
  watch: (json['watch'] as num?)?.toInt(),
);

Map<String, dynamic> _$PostDtoToJson(PostDto instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'content': instance.content,
  'images': instance.images,
  'createdAt': instance.createdAt?.toIso8601String(),
  'updatedAt': instance.updatedAt?.toIso8601String(),
  'userId': instance.userId,
  'likes': instance.likes,
  'dislikes': instance.dislikes,
  'watch': instance.watch,
};

PostCreateDto _$PostCreateDtoFromJson(Map<String, dynamic> json) =>
    PostCreateDto(
      title: json['title'] as String,
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      userId: json['userId'] as String,
    );

Map<String, dynamic> _$PostCreateDtoToJson(PostCreateDto instance) =>
    <String, dynamic>{
      'title': instance.title,
      'content': instance.content,
      'images': instance.images,
      'userId': instance.userId,
    };

PostUpdateDto _$PostUpdateDtoFromJson(Map<String, dynamic> json) =>
    PostUpdateDto(
      title: json['title'] as String,
      content: json['content'] as String,
      images: (json['images'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
    );

Map<String, dynamic> _$PostUpdateDtoToJson(PostUpdateDto instance) =>
    <String, dynamic>{
      'title': instance.title,
      'content': instance.content,
      'images': instance.images,
    };
