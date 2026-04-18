import 'dart:convert';

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
    final accessToken = await _secureStorage.read(key: _accessTokenKey);
    final refreshToken = await _secureStorage.read(key: _refreshTokenKey);
    final prefs = await SharedPreferences.getInstance();
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
  Future<String?> readAccessToken() {
    return _secureStorage.read(key: _accessTokenKey);
  }

  @override
  Future<void> saveTokens(TokenDto token) async {
    final writes = <Future<void>>[];

    if (token.accessToken != null) {
      writes.add(
        _secureStorage.write(key: _accessTokenKey, value: token.accessToken),
      );
    }

    if (token.refreshToken != null) {
      writes.add(
        _secureStorage.write(key: _refreshTokenKey, value: token.refreshToken),
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
      _secureStorage.delete(key: _accessTokenKey),
      _secureStorage.delete(key: _refreshTokenKey),
      prefs.remove(_userKey),
    ]);
  }
}
