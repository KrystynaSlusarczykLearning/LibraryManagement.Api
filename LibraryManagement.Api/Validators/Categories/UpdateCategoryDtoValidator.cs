using FluentValidation;
using LibraryManagement.Api.DTOs.Categories;

namespace LibraryManagement.Api.Validators.Categories;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}
