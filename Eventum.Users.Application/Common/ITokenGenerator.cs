using Eventum.Users.Domain;

namespace Eventum.Users.Application.Common;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
