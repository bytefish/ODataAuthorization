using System.Linq;
using System.Threading.Tasks;
using ODataAuthorization.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ODataAuthorizationDemo.Models;
using ODataAuthorization;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Http;

namespace ODataAuthorizationDemo
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
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("ODataAuthDemo"));

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

            services
                .AddControllers()
                .AddOData((opt) => opt
                    .AddRouteComponents("odata", AppEdmModel.GetModel())
                    .EnableQueryFeatures())
                .AddODataAuthorization((options) =>
                {
                    options.ScopesFinder = context =>
                    {
                        var scopes = context.User.FindAll("Scope").Select(claim => claim.Value);

                        return Task.FromResult(scopes);
                    };

                    options
                        .ConfigureAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        // Configure the Authentication, so it returns a 401 for unauthorized access:
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
            });
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
            app.UseODataAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
