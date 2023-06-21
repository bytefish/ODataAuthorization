// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using JwtAuthenticationSample.Models;
using JwtAuthenticationSample.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthenticationSample.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private AppDbContext _dbContext;
    private IJwtTokenService _jwtTokenService;

    public AuthController(AppDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(d => d.Email == request.Email);

        if (user == null)
        {
            return BadRequest("Bad credentials");
        }

        // Add Requested Scopes for easier tests:
        if(request.RequestedScopes != null)
        {
            user.Scopes = request.RequestedScopes;
        }
        
        var isPasswordValid = user.Password == request.Password;

        if (!isPasswordValid)
        {
            return BadRequest("Bad credentials");
        }

        var accessToken = _jwtTokenService.CreateToken(user);
        
        return Ok(new LoginResponseDto
        {
            Username = user.Username,
            Email = user.Email,
            Token = accessToken
        });
    }
    
    
    public class LoginRequestDto
    {
        [EmailAddress, Required]
        public string Email { get; set; } = null!;

        [MinLength(6), Required] 
        public string Password { get; set; } = null!;

        public string RequestedScopes { get; set; }
    }
    
    public class LoginResponseDto
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public string Token { get; set; }
    }
}