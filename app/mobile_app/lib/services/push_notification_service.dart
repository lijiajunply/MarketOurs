// This is a placeholder as adding real Firebase requires Google Services files.
// However, this structure shows how to integrate the push service.

import 'package:flutter_local_notifications/flutter_local_notifications.dart';
// import 'package:firebase_messaging/firebase_messaging.dart';
import 'api_service.dart';

class PushNotificationService {
  final ApiService _apiService;
  final FlutterLocalNotificationsPlugin _localNotifications = FlutterLocalNotificationsPlugin();

  PushNotificationService(this._apiService);

  Future<void> initialize() async {
    // 1. Initialize Local Notifications
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings();
    const initSettings = InitializationSettings(android: androidSettings, iOS: iosSettings);
    
    await _localNotifications.initialize(
      initSettings,
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
      final response = await _apiService.post('/User/push-token', data: '"$token"'); // Send as raw string JSON
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }

  void showLocalNotification(dynamic message) {
    // Extract info from FCM message
    const androidDetails = AndroidNotificationDetails(
      'marketours_channel',
      'MarketOurs Notifications',
      importance: Importance.max,
      priority: Priority.high,
    );
    const iosDetails = DarwinNotificationDetails();
    const details = NotificationDetails(android: androidDetails, iOS: iosDetails);

    _localNotifications.show(
      0,
      message.notification?.title ?? 'New Notification',
      message.notification?.body ?? '',
      details,
    );
  }
}
