using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AspNetCore3ODataPermissionsSample.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ODataAuthorization;
using ODataAuthorizationSample.Data;

namespace AspNetCore3ODataPermissionsSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<AppDbContext>(opt => opt
                    .UseLazyLoadingProxies()
                    .UseInMemoryDatabase("ODataCustomAuthSample"));

            services.AddCors(options =>
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
            services.AddAuthentication("AuthScheme")
                .AddScheme<CustomAuthenticationOptions, CustomAuthenticationHandler>("AuthScheme", options => { });

            services.AddAuthorization(o => o
                .AddODataAuthorizationPolicy());

            services
                .AddControllers()
                // Enable OData Functionality
                .AddOData((opt) =>
                {
                    opt
                        .AddRouteComponents("odata", AppModel.GetEdmModel())
                        .EnableQueryFeatures().Select().Expand().OrderBy().Filter().Count();
                });


            //services.AddAuthorization();

            services.AddRouting();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAll");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireODataAuthorization();
            });

            DatabaseUtils.CreateDatabaseAndSampleData(app.ApplicationServices);
        }
    }

    // our customer authentication handler
    internal class CustomAuthenticationHandler : AuthenticationHandler<CustomAuthenticationOptions>
    {
        public CustomAuthenticationHandler(IOptionsMonitor<CustomAuthenticationOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new System.Security.Principal.GenericIdentity("Me");
            // in this dummy authentication scheme, we assume that the permissions granted
            // to the user are stored as a comma-separate list in a header called Permissions
            var scopeValues = Request.Headers["Permissions"];
            if (scopeValues.Count != 0)
            {
                var scopes = scopeValues.ToArray()[0].Split(",").Select(s => s.Trim());
                var claims = scopes.Select(scope => new Claim("Scope", scope));
                identity.AddClaims(claims);
            }

            var principal = new GenericPrincipal(identity, Array.Empty<string>());
            // we use the same auhentication scheme as the one specified in the OData model permissions
            var ticket = new AuthenticationTicket(principal, "AuthScheme");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    internal class CustomAuthenticationOptions : AuthenticationSchemeOptions
    {
    }
}
