using System.ComponentModel.DataAnnotations;

namespace Cosmetics.DTO.KOLVideos
{
	public class KOLVideoCreateDTO
	{
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        public string Description { get; set; }

        [Required]
        public IFormFile VideoFile { get; set; }

        [Required]
        public Guid ProductId { get; set; }
    }
}
