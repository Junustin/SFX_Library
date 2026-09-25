using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SoundEffectLibrary.Api.Api.Services;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("https://localhost:7113")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        ProblemDetailsFactory problemFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        string detail = "Too many request.";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            context.HttpContext.Response.Headers.RetryAfter = $"{seconds}";

            detail = $"Too many request. Try again after {seconds} seconds.";
        }

        ProblemDetails problemDetails = problemFactory.CreateProblemDetails(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    detail
                );

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    };
 
    options.AddFixedWindowLimiter(policyName: "fixed", cfg =>
    {
        cfg.PermitLimit = 5;
        cfg.Window = TimeSpan.FromSeconds(10);
    });

    options.AddPolicy("per-ip", httpContext =>
    {
        string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!string.Equals(ipAddress, "unknown"))
        {
            return RateLimitPartition.GetTokenBucketLimiter(
                ipAddress,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 2,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10)
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter("unknown",
            _ => new FixedWindowRateLimiterOptions
        {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(30),
        });
    });
});

builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = 
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"];
        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = symmetricKey,
            ValidateLifetime = true,
        };
    });

builder.Services.AddOpenApi();

builder.Services.AddDbContext<SfxDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAudioFileValidator, AudioFileValidator>();
builder.Services.AddScoped<IAudioFileStorage, AudioFileStorage>();

builder.Services.AddScoped<CreateAudioAssetService>();
builder.Services.AddScoped<DeleteAudioAssetService>();
builder.Services.AddScoped<GetAudioFileService>();

builder.Services.Configure<AudioUploadOptions>(builder.Configuration.GetSection("AudioUpload"));
builder.Services.Configure<AudioStorageOptions>(builder.Configuration.GetSection("AudioStorage"));

var app = builder.Build();
app.UseCors("Frontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();