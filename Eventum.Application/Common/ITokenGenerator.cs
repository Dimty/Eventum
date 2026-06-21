using Eventum.Domain.Models;

namespace Eventum.Application.Common;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}