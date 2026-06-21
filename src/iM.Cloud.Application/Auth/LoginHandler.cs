using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;
using iM.Cloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace iM.Cloud.Application.Auth;

public sealed class LoginHandler
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IApplicationDbContext _db;
    private readonly JwtOptions _jwtOptions;

    public LoginHandler(
        IUserService userService,
        IAuthService authService,
        IApplicationDbContext db,
        IOptions<JwtOptions> jwtOptions)
    {
        _userService = userService;
        _authService = authService;
        _db = db;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("Invalid credentials.");

        var valid = await _userService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (!valid.Succeeded)
            return Result<LoginResponse>.Failure("Invalid credentials.");

        var accessToken = _authService.GenerateAccessToken(user.Id, user.Email, user.DisplayName);
        var refreshToken = _authService.GenerateRefreshToken();
        var tokenHash = _authService.HashToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, tokenHash, expiresAt));
        await _db.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            User = user
        });
    }
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "iM.Cloud";
    public string Audience { get; set; } = "iM.Cloud";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
