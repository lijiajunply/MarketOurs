using FluentValidation;
using MarketOurs.Data.DTOs;

namespace MarketOurs.WebAPI.Validators;

public class CommentCreateDtoValidator : AbstractValidator<CommentCreateDto>
{
    public CommentCreateDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("评论内容不能为空")
            .MaximumLength(512).WithMessage("评论内容长度不能超过512位");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空")
            .MaximumLength(64).WithMessage("用户ID长度不能超过64位");

        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("贴子ID不能为空")
            .MaximumLength(64).WithMessage("贴子ID长度不能超过64位");

        RuleFor(x => x.ParentCommentId)
            .MaximumLength(64).WithMessage("父评论ID长度不能超过64位");
    }
}

public class CommentUpdateDtoValidator : AbstractValidator<CommentUpdateDto>
{
    public CommentUpdateDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("评论内容不能为空")
            .MaximumLength(512).WithMessage("评论内容长度不能超过512位");
    }
}
