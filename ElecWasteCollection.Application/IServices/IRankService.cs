using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IRankService
    {
        Task<double> UpdateUserRankImpactAsync(User user, Guid productId);
        Task<object?> GetRankProgressAsync(Guid userId);
        Task<IEnumerable<Rank>> GetAllRanksAsync();
        Task<IEnumerable<object>> GetTopGreenUsersAsync(int top = 10);
    }
}
