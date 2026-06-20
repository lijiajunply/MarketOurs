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
  @JsonValue(4)
  review,
  @JsonValue(-1)
  unknown,
}

@JsonSerializable()
class NotificationDto {
  final String id;
  final String userId;
  final String title;
  final String content;
  @JsonKey(unknownEnumValue: NotificationType.unknown)
  final NotificationType type;
  final String? targetId;
  final bool isRead;
  final DateTime createdAt;
  final Map<String, dynamic>? params;

  NotificationDto({
    required this.id,
    required this.userId,
    required this.title,
    required this.content,
    required this.type,
    this.targetId,
    required this.isRead,
    required this.createdAt,
    this.params,
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

sealed class NotificationParams {
  const NotificationParams();

  factory NotificationParams.fromJson(Map<String, dynamic> json) {
    final type = json['\$type'] as String? ?? '';
    return switch (type) {
      'commentReply' => CommentReplyParams.fromJson(json),
      'postReply' => PostReplyParams.fromJson(json),
      'hotList' => HotListParams.fromJson(json),
      'review' => ReviewParams.fromJson(json),
      'system' => SystemParams.fromJson(json),
      _ => throw FormatException('Unknown NotificationParams \$type: $type'),
    };
  }

  Map<String, dynamic> toJson();
}

class CommentReplyParams extends NotificationParams {
  final String commenterName;
  final String bodySnippet;

  const CommentReplyParams({required this.commenterName, required this.bodySnippet});

  factory CommentReplyParams.fromJson(Map<String, dynamic> json) => CommentReplyParams(
        commenterName: json['commenterName'] as String? ?? '',
        bodySnippet: json['bodySnippet'] as String? ?? '',
      );

  @override
  Map<String, dynamic> toJson() => {
        r'$type': 'commentReply',
        'commenterName': commenterName,
        'bodySnippet': bodySnippet,
      };
}

class PostReplyParams extends NotificationParams {
  final String commenterName;
  final String bodySnippet;

  const PostReplyParams({required this.commenterName, required this.bodySnippet});

  factory PostReplyParams.fromJson(Map<String, dynamic> json) => PostReplyParams(
        commenterName: json['commenterName'] as String? ?? '',
        bodySnippet: json['bodySnippet'] as String? ?? '',
      );

  @override
  Map<String, dynamic> toJson() => {
        r'$type': 'postReply',
        'commenterName': commenterName,
        'bodySnippet': bodySnippet,
      };
}

class HotListPost {
  final String id;
  final String title;

  const HotListPost({required this.id, required this.title});

  factory HotListPost.fromJson(Map<String, dynamic> json) =>
      HotListPost(id: json['id'] as String? ?? '', title: json['title'] as String? ?? '');

  Map<String, dynamic> toJson() => {'id': id, 'title': title};
}

class HotListParams extends NotificationParams {
  final String header;
  final List<HotListPost> posts;

  const HotListParams({required this.header, required this.posts});

  factory HotListParams.fromJson(Map<String, dynamic> json) => HotListParams(
        header: json['header'] as String? ?? '',
        posts: (json['posts'] as List<dynamic>?)
                ?.map((e) => HotListPost.fromJson(e as Map<String, dynamic>))
                .toList() ??
            [],
      );

  @override
  Map<String, dynamic> toJson() => {
        r'$type': 'hotList',
        'header': header,
        'posts': posts.map((p) => p.toJson()).toList(),
      };
}

class ReviewParams extends NotificationParams {
  final String entityType;
  final String name;
  final bool approved;
  final String? reason;

  const ReviewParams({
    required this.entityType,
    required this.name,
    required this.approved,
    this.reason,
  });

  factory ReviewParams.fromJson(Map<String, dynamic> json) => ReviewParams(
        entityType: json['entityType'] as String? ?? 'post',
        name: json['name'] as String? ?? '',
        approved: json['approved'] as bool? ?? true,
        reason: json['reason'] as String?,
      );

  @override
  Map<String, dynamic> toJson() => {
        r'$type': 'review',
        'entityType': entityType,
        'name': name,
        'approved': approved,
        if (reason != null) 'reason': reason,
      };
}

class SystemParams extends NotificationParams {
  final String message;

  const SystemParams({required this.message});

  factory SystemParams.fromJson(Map<String, dynamic> json) =>
      SystemParams(message: json['message'] as String? ?? '');

  @override
  Map<String, dynamic> toJson() => {
        r'$type': 'system',
        'message': message,
      };
}

extension NotificationDtoParams on NotificationDto {
  NotificationParams? get typedParams {
    if (params == null) return null;
    try {
      return NotificationParams.fromJson(params!);
    } catch (_) {
      return null;
    }
  }
}
