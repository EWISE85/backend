using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.Entities;

namespace ElecWasteCollection.Application.Services
{
    public class TeamAssignService : ITeamAssignService
    {
        private readonly List<UserAddress> _userAddress = FakeDataSeeder.userAddress;
		public async Task<AssignTeamResult> AssignPostsToTeamsAsync(AssignTeamRequest request)
        {
            var result = new AssignTeamResult();

            var selectedPosts = FakeDataSeeder.posts
                .Where(p => request.PostIds.Contains(p.Id))
                .ToList();

            if (!selectedPosts.Any())
                throw new Exception("Không có bài đăng hợp lệ.");

            if (!FakeDataSeeder.TeamRatios.Any())
                throw new Exception("Chưa cấu hình tỷ lệ team.");

            var ratios = FakeDataSeeder.TeamRatios;

            int totalPosts = selectedPosts.Count;

            var quota = ratios.Select(x => new TeamQuotaItem
            {
                TeamId = x.TeamId,
                Ratio = x.RatioPercent,
                Quota = (int)Math.Round((totalPosts * x.RatioPercent) / 100.0)
            }).ToList();

            int diff = totalPosts - quota.Sum(x => x.Quota);
            if (diff != 0)
            {
                var biggest = quota.OrderByDescending(x => x.Ratio).First();
                biggest.Quota += diff;
            }

            foreach (var team in quota)
            {
                var myPoints = FakeDataSeeder.smallCollectionPoints
                    .Where(p => p.City_Team_Id == team.TeamId)
                    .ToList();

                if (!myPoints.Any())
                    continue;

                var unassigned = selectedPosts
                    .Where(p => p.CollectionTeamId == null)
                    .ToList();

                foreach (var post in unassigned)
                {
                    if (team.Quota == 0) break;

                    var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);
                    var userAddress = _userAddress.FirstOrDefault(ua => ua.UserId == user.UserId);
					double bestDist = double.MaxValue;
                    SmallCollectionPoints? bestPoint = null;

                    foreach (var point in myPoints)
                    {
                        if (userAddress.Iat == null || userAddress.Ing == null) continue;

                        double dist = GeoHelper.DistanceKm(
                            point.Latitude,
                            point.Longitude,
							userAddress.Iat.Value,
							userAddress.Ing.Value
                        );

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestPoint = point;
                        }
                    }

                    if (bestPoint == null) continue;

                    post.CollectionTeamId = team.TeamId;

                    result.Assigned.Add(new AssignedTeamItem
                    {
                        PostId = post.Id,
                        TeamId = team.TeamId,
                        RatioPercent = team.Ratio,
                        DistanceKm = Math.Round(bestDist, 2),
                        Reason = "Closest small point + respecting ratio"
                    });

                    team.Quota--;
                }
            }

            result.Processed = result.Assigned.Count;
            return await Task.FromResult(result);
        }
    }
}
