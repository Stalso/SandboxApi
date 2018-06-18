using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SandboxApi.Data;
using SandboxApi.Filters;
using SandboxApi.Models;
using SandboxApi.Models.Settings;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SandboxApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env,IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JwtOptions>(Configuration.GetSection("JWT"));
            services.AddCustomMVC(Configuration)
                .AddCustomDbContext(Configuration)
                .AddCustomIdentity(Configuration)
                .AddSwagger();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseAuthentication();

            app.UseCors("CorsPolicy");

            app.UseMvcWithDefaultRoute();

            app.UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SandboxApi.API V1");
                    c.RoutePrefix = String.Empty;
                });

            app.UseWelcomePage();
        }
    }

    public static class CustomExtensionMethods
    {
        public static IServiceCollection AddCustomIdentity(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register the Identity services.
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;

                // Password settings (only for rapid development)
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });


            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SigningKey"]));
            // Register the OpenIddict services.
            services.AddOpenIddict<Guid>(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                options.AddMvcBinders();

                // Enable the token endpoint.
                options.EnableTokenEndpoint("/connect/token");

                // Enable the password flow.
                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                // During development, you can disable the HTTPS requirement.
                options.DisableHttpsRequirement();

                // Note: to use JWT access tokens instead of the default
                // encrypted format, the following lines are required:
                //
                options.UseJsonWebTokens();
                if (!Double.TryParse(configuration["JWT:AccessTokenLifetime"], out var accessTokenLifetime))
                {
                    accessTokenLifetime = 3600;
                }
                options.SetAccessTokenLifetime(TimeSpan.FromSeconds(accessTokenLifetime));
                options.AddSigningKey(securityKey);
                // options.AddEphemeralSigningKey();
            });


            // If you prefer using JWT, don't forget to disable the automatic
            // JWT -> WS-Federation claims mapping used by the JWT middleware:
            //
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["JWT:Authority"];
                    options.Audience = configuration["JWT:Audience"];
                    options.RequireHttpsMetadata = false;
                 

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = OpenIdConnectConstants.Claims.Subject,
                        RoleClaimType = OpenIdConnectConstants.Claims.Role,
                        IssuerSigningKey = securityKey,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(0)
                    };
                });

            return services;
        }

        public static IServiceCollection AddCustomDbContext(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict<Guid>();
            });

            return services;
        }

        public static IServiceCollection AddCustomMVC(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "SandBox HTTP API",
                    Version = "v1",
                    Description = "Sandbox api for AppMyCv services",
                });

                options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }}
                });

                options.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                options.OperationFilter<FormDataOperationFilter>();
                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            return services;
        }
    }
}