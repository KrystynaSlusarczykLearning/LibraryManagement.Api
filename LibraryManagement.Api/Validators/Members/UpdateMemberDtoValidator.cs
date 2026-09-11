using FluentValidation;
using LibraryManagement.Api.DTOs.Members;

namespace LibraryManagement.Api.Validators.Members;

public class UpdateMemberDtoValidator : AbstractValidator<UpdateMemberDto>
{
    public UpdateMemberDtoValidator()
    {
        RuleFor(dto => dto.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(dto => dto.LastName)
            .NotEmpty()
            .MaximumLength(50);
    }
}
