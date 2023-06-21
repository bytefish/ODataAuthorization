// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace JwtAuthenticationSample.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }
    
    public string Scopes { get; set; }
}