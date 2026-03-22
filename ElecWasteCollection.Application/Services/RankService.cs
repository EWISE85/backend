using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

public class RankService : IRankService
{
    private readonly IUnitOfWork _unitOfWork;

    public RankService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<double> UpdateUserRankImpactAsync(User user, Guid productId)
    {
        var product = await _unitOfWork.Products.GetAsync(
            filter: p => p.ProductId == productId,
            includeProperties: "Category,Category.ParentCategory,ProductValues.Attribute"
        );

        if (product == null || user == null) return 0;

        double actualWeight = 0;

        if (product.ProductValues != null && product.ProductValues.Any())
        {

            var weightAttr = product.ProductValues.FirstOrDefault(v =>
                v.Attribute != null && (
                    v.Attribute.Name.Contains("Trọng lượng", StringComparison.OrdinalIgnoreCase) ||
                    v.Attribute.Name.Contains("Khối lượng", StringComparison.OrdinalIgnoreCase)));

            if (weightAttr != null && weightAttr.Value.HasValue)
            {
                actualWeight = weightAttr.Value.Value; 
            }
        }

        if (actualWeight <= 0)
        {
            actualWeight = product.Category.DefaultWeight > 0 ? product.Category.DefaultWeight : 1.0;
        }

        double factor = product.Category.ParentCategory?.EmissionFactor ?? product.Category.EmissionFactor;
        if (factor <= 0) factor = 0.5;

        double co2Saved = actualWeight * factor;

        user.TotalCo2Saved += co2Saved;

        // 6. Check Rank
        var allRanks = await _unitOfWork.Ranks.GetAllAsync();
        var applicableRank = allRanks
            .Where(r => r.MinCo2 <= user.TotalCo2Saved)
            .OrderByDescending(r => r.MinCo2)
            .FirstOrDefault();

        if (applicableRank != null)
        {
            user.CurrentRankId = applicableRank.RankId;
        }

        return co2Saved;
    }

    public async Task<object?> GetRankProgressAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetAsync(
            filter: u => u.UserId == userId,
            includeProperties: "Rank"
        );

        if (user == null) return null;

        var allRanks = await _unitOfWork.Ranks.GetAllAsync();

        var nextRank = allRanks
            .Where(r => r.MinCo2 > user.TotalCo2Saved)
            .OrderBy(r => r.MinCo2)
            .FirstOrDefault();

        return new
        {
            UserId = user.UserId,
            CurrentRankName = user.Rank?.RankName ?? "Chưa có hạng",
            CurrentCo2 = Math.Round(user.TotalCo2Saved, 2),
            NextRankName = nextRank?.RankName ?? "Kim cương",
            Co2ToNextRank = nextRank != null ? Math.Round(nextRank.MinCo2 - user.TotalCo2Saved, 2) : 0,
            RankIcon = user.Rank?.IconUrl
        };
    }

    public async Task<IEnumerable<Rank>> GetAllRanksAsync()
    {
        var ranks = await _unitOfWork.Ranks.GetAllAsync();

        return ranks
            .OrderBy(r => r.MinCo2) 
            .Take(4)               
            .Select(r => new Rank
            {
                RankId = r.RankId,
                RankName = r.RankName,
                MinCo2 = r.MinCo2,
                IconUrl = r.IconUrl
            });
    }
    public async Task<IEnumerable<object>> GetTopGreenUsersAsync(int top = 10)
    {
        var users = await _unitOfWork.Users.GetAllAsync(
            filter: u => u.TotalCo2Saved > 0,
            includeProperties: "Rank"
        );

        return users
            .OrderByDescending(u => u.TotalCo2Saved)
            .Take(top)
            .Select((u, index) => new
            {
                RankPosition = index + 1,
                UserId = u.UserId,
                UserName = u.Name,
                Avatar = u.Avatar,
                TotalCo2Saved = Math.Round(u.TotalCo2Saved, 2),
                RankName = u.Rank?.RankName ?? "Chưa có hạng",
                RankIcon = u.Rank?.IconUrl
            });
    }
}