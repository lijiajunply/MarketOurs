import 'package:flutter/cupertino.dart';

import 'app_theme.dart';

class AppTextField extends StatefulWidget {
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
  State<AppTextField> createState() => _AppTextFieldState();
}

class _AppTextFieldState extends State<AppTextField> {
  late final TextEditingController _controller;
  late final bool _ownsController;

  @override
  void initState() {
    super.initState();
    _ownsController = widget.controller == null;
    _controller = widget.controller ?? TextEditingController();
  }

  @override
  void dispose() {
    if (_ownsController) {
      _controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FormField<String>(
      initialValue: _controller.text,
      validator: widget.validator,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      builder: (field) {
        final borderColor = field.hasError
            ? AppColors.destructive
            : AppColors.input.withValues(alpha: 0.55);

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              decoration: BoxDecoration(
                color: CupertinoDynamicColor.resolve(AppColors.secondary, context)
                    .withValues(alpha: 0.85),
                borderRadius: BorderRadius.circular(AppRadii.lg),
                border: Border.all(
                  color: CupertinoDynamicColor.resolve(borderColor, context),
                ),
              ),
              child: CupertinoTextField(
                controller: _controller,
                placeholder: widget.placeholder,
                keyboardType: widget.keyboardType,
                textInputAction: widget.textInputAction,
                maxLines: widget.maxLines,
                obscureText: widget.obscureText,
                onChanged: field.didChange,
                onSubmitted: widget.onFieldSubmitted,
                prefix: widget.prefix == null
                    ? null
                    : Padding(
                        padding: const EdgeInsets.only(left: 14),
                        child: widget.prefix!,
                      ),
                suffix: widget.suffix == null
                    ? null
                    : Padding(
                        padding: const EdgeInsets.only(right: 14),
                        child: widget.suffix!,
                      ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 16,
                ),
                decoration: null,
                style: AppTextStyles.body(context),
                placeholderStyle: TextStyle(
                  fontSize: 16,
                  color: CupertinoDynamicColor.resolve(
                    AppColors.mutedForeground,
                    context,
                  ),
                ),
              ),
            ),
            if (field.hasError) ...[
              const SizedBox(height: 6),
              Padding(
                padding: const EdgeInsets.only(left: 4),
                child: Text(
                  field.errorText ?? '',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: CupertinoDynamicColor.resolve(
                      AppColors.destructive,
                      context,
                    ),
                  ),
                ),
              ),
            ],
          ],
        );
      },
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
          color: AppColors.mutedForeground,
        ),
      ),
    );
  }
}
