import 'package:flutter/cupertino.dart';

import '../../ui/app_fields.dart';

class PasswordFormField extends StatefulWidget {
  const PasswordFormField({
    super.key,
    this.controller,
    this.placeholder,
    this.validator,
    this.onFieldSubmitted,
    this.textInputAction,
  });

  final TextEditingController? controller;
  final String? placeholder;
  final FormFieldValidator<String>? validator;
  final ValueChanged<String>? onFieldSubmitted;
  final TextInputAction? textInputAction;

  @override
  State<PasswordFormField> createState() => _PasswordFormFieldState();
}

class _PasswordFormFieldState extends State<PasswordFormField> {
  bool _obscureText = true;

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      controller: widget.controller,
      placeholder: widget.placeholder,
      suffix: CupertinoButton(
        padding: EdgeInsets.zero,
        minimumSize: Size.zero,
          onPressed: () => setState(() => _obscureText = !_obscureText),
          child: Icon(
            _obscureText ? CupertinoIcons.eye : CupertinoIcons.eye_slash,
            size: 18,
          ),
        ),
      validator: widget.validator,
      onFieldSubmitted: widget.onFieldSubmitted,
      textInputAction: widget.textInputAction,
      obscureText: _obscureText,
    );
  }
}
