import 'package:json_annotation/json_annotation.dart';

part 'notification.g.dart';

enum NotificationType {
  @JsonValue(0)
  commentReply,
  @JsonValue(1)
  postReply,
  @JsonValue(2)
  hotList,
  @JsonValue(3)
  system,
}

@JsonSerializable()
class NotificationDto {
  final String id;
  final String userId;
  final String title;
  final String content;
  final NotificationType type;
  final String? targetId;
  final bool isRead;
  final DateTime createdAt;

  NotificationDto({
    required this.id,
    required this.userId,
    required this.title,
    required this.content,
    required this.type,
    this.targetId,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationDto.fromJson(Map<String, dynamic> json) =>
      _$NotificationDtoFromJson(json);
  Map<String, dynamic> toJson() => _$NotificationDtoToJson(this);
}

@JsonSerializable()
class PushSettingsDto {
  final bool enableEmailNotifications;
  final bool enableHotListPush;
  final bool enableCommentReplyPush;

  PushSettingsDto({
    required this.enableEmailNotifications,
    required this.enableHotListPush,
    required this.enableCommentReplyPush,
  });

  factory PushSettingsDto.fromJson(Map<String, dynamic> json) =>
      _$PushSettingsDtoFromJson(json);
  Map<String, dynamic> toJson() => _$PushSettingsDtoToJson(this);
}
