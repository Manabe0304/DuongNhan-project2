using System.Security.Cryptography;
using DuongNhan.ApiService.Models;
using Microsoft.AspNetCore.Identity;

namespace DuongNhan.ApiService.Services;

/// <summary>
/// Performs a throwaway password verification so that a login attempt for an
/// unknown account costs roughly the same CPU time as a real one. Without this,
/// response timing leaks whether an email address is registered
/// (MITRE ATT&CK T1589.002 - Gather Victim Identity Information).
/// </summary>
internal sealed class DummyPasswordVerifier
{
    private readonly IPasswordHasher<User> _hasher;
    private readonly User _probeUser;
    private readonly string _probeHash;

    public DummyPasswordVerifier(IPasswordHasher<User> hasher)
    {
        _hasher = hasher;
        _probeUser = new User { Email = "probe@invalid.local", PasswordHash = string.Empty };
        _probeHash = hasher.HashPassword(_probeUser, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    }

    public void Verify(string password)
        => _hasher.VerifyHashedPassword(_probeUser, _probeHash, password);
}
