import 'package:dio/dio.dart';

/// 后端业务错误码 → 用户可读消息映射。
/// 与后端 ErrorCode.cs 保持同步。客户端应基于 errorCode（而非 message）做程序化错误处理。
///
/// 错误码分段：
///   0           成功
///   1000-1099   通用/参数错误
///   1100-1199   通用业务错误
///   2000-2099   认证错误
///   2100-2199   权限错误
///   3000-3099   用户错误
///   4000-4099   帖子错误
///   4100-4199   评论错误
///   5000-5099   关注/屏蔽错误
///   6000-6099   文件错误
///   7000-7099   点赞错误
///   8000-8099   系统/基础设施错误
///   8100-8199   外部服务错误
///   9000-9099   平台/限流错误
const Map<int, String> _errorCodeMessages = {
  // 成功
  0: '操作成功',

  // 通用/参数错误 (1000-1099)
  1000: '参数不能为空',
  1001: '参数格式不正确',
  1002: '参数超出允许范围',
  1003: '参数验证失败',
  1004: '请求体缺失',
  1005: '不支持的 Content-Type',

  // 通用业务错误 (1100-1199)
  1100: '资源已存在',
  1101: '操作失败',
  1102: '数据处理失败',
  1103: '当前状态不允许执行此操作',
  1104: '操作过于频繁，请稍后重试',
  1105: '资源已过期',

  // 认证错误 (2000-2099)
  2000: '请先登录',
  2001: '令牌无效',
  2002: '令牌已过期',
  2003: '登录已过期，请重新登录',
  2004: '刷新令牌无效或已过期',
  2005: '用户名或密码错误',
  2006: 'OAuth 授权码无效',
  2007: '不支持的第三方登录方式',
  2008: '验证码无效或已过期',
  2009: '注册会话已过期，请重新开始',
  2010: '第三方账号未绑定本地账户',
  2011: '该第三方账号已被其他账户绑定',
  2012: '未找到关联账户，请先登录并绑定',

  // 权限错误 (2100-2199)
  2100: '权限不足',
  2101: '无权修改他人的帖子',
  2102: '无权删除他人的帖子',
  2103: '无权修改他人的评论',
  2104: '无权删除他人的评论',
  2105: '账号已被禁用',
  2106: '账号尚未激活',

  // 用户错误 (3000-3099)
  3000: '用户不存在',
  3001: '用户未绑定邮箱',
  3002: '用户未绑定手机号',
  3003: '该账号已存在',
  3004: '邮箱已被注册',
  3005: '旧密码错误',
  3006: '密码验证失败',
  3007: '不能操作自己的账号',
  3008: '该账号未绑定邮箱或手机号，无法接收重置验证码',
  3009: '不支持的验证方式',
  3010: '不支持的第三方平台',
  3011: '该第三方账号尚未绑定',

  // 帖子错误 (4000-4099)
  4000: '帖子不存在',
  4001: '帖子创建失败',
  4002: '帖子更新失败',

  // 评论错误 (4100-4199)
  4100: '评论不存在',
  4101: '要回复的评论不存在',
  4102: '评论创建失败',
  4103: '评论更新失败',

  // 关注/屏蔽错误 (5000-5099)
  5000: '不能关注自己',
  5001: '不能屏蔽自己',
  5002: '无法关注已屏蔽或屏蔽您的用户',
  5003: '关注操作过于频繁，请稍后重试',
  5004: '屏蔽操作过于频繁，请稍后重试',

  // 文件错误 (6000-6099)
  6000: '文件未找到',
  6001: '不支持的文件类型',
  6002: '文件上传失败',
  6003: '文件大小超出限制',

  // 点赞错误 (7000-7099)
  7000: '点赞操作过于频繁，请稍后重试',
  7001: '已点过赞',
  7002: '未点过赞，无法取消',

  // 系统/基础设施错误 (8000-8099)
  8000: '服务器内部错误，请稍后重试',
  8001: '数据库操作失败',
  8002: '缓存服务不可用',
  8003: '缓存操作失败',
  8004: '网络连接异常，请检查网络',

  // 外部服务错误 (8100-8199)
  8100: '外部服务调用失败',
  8101: '外部服务响应超时',
  8102: '外部服务返回错误',
  8103: '外部服务未配置',
  8104: '邮件发送失败',
  8105: '短信发送失败',

  // 平台/限流错误 (9000-9099)
  9000: '请求频率过高，请稍后重试',
  9001: 'IP 已被加入黑名单',
};

const String _defaultErrorMessage = '操作失败，请稍后重试';

/// 根据后端返回的 errorCode 获取用户可读消息。
/// 优先使用 errorCode 映射；其次使用 fallback；最后使用默认消息。
String errorMessageFromCode(int? code, {String? fallback}) {
  if (code != null && code != 0 && _errorCodeMessages.containsKey(code)) {
    return _errorCodeMessages[code]!;
  }
  if (fallback != null && fallback.trim().isNotEmpty) {
    return fallback.trim();
  }
  return _defaultErrorMessage;
}

/// 从异常对象（DioException 或其他）提取用户可读错误消息。
String extractErrorFromException(Object error) {
  if (error is DioException) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
        return '连接服务器超时，请检查网络后重试';
      case DioExceptionType.sendTimeout:
        return '图片上传超时，请换个网络或减少图片数量后重试';
      case DioExceptionType.receiveTimeout:
        return '服务器处理时间较长，请稍后重试或减少图片数量';
      case DioExceptionType.connectionError:
        return '网络连接失败，请检查网络后重试';
      case DioExceptionType.cancel:
        return '请求已取消';
      case DioExceptionType.badCertificate:
      case DioExceptionType.badResponse:
      case DioExceptionType.unknown:
        break;
    }

    final data = error.response?.data;
    if (data is Map<String, dynamic>) {
      final errorCode = data['errorCode'];
      if (errorCode is int && errorCode != 0) {
        return errorMessageFromCode(errorCode);
      }
      final detail = data['detail'] ?? data['message'];
      if (detail is String && detail.trim().isNotEmpty) {
        return detail.trim();
      }
    }

    final message = error.message?.trim();
    if (message != null && message.isNotEmpty) {
      return message;
    }
  }

  final message = error.toString().trim();
  if (message.startsWith('Exception:')) {
    return message.substring('Exception:'.length).trim();
  }
  return message.isEmpty ? _defaultErrorMessage : message;
}
