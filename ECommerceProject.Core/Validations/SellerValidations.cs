using FluentValidation;
using ECommerceProject.Core.DTOs;

namespace ECommerceProject.Core.Validations
{
    public class CreateSellerDtoValidator : AbstractValidator<CreateSellerDto>
    {
        public CreateSellerDtoValidator()
        {
            RuleFor(x => x.StoreName)
                .NotEmpty().WithMessage("Mağaza adı boş olamaz")
                .MaximumLength(100).WithMessage("Mağaza adı 100 karakterden uzun olamaz");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama 500 karakterden uzun olamaz");

            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("İletişim e-postası boş olamaz")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
                .MaximumLength(100).WithMessage("E-posta 100 karakterden uzun olamaz");

            RuleFor(x => x.ContactPhone)
                .MaximumLength(20).WithMessage("Telefon numarası 20 karakterden uzun olamaz");

            RuleFor(x => x.Address)
                .MaximumLength(200).WithMessage("Adres 200 karakterden uzun olamaz");
        }
    }

    public class UpdateSellerDtoValidator : AbstractValidator<UpdateSellerDto>
    {
        public UpdateSellerDtoValidator()
        {
            RuleFor(x => x.StoreName)
                .NotEmpty().WithMessage("Mağaza adı boş olamaz")
                .MaximumLength(100).WithMessage("Mağaza adı 100 karakterden uzun olamaz");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama 500 karakterden uzun olamaz");

            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("İletişim e-postası boş olamaz")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
                .MaximumLength(100).WithMessage("E-posta 100 karakterden uzun olamaz");

            RuleFor(x => x.ContactPhone)
                .MaximumLength(20).WithMessage("Telefon numarası 20 karakterden uzun olamaz");

            RuleFor(x => x.Address)
                .MaximumLength(200).WithMessage("Adres 200 karakterden uzun olamaz");
        }
    }
} 