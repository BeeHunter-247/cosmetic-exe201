using Cosmetics.Interfaces;
using Cosmetics.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Cosmetics.Repositories
{
	public class AffiliateProfileRepository : GenericRepository<AffiliateProfile>, IAffiliateProfileRepository
	{
		public AffiliateProfileRepository(ComedicShopDBContext context) : base(context)
		{
		}

		public async Task<AffiliateProfile?> GetByUserIdAsync(int userId)
		{
			return await _context.AffiliateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
		}
	}
}
