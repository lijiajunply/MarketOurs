import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../models/auth.dart';
import '../models/auth_session.dart';
import '../models/user.dart';

abstract class AuthStorage {
  Future<AuthSession?> readSession();

  Future<String?> readAccessToken();

  Future<void> saveTokens(TokenDto token);

  Future<void> saveUser(UserDto user);

  Future<void> clear();
}

class SecureAuthStorage implements AuthStorage {
  SecureAuthStorage({FlutterSecureStorage? secureStorage})
    : _secureStorage = secureStorage ?? const FlutterSecureStorage();

  static const _accessTokenKey = 'auth.access_token';
  static const _refreshTokenKey = 'auth.refresh_token';
  static const _userKey = 'auth.user';

  final FlutterSecureStorage _secureStorage;

  @override
  Future<AuthSession?> readSession() async {
    final prefs = await SharedPreferences.getInstance();
    final accessToken = await _readValue(
      secureKey: _accessTokenKey,
      prefs: prefs,
    );
    final refreshToken = await _readValue(
      secureKey: _refreshTokenKey,
      prefs: prefs,
    );
    final userJson = prefs.getString(_userKey);

    if ((accessToken?.isEmpty ?? true) &&
        (refreshToken?.isEmpty ?? true) &&
        (userJson == null || userJson.isEmpty)) {
      return null;
    }

    UserDto? user;
    if (userJson != null && userJson.isNotEmpty) {
      try {
        user = UserDto.fromJson(jsonDecode(userJson) as Map<String, dynamic>);
      } catch (_) {
        final mutablePrefs = await SharedPreferences.getInstance();
        await mutablePrefs.remove(_userKey);
      }
    }

    return AuthSession(
      accessToken: accessToken,
      refreshToken: refreshToken,
      user: user,
    );
  }

  @override
  Future<String?> readAccessToken() async {
    final prefs = await SharedPreferences.getInstance();
    return _readValue(secureKey: _accessTokenKey, prefs: prefs);
  }

  @override
  Future<void> saveTokens(TokenDto token) async {
    final prefs = await SharedPreferences.getInstance();
    final writes = <Future<void>>[];

    if (token.accessToken != null) {
      writes.add(
        _writeValue(
          secureKey: _accessTokenKey,
          value: token.accessToken,
          prefs: prefs,
        ),
      );
    }

    if (token.refreshToken != null) {
      writes.add(
        _writeValue(
          secureKey: _refreshTokenKey,
          value: token.refreshToken,
          prefs: prefs,
        ),
      );
    }

    await Future.wait(writes);
  }

  @override
  Future<void> saveUser(UserDto user) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_userKey, jsonEncode(user.toJson()));
  }

  @override
  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await Future.wait([
      _deleteValue(secureKey: _accessTokenKey, prefs: prefs),
      _deleteValue(secureKey: _refreshTokenKey, prefs: prefs),
      prefs.remove(_userKey),
    ]);
  }

  Future<String?> _readValue({
    required String secureKey,
    required SharedPreferences prefs,
  }) async {
    try {
      return await _secureStorage.read(key: secureKey);
    } on PlatformException catch (error) {
      _logFallback('read', secureKey, error);
      return prefs.getString(secureKey);
    }
  }

  Future<void> _writeValue({
    required String secureKey,
    required String? value,
    required SharedPreferences prefs,
  }) async {
    if (value == null) {
      return;
    }

    try {
      await _secureStorage.write(key: secureKey, value: value);
      await prefs.remove(secureKey);
    } on PlatformException catch (error) {
      _logFallback('write', secureKey, error);
      await prefs.setString(secureKey, value);
    }
  }

  Future<void> _deleteValue({
    required String secureKey,
    required SharedPreferences prefs,
  }) async {
    try {
      await _secureStorage.delete(key: secureKey);
    } on PlatformException catch (error) {
      _logFallback('delete', secureKey, error);
    } finally {
      await prefs.remove(secureKey);
    }
  }

  void _logFallback(String action, String key, PlatformException error) {
    debugPrint(
      'Secure storage $action failed for "$key", falling back to SharedPreferences: '
      '${error.code} ${error.message}',
    );
  }
}
