namespace Cosmetics.DTO.KOLVideos
{
	public class KOLVideoUpdateDTO
	{
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid? ProductId { get; set; }
        public bool? IsActive { get; set; }
    }
}
