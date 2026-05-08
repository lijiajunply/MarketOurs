// This is a placeholder as adding real Firebase requires Google Services files.
// However, this structure shows how to integrate the push service.

import 'package:flutter_local_notifications/flutter_local_notifications.dart';
// import 'package:firebase_messaging/firebase_messaging.dart';
import '../models/api_response.dart';
import 'api_service.dart';

class PushNotificationService {
  final _api = ApiService().dio;
  final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();

  PushNotificationService();

  Future<void> initialize() async {
    // 1. Initialize Local Notifications
    const androidSettings = AndroidInitializationSettings(
      '@mipmap/ic_launcher',
    );
    const iosSettings = DarwinInitializationSettings();
    const initSettings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );

    await _localNotifications.initialize(
      settings: initSettings,
      onDidReceiveNotificationResponse: (details) {
        // Handle notification click
      },
    );

    // 2. Request Permissions (FCM)
    // FirebaseMessaging messaging = FirebaseMessaging.instance;
    // NotificationSettings settings = await messaging.requestPermission();

    // 3. Get FCM Token and Register with Backend
    // String? token = await messaging.getToken();
    // if (token != null) {
    //   await registerToken(token);
    // }

    // 4. Handle Foreground Messages
    // FirebaseMessaging.onMessage.listen((RemoteMessage message) {
    //   showLocalNotification(message);
    // });
  }

  Future<bool> registerToken(String token) async {
    try {
      final response = await _api.post('/User/push-token', data: token);
      final apiRes = ApiResponse<Object?>.fromJson(
        response.data as Map<String, dynamic>,
        (json) => json,
      );
      return apiRes.code == 200 &&
          (apiRes.errorCode == null || apiRes.errorCode == 0);
    } catch (e) {
      return false;
    }
  }

  void showLocalNotification(dynamic message) {
    // Extract info from FCM message
    const androidDetails = AndroidNotificationDetails(
      'marketours_channel',
      '光汇 通知',
      importance: Importance.max,
      priority: Priority.high,
    );
    const iosDetails = DarwinNotificationDetails();
    const details = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );

    _localNotifications.show(
      id: 0,
      title: message.notification?.title ?? 'New Notification',
      body: message.notification?.body ?? '',
      notificationDetails: details,
    );
  }
}
