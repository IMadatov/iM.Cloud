using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Models;

namespace iM.Cloud.Application.Common.Interfaces;

public interface IAuthService
{
    string GenerateAccessToken(Guid userId, string email, string displayName);
    string GenerateRefreshToken();
    string HashToken(string token);
}
