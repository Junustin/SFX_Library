using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Dtos;
using SoundEffectLibrary.Api.Models;

namespace SoundEffectLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [EnableRateLimiting("per-ip")]
    public class CategoriesController : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "AssetManager")]
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult<Category>> AddCategory (
            CreateCategoryRequest request, 
            SfxDbContext dbContext)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Category name must not be empty.",
                    Status = 400
                });
            var category = new Category
            {
                
                CategoryName = request.CategoryName
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            return Ok(category);
        }

        [HttpGet]
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult<IReadOnlyList<Category>>> GetAllCategory(
            SfxDbContext dbContext)
        {
            var categories = await dbContext.Categories.ToListAsync();

            return Ok(categories);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AssetManager")]
        [EnableRateLimiting("per-ip")]
        public async Task<IActionResult> DeleteCategory(
            int id, 
            SfxDbContext dbContext,
            CancellationToken cancellationToken)
        {
            await dbContext
                .Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
