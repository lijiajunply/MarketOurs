using FluentValidation;
using MarketOurs.Data.DTOs;

namespace MarketOurs.WebAPI.Validators;

public class PostCreateDtoValidator : AbstractValidator<PostCreateDto>
{
    public PostCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .MaximumLength(128).WithMessage("标题长度不能超过128位");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("内容不能为空")
            .MaximumLength(1024).WithMessage("内容长度不能超过1024位");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空")
            .MaximumLength(64).WithMessage("用户ID长度不能超过64位");
    }
}

public class PostUpdateDtoValidator : AbstractValidator<PostUpdateDto>
{
    public PostUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .MaximumLength(128).WithMessage("标题长度不能超过128位");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("内容不能为空")
            .MaximumLength(1024).WithMessage("内容长度不能超过1024位");
    }
}
