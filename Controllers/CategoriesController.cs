using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Models;

namespace SoundEffectLibrary.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<Category>> AddCategory (
            CreateCategoryRequest request, 
            SfxDbContext dbContext)
        {
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
