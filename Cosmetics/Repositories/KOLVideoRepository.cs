using Cosmetics.Interfaces;
using Cosmetics.Models;
using Microsoft.EntityFrameworkCore;

namespace Cosmetics.Repositories
{
	public class KOLVideoRepository : GenericRepository<Kolvideo>, IKOLVideoRepository
	{
		public KOLVideoRepository(ComedicShopDBContext context) : base(context)
		{
		}

		public async Task<IEnumerable<Kolvideo>> GetAllByAffiliateProfileIdAsync(Guid affiliateProfileId)
		{
			return await _context.Kolvideos.Where(k => k.AffiliateProfileId == affiliateProfileId).ToListAsync();
		}
	}
}
