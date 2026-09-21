using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using FluentValidation;

namespace DuongNhan.ApiService.Features.Auth.Register;

internal sealed class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(r => r.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(r => r.DisplayName)
            .MaximumLength(100)
            .When(r => !string.IsNullOrWhiteSpace(r.DisplayName));

        RuleFor(r => r.PhoneNumber)
            .MaximumLength(30)
            .When(r => !string.IsNullOrWhiteSpace(r.PhoneNumber));
    }
}