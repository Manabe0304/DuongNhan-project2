using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using FluentValidation;

namespace DuongNhan.ApiService.Features.Auth.Login;

internal sealed class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(320).WithMessage("Email is too long.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}
