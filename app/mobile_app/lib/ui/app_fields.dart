import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

class AppTextField extends StatelessWidget {
  const AppTextField({
    super.key,
    this.controller,
    this.placeholder,
    this.keyboardType,
    this.textInputAction,
    this.onFieldSubmitted,
    this.maxLines = 1,
    this.validator,
    this.prefix,
    this.suffix,
    this.obscureText = false,
  });

  final TextEditingController? controller;
  final String? placeholder;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;
  final int maxLines;
  final FormFieldValidator<String>? validator;
  final Widget? prefix;
  final Widget? suffix;
  final bool obscureText;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        hintText: placeholder,
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
        prefixIcon: prefix,
        suffixIcon: suffix,
      ),
      keyboardType: keyboardType,
      textInputAction: textInputAction,
      maxLines: maxLines,
      obscureText: obscureText,
      onFieldSubmitted: onFieldSubmitted,
      validator: validator,
    );
  }
}

class AppPasswordField extends StatefulWidget {
  const AppPasswordField({
    super.key,
    this.controller,
    this.placeholder,
    this.validator,
    this.textInputAction,
    this.onFieldSubmitted,
  });

  final TextEditingController? controller;
  final String? placeholder;
  final FormFieldValidator<String>? validator;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;

  @override
  State<AppPasswordField> createState() => _AppPasswordFieldState();
}

class _AppPasswordFieldState extends State<AppPasswordField> {
  bool _obscureText = true;

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      controller: widget.controller,
      placeholder: widget.placeholder,
      validator: widget.validator,
      textInputAction: widget.textInputAction,
      onFieldSubmitted: widget.onFieldSubmitted,
      obscureText: _obscureText,
      suffix: CupertinoButton(
        padding: EdgeInsets.zero,
        minimumSize: Size.zero,
        onPressed: () => setState(() => _obscureText = !_obscureText),
        child: Icon(
          _obscureText ? CupertinoIcons.eye : CupertinoIcons.eye_slash,
          size: 18,
        ),
      ),
    );
  }
}
