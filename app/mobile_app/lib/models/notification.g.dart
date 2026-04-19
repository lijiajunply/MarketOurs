// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'notification.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

NotificationDto _$NotificationDtoFromJson(Map<String, dynamic> json) =>
    NotificationDto(
      id: json['id'] as String,
      userId: json['userId'] as String,
      title: json['title'] as String,
      content: json['content'] as String,
      type: $enumDecode(
        _$NotificationTypeEnumMap,
        json['type'],
        unknownValue: NotificationType.unknown,
      ),
      targetId: json['targetId'] as String?,
      isRead: json['isRead'] as bool,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$NotificationDtoToJson(NotificationDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'title': instance.title,
      'content': instance.content,
      'type': _$NotificationTypeEnumMap[instance.type]!,
      'targetId': instance.targetId,
      'isRead': instance.isRead,
      'createdAt': instance.createdAt.toIso8601String(),
    };

const _$NotificationTypeEnumMap = {
  NotificationType.commentReply: 0,
  NotificationType.postReply: 1,
  NotificationType.hotList: 2,
  NotificationType.system: 3,
  NotificationType.review: 4,
  NotificationType.unknown: -1,
};

PushSettingsDto _$PushSettingsDtoFromJson(Map<String, dynamic> json) =>
    PushSettingsDto(
      enableEmailNotifications: json['enableEmailNotifications'] as bool,
      enableHotListPush: json['enableHotListPush'] as bool,
      enableCommentReplyPush: json['enableCommentReplyPush'] as bool,
    );

Map<String, dynamic> _$PushSettingsDtoToJson(PushSettingsDto instance) =>
    <String, dynamic>{
      'enableEmailNotifications': instance.enableEmailNotifications,
      'enableHotListPush': instance.enableHotListPush,
      'enableCommentReplyPush': instance.enableCommentReplyPush,
    };
