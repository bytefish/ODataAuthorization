// Licensed under the MIT License.  See License.txt in the project root for license information.

using JwtAuthenticationSample.Models;

namespace JwtAuthenticationSample.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}