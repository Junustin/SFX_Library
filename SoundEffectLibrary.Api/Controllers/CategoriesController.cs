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
        public async Task<ActionResult<Category>> AddCategory (
            CreateCategoryRequest request, 
            SfxDbContext dbContext)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Category name must not be null.");
            var category = new Category
            {
                
                CategoryName = request.CategoryName
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            return Ok(category);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Category>>> GetAllCategory(
            SfxDbContext dbContext)
        {
            var categories = await dbContext.Categories.ToListAsync();

            return Ok(categories);
        }
    }
}
