using Cosmetics.Models;
using System.Linq.Expressions;

namespace Cosmetics.Interfaces
{
    public interface IAffiliateProfileRepository : IGenericRepository<AffiliateProfile>
    {
        Task<AffiliateProfile?> GetByUserIdAsync(int userId); 
    }
}
