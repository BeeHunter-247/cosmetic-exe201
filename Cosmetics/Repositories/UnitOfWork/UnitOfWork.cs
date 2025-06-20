using Cosmetics.Interfaces;
using Cosmetics.Models;
using Cosmetics.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cosmetics.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ComedicShopDBContext _context;
        public IOrderRepository Orders { get; }
        public IOrderDetailRepository OrderDetails { get; }
        public IBrandRepository Brands { get; }
        public ICategoryRepository Categories { get; }
        public IProductRepository Products { get; }
        public IUserRepository Users { get; }
        public IAffiliateRepository Affiliates { get; }
        public IPaymentTransactionRepository PaymentTransactions { get; }
        public ICartDetailRepository CartDetails { get; }
        public IKOLVideoRepository KolVideos { get; }
        public IAffiliateProfileRepository AffiliateProfiles { get; }

        
        public IGenericRepository<Image> Image { get; }
        public IGenericRepository<SkinAnalysisResult> SkinAnalysisResult { get; }

        


        // Uncomment if you need AffiliateLinks later
        // public IAffiliateLinkRepository AffiliateLinks { get; }

        public UnitOfWork(
            ComedicShopDBContext context,
            IOrderRepository orderRepository,
            IOrderDetailRepository orderDetailRepository,
            IBrandRepository brandRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IAffiliateRepository affiliateRepository,
            IPaymentTransactionRepository paymentTransactionRepository,
            ICartDetailRepository cartDetailRepository,
            IKOLVideoRepository kolVideoRepository,
            IAffiliateProfileRepository affiliateProfileRepository,
            IGenericRepository<Image> imageRepository,
            IGenericRepository<SkinAnalysisResult> skinAnalysisResultRepository

          
            // Uncomment and add if needed
            // IAffiliateLinkRepository affiliateLinkRepository
            )
        {
            _context = context;
            Orders = orderRepository;
            OrderDetails = orderDetailRepository;
            Brands = brandRepository;
            Categories = categoryRepository;
            Products = productRepository;
            Users = userRepository;
            Affiliates = affiliateRepository;
            PaymentTransactions = paymentTransactionRepository; // Fixed: Assigned
            CartDetails = cartDetailRepository;
            KolVideos = kolVideoRepository;
            AffiliateProfiles = affiliateProfileRepository;
            Image = imageRepository;
            SkinAnalysisResult = skinAnalysisResultRepository;

            // Uncomment if needed
            // AffiliateLinks = affiliateLinkRepository;
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync(IDbContextTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            await transaction.CommitAsync();
        }

        public async Task RollbackAsync(IDbContextTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            await transaction.RollbackAsync();
        }

        public void Dispose() => _context.Dispose();
    }
}