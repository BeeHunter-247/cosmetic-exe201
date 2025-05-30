namespace Cosmetics.DTO.KOLVideos
{
	public class KOLVideoDTO
	{
		public Guid VideoId { get; set; }

		public Guid AffiliateProfileId { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string VideoUrl { get; set; }

		public Guid ProductId { get; set; }

		public DateTime? CreatedAt { get; set; }

		public bool? IsActive { get; set; }
	}
}
