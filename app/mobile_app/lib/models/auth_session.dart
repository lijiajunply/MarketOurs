import 'user.dart';

class AuthSession {
  const AuthSession({this.accessToken, this.refreshToken, this.user});

  final String? accessToken;
  final String? refreshToken;
  final UserDto? user;

  bool get hasToken => (accessToken?.isNotEmpty ?? false);

  AuthSession copyWith({
    String? accessToken,
    String? refreshToken,
    UserDto? user,
    bool clearUser = false,
  }) {
    return AuthSession(
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      user: clearUser ? null : (user ?? this.user),
    );
  }
}
