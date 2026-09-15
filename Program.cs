using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SoundEffectLibrary;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Interface;
using SoundEffectLibrary.Models;
using SoundEffectLibrary.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<SfxDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAudioFileValidator, AudioFileValidator>();
builder.Services.AddScoped<IAudioFileStorage, AudioFileStorage>();

builder.Services.AddScoped<CreateAudioAssetService>();
builder.Services.AddScoped<GetAudioFileService>();

builder.Services.Configure<AudioUploadOptions>(builder.Configuration.GetSection("AudioUpload"));
builder.Services.Configure<AudioStorageOptions>(builder.Configuration.GetSection("AudioStorage"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
