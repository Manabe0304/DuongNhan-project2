using System.Text.RegularExpressions;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using FluentValidation;

namespace DuongNhan.ApiService.Features.Auth.Register;

internal sealed partial class RegisterValidator : Validator<RegisterRequest>
{
    [GeneratedRegex(@"^[\p{L}\p{M}\p{Zs}\-'\.]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GetDisplayNamePatternRegex();

    private static readonly Regex DisplayNamePatternRegex = GetDisplayNamePatternRegex();
    private static readonly Regex DisplayNamePattern = DisplayNamePatternRegex;

    public RegisterValidator(BreachedPasswordValidator breachedValidator)
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(320).WithMessage("Email is too long.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Must(p => p.Any(char.IsUpper))
                .WithMessage("Password must contain an uppercase letter.")
            .Must(p => p.Any(char.IsLower))
                .WithMessage("Password must contain a lowercase letter.")
            .Must(p => p.Any(char.IsDigit))
                .WithMessage("Password must contain a digit.")
            .MustAsync(async (password, ct) =>
            {
                var isBreached = await breachedValidator.IsBreachedAsync(password, ct);
                return !isBreached;
            })
            .WithMessage("This password has appeared in a known data breach. Please choose a different one.");

        RuleFor(r => r.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .Must(v => v!.Trim().Length >= 2)
                .WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Display name is too long.")
            .Matches(DisplayNamePattern)
                .WithMessage("Display name may only contain letters, spaces, hyphens, apostrophes, and periods.");

        RuleFor(r => r.PhoneNumber)
            .Must(PhoneNumberValidation.IsValidE164)
                .When(r => !string.IsNullOrWhiteSpace(r.PhoneNumber))
                .WithMessage("Phone number must be in E.164 format (e.g. +14155552671).")
            .MaximumLength(16)
                .When(r => !string.IsNullOrWhiteSpace(r.PhoneNumber));
    }
}