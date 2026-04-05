using FluentValidation;
using MarketOurs.Data.DTOs;
using MarketOurs.WebAPI.Controllers;

namespace MarketOurs.WebAPI.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty().WithMessage("账号不能为空")
            .MaximumLength(128).WithMessage("账号长度不能超过128位");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6位")
            .MaximumLength(32).WithMessage("密码长度不能超过128位");
    }
}

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty().WithMessage("账号不能为空")
            .MaximumLength(128).WithMessage("账号长度不能超过128位")
            .Must(account => IsEmail(account) || IsPhone(account))
            .WithMessage("账号必须是有效的邮箱或手机号");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6位")
            .MaximumLength(32).WithMessage("密码长度不能超过128位")
            .Matches(@"[A-Z]").WithMessage("密码必须包含至少一个大写字母")
            .Matches(@"[a-z]").WithMessage("密码必须包含至少一个小写字母")
            .Matches(@"[0-9]").WithMessage("密码必须包含至少一个数字");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(128).WithMessage("用户名长度不能超过128位");
    }

    private bool IsEmail(string account)
    {
        if (string.IsNullOrEmpty(account)) return false;
        return account.Contains("@") && System.Text.RegularExpressions.Regex.IsMatch(account, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private bool IsPhone(string account)
    {
        if (string.IsNullOrEmpty(account)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(account, @"^1[3-9]\d{9}$");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("验证码不能为空");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .MinimumLength(6).WithMessage("新密码长度不能少于6位")
            .MaximumLength(32).WithMessage("新密码长度不能超过128位")
            .Matches(@"[A-Z]").WithMessage("新密码必须包含至少一个大写字母")
            .Matches(@"[a-z]").WithMessage("新密码必须包含至少一个小写字母")
            .Matches(@"[0-9]").WithMessage("新密码必须包含至少一个数字");
    }
}
