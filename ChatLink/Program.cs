using System.Text.Json.Serialization;
using ChatLink.Controllers;
using ChatLink.MiddleWare;
using ChatLink.Models;
using ChatLink.Models.Auth;
using ChatLink.Models.Models;
using ChatLink.Services;
using ChatLink.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ChatLink;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure request size limit
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024; // 5 GB
        });

        // Add services to the container.
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024; // 5 GB
        });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost",
                builder =>
                {
                    builder.WithOrigins("http://localhost", "http://10.0.2.2", "https://yourdomain.com", "null")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.AddAuthorization();
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
        });

        builder.Services.AddSignalR();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(x =>
        {
            var setup = new OpenApiSecurityScheme()
            {
                Scheme = "bearer",
                Description = "Insert your JWT token",
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            x.AddSecurityDefinition(setup.Reference.Id, setup);
            x.AddSecurityRequirement(new OpenApiSecurityRequirement { { setup, Array.Empty<string>() } });
        });

        builder.Services.AddIdentity<User, IdentityRole>(x =>
        {
            x.SignIn.RequireConfirmedAccount = false;
            x.SignIn.RequireConfirmedEmail = false;
            x.SignIn.RequireConfirmedPhoneNumber = false;
            x.Password.RequireNonAlphanumeric = false;
            x.Password.RequireUppercase = false;
            x.Password.RequireLowercase = false;
            x.Password.RequireDigit = false;
            x.Password.RequiredLength = 3;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var connectionDbConfig = builder.Configuration.GetConnectionString("DefaultConnection");

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionDbConfig));
        }
        else
        {
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionDbConfig).EnableSensitiveDataLogging());
        }

        var authConfig = builder.Configuration.GetSection("Auth");
        var authOptions = authConfig.Get<AuthOptions>();
        builder.Services.Configure<AuthOptions>(authConfig);

        var key = authOptions?.GetSymmetricSecurityKey();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/api/signalr")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };

            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authOptions?.Issuer,

                ValidateAudience = true,
                ValidAudience = authOptions?.Audience,

                ValidateLifetime = true,

                IssuerSigningKey = key,
                TokenDecryptionKey = key,
                ValidateIssuerSigningKey = true
            };
        });

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IChatService, ChatService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseDeveloperExceptionPage();
        }

        if (!app.Environment.IsDevelopment())
        {
            //app.UseHttpsRedirection();
        }

        app.UseRouting();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Apply the CORS policy globally before other middleware
        app.UseCors("AllowLocalhost");

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            //app.UseMiddleware<LogHeadersMiddleware>();
        }

        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                context.Response.StatusCode = 204;
                return;
            }

            await next.Invoke();
        });



        app.MapHub<ChatHub>("/api/signalr", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
        });
        app.MapControllers();

        app.Run();
    }
}
