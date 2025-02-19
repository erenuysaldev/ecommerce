using FluentValidation;
using ECommerceProject.Core.DTOs;

namespace ECommerceProject.Core.Validations
{
    public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kategori adı boş olamaz")
                .Length(2, 50).WithMessage("Kategori adı 2-50 karakter arasında olmalıdır");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Kategori açıklaması en fazla 200 karakter olabilir");
        }
    }

    public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
    {
        public UpdateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kategori adı boş olamaz")
                .Length(2, 50).WithMessage("Kategori adı 2-50 karakter arasında olmalıdır");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Kategori açıklaması en fazla 200 karakter olabilir");
        }
    }
} 