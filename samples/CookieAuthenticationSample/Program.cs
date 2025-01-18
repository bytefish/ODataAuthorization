using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ODataAuthorization;
using ODataAuthorizationDemo.Models;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("ODataAuthDemo"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Add Cookie Authentication:
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie((options) =>
    {
        options.AccessDeniedPath = string.Empty;

        options.Events.OnRedirectToAccessDenied = (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToLogin = (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddODataAuthorizationPolicy();
});

builder.Services
    .AddControllers()
    // Add OData Routes:
    .AddOData((opt) => opt
        .AddRouteComponents("odata", AppEdmModel.GetModel())
        .EnableQueryFeatures());

var app = builder.Build();

app.UseCors("AllowAll");

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app
    .MapControllers()
    .RequireODataAuthorization();

app.Run();