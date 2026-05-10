// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'like_toggle_result.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

LikeToggleResult _$LikeToggleResultFromJson(Map<String, dynamic> json) =>
    LikeToggleResult(
      isLiked: json['isLiked'] as bool,
      isDisliked: json['isDisliked'] as bool,
      likeCount: (json['likeCount'] as num).toInt(),
      dislikeCount: (json['dislikeCount'] as num).toInt(),
    );

Map<String, dynamic> _$LikeToggleResultToJson(LikeToggleResult instance) =>
    <String, dynamic>{
      'isLiked': instance.isLiked,
      'isDisliked': instance.isDisliked,
      'likeCount': instance.likeCount,
      'dislikeCount': instance.dislikeCount,
    };
