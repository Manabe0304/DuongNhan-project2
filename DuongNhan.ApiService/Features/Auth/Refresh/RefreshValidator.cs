using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using FluentValidation;

namespace DuongNhan.ApiService.Features.Auth.Refresh;

internal sealed class RefreshValidator : Validator<RefreshTokenRequest>
{
    public RefreshValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(512).WithMessage("Refresh token is invalid.");
    }
}
