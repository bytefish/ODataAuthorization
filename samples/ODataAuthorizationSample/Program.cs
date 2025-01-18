using AspNetCore3ODataPermissionsSample;
using AspNetCore3ODataPermissionsSample.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using ODataAuthorization;
using ODataAuthorizationSample.Auth;

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


// OData authorization depends on the AspNetCore authentication and authorization services
// we need to specify at least one authentication scheme and handler. Here we opt for a simple custom handler defined
// later in this file, for demonstration purposes. Could also use cookie-based or JWT authentication
builder.Services.AddAuthentication("AuthScheme")
    .AddScheme<CustomAuthenticationOptions, CustomAuthenticationHandler>("AuthScheme", options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddODataAuthorizationPolicy();
});

builder.Services
    .AddControllers()
    // Add OData Routes:
    .AddOData((opt) => opt
        .AddRouteComponents("odata", AppModel.GetEdmModel())
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