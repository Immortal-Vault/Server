﻿using ImmortalVault_Server.Models;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace ImmortalVault_Server.Services.Auth;

public interface IMfaService
{
    Task<bool> UseUserMfa(User user, string stringMfa);
    string GenerateMfaRecoveryCode(Random random);
    string? SetupMfa(User user);
    Task<string[]?> EnableMfa(User user, string totpCode);
    Task<bool> DisableMfa(User user, string password, string totpCode);
}

public class MfaService : IMfaService
{
    private static readonly Dictionary<int, string> _mfaRequests = new();
    private readonly ApplicationDbContext _dbContext;

    public MfaService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UseUserMfa(User user, string stringMfa)
    {
        if (!user.MfaEnabled || user.Mfa is null)
        {
            throw new OperationCanceledException("Mfa disabled");
        }

        if (user.MfaRecoveryCodes is not { } codes || !codes.Contains(stringMfa))
        {
            return await ValidateMfa(user.Mfa, stringMfa);
        }

        var newCodes = codes.Where(c => c != stringMfa).ToArray();
        await this._dbContext.Users.Where(a => a.Id == user.Id).ExecuteUpdateAsync(
            a => a.SetProperty(p => p.MfaRecoveryCodes, newCodes.ToList()));

        return true;
    }

    public string GenerateMfaRecoveryCode(Random random)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(6).ToArray());
    }

    public string? SetupMfa(User user)
    {
        if (user.Mfa is not null) return null;
        var mfa = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(11));
        if (_mfaRequests.TryGetValue(user.Id, out var value))
        {
            return value;
        }

        _mfaRequests.Add(user.Id, mfa);
        return mfa;
    }


    public async Task<string[]?> EnableMfa(User user, string totpCode)
    {
        if (!_mfaRequests.TryGetValue(user.Id, out var mfa))
            return null;
        var totp = new Totp(Base32Encoding.ToBytes(mfa));
        if (!totp.VerifyTotp(totpCode, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
            return null;
        _mfaRequests.Remove(user.Id);
        var random = new Random();
        var codes = Enumerable.Range(0, 8).Select(_ => this.GenerateMfaRecoveryCode(random))
            .ToArray();

        await this._dbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(
            (u) =>
                u.SetProperty(p => p.Mfa, mfa).SetProperty(p => p.MfaRecoveryCodes, codes.ToList()));

        return codes;
    }

    public async Task<bool> DisableMfa(User user, string password, string totpCode)
    {
        if (user.Mfa is null) return true;
        if (!Argon2.Verify(user.Password, password)) return false;
        if (!await ValidateMfa(user.Mfa, totpCode)) return false;

        await this._dbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u
            .SetProperty(p => p.Mfa, (string?)null).SetProperty(p => p.MfaRecoveryCodes, (List<string>?)null));

        return true;
    }

    private static Task<bool> ValidateMfa(string mfa, string stringMfa)
    {
        return Task.FromResult(new Totp(Base32Encoding.ToBytes(mfa)).VerifyTotp(stringMfa, out _,
            VerificationWindow.RfcSpecifiedNetworkDelay));
    }
}