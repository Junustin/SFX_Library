using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Models;
using SoundEffectLibrary.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<SfxDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapPost("/api/sfx", async (
    CreateAudioAssetRequest request, 
    SfxDbContext dbContext) =>
{
    var audioAsset = new AudioAsset
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        CategoryId = request.CategoryId
    };

    dbContext.AudioAssets.Add(audioAsset);
    await dbContext.SaveChangesAsync();

    return Results.Ok(audioAsset);
});

app.MapGet("/api/sfx", async (SfxDbContext dbContext) =>
{
    var audioAssets = await dbContext.AudioAssets.ToListAsync();

    return Results.Ok(audioAssets);
});

app.MapControllers();

app.Run();
