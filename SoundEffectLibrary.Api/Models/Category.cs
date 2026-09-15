namespace SoundEffectLibrary.Models
{
    public class Category 
    { 
        public int Id { get; set; } 
        public string CategoryName { get; set; } = ""; 
    }

    public record CreateCategoryRequest(string CategoryName);
    public record CategoryResponse(int Id, string CategoryName);
}
