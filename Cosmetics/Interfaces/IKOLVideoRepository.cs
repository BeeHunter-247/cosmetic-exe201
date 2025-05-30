using Cosmetics.Models;

namespace Cosmetics.Interfaces
{
	public interface IKOLVideoRepository : IGenericRepository<Kolvideo>
	{
		Task<IEnumerable<Kolvideo>> GetAllByAffiliateProfileIdAsync(Guid affiliateProfileId);
	}
}
