using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace iM.Cloud.Application.Auth;

public sealed class RefreshTokenHandler
{
    private readonly IAuthService _authService;
    private readonly IApplicationDbContext _db;
    private readonly IUserService _userService;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenHandler(
        IAuthService authService,
        IApplicationDbContext db,
        IUserService userService,
        IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _db = db;
        _userService = userService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<LoginResponse>> HandleAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _authService.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive)
            return Result<LoginResponse>.Failure("Invalid refresh token.");

        var user = await _userService.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("Invalid refresh token.");

        stored.Revoke();

        var accessToken = _authService.GenerateAccessToken(user.Id, user.Email, user.DisplayName);
        var refreshToken = _authService.GenerateRefreshToken();
        var newHash = _authService.HashToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        _db.RefreshTokens.Add(Domain.Entities.RefreshToken.Create(user.Id, newHash, expiresAt));
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
