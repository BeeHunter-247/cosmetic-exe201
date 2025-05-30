namespace Cosmetics.DTO.Category
{
    public class CategoryDTO
    {
        public Guid CategoryId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
