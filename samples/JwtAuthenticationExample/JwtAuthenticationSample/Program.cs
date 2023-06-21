// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Text;
using JwtAuthenticationSample.Models;
using JwtAuthenticationSample.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ODataAuthorization;
using ODataAuthorization.Extensions;

var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy  =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("JwtAuthentication"));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddControllers()
    .AddOData(options => 
        options.EnableQueryFeatures(null).AddRouteComponents("odata", AppEdmModel.GetModel()))
    .AddODataAuthorization(options =>
    {
        options.ScopesFinder = context =>
        {
            var scopes = context.User.FindAll("Scope")
                .Select(claim => claim.Value);
            foreach (var s in scopes.ToList ())
            {
                Console.WriteLine(s);
            }
            return Task.FromResult(scopes);
        };
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {

        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "apiWithAuthBackend",
            ValidAudience = "apiWithAuthBackend",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])
            ),
        };
        
        // options.Events.OnAuthenticationFailed = (context) =>
        // {
        //     context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //     return Task.CompletedTask;
        // };
        // options.Events.OnForbidden = context =>
        // {
        //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //     return Task.CompletedTask;
        // };
        
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.IScopesEvaluator
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseODataRouteDebug();
}

app.UseCors(MyAllowSpecificOrigins);

// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
    AppDbContextHelper.SeedDb(db);
}

app.Run();