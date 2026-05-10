import 'package:json_annotation/json_annotation.dart';

part 'like_toggle_result.g.dart';

@JsonSerializable()
class LikeToggleResult {
  final bool isLiked;
  final bool isDisliked;
  final int likeCount;
  final int dislikeCount;

  LikeToggleResult({
    required this.isLiked,
    required this.isDisliked,
    required this.likeCount,
    required this.dislikeCount,
  });

  factory LikeToggleResult.fromJson(Map<String, dynamic> json) =>
      _$LikeToggleResultFromJson(json);
  Map<String, dynamic> toJson() => _$LikeToggleResultToJson(this);
}
