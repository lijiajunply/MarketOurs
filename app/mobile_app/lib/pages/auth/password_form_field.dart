import 'package:flutter/material.dart';

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
    return TextFormField(
      controller: widget.controller,
      decoration: InputDecoration(
        hintText: widget.placeholder,
        filled: true,
        fillColor: const Color(0xFFF2F2F7),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: Color(0xFF007AFF)),
        ),
        suffixIcon: IconButton(
          onPressed: () => setState(() => _obscureText = !_obscureText),
          icon: Icon(_obscureText ? Icons.visibility : Icons.visibility_off),
          tooltip: _obscureText ? '查看密码' : '隐藏密码',
        ),
      ),
      validator: widget.validator,
      onFieldSubmitted: widget.onFieldSubmitted,
      textInputAction: widget.textInputAction,
      obscureText: _obscureText,
    );
  }
}
