import 'package:flutter/cupertino.dart';

class AuthLoadingScreen extends StatelessWidget {
  const AuthLoadingScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const CupertinoPageScaffold(
      child: Center(child: CupertinoActivityIndicator(radius: 14)),
    );
  }
}
