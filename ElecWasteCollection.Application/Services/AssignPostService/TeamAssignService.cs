using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class TeamAssignService : ITeamAssignService
    {
        public async Task<AssignTeamResult> AssignPostsToTeamsAsync(AssignTeamRequest request)
        {
            var result = new AssignTeamResult();

            if (request == null || request.PostIds == null || !request.PostIds.Any())
                throw new ArgumentException("Danh sách PostIds không được rỗng.");

            var distinctPostIds = request.PostIds.Distinct().ToList();

            var allPosts = FakeDataSeeder.posts;

            var selectedPosts = allPosts
                .Where(p => distinctPostIds.Contains(p.Id))
                .ToList();

            var notFoundIds = distinctPostIds
                .Except(selectedPosts.Select(p => p.Id))
                .ToList();

            foreach (var missingId in notFoundIds)
            {
                result.Unassigned.Add(new UnassignedTeamItem
                {
                    PostId = missingId,
                    Reason = "Post not found"
                });
            }

            if (!selectedPosts.Any())
                throw new Exception("Không có bài đăng hợp lệ.");

            if (!FakeDataSeeder.TeamRatios.Any())
                throw new Exception("Chưa cấu hình tỷ lệ team.");

            var ratios = FakeDataSeeder.TeamRatios
                .Where(r => r.RatioPercent > 0)
                .ToList();

            double totalRatio = ratios.Sum(r => r.RatioPercent);
            if (Math.Abs(totalRatio - 100) > 0.0001)
                throw new Exception($"Tổng tỷ lệ phải bằng 100%, hiện tại = {totalRatio}.");

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

            FakeDataSeeder.UnassignedTeamPosts.Clear();

            foreach (var post in selectedPosts)
            {
                if (post.CollectionTeamId != null)
                    continue;

                var user = FakeDataSeeder.userAddress.FirstOrDefault(u => u.UserId == post.SenderId);
                if (user == null || user.Iat == null || user.Ing == null)
                {
                    var un = new UnassignedTeamItem
                    {
                        PostId = post.Id,
                        Reason = "User missing or location missing"
                    };
                    result.Unassigned.Add(un);
                    FakeDataSeeder.UnassignedTeamPosts.Add(un);
                    continue;
                }

                var reachableTeams = new List<(TeamQuotaItem team, double minDistance)>();

                foreach (var teamQuota in quota.Where(q => q.Quota > 0))
                {
                    var myPoints = FakeDataSeeder.smallCollectionPoints
                        .Where(p => p.City_Team_Id == teamQuota.TeamId)
                        .ToList();

                    if (!myPoints.Any())
                        continue;

                    double bestDist = double.MaxValue;

                    foreach (var point in myPoints)
                    {
                        double dist = GeoHelper.DistanceKm(
                            point.Latitude,
                            point.Longitude,
                            user.Iat.Value,
                            user.Ing.Value
                        );

                        if (dist <= SystemConfig.MaxDistanceKm && dist < bestDist)
                        {
                            bestDist = dist;
                        }
                    }

                    if (bestDist < double.MaxValue)
                    {
                        reachableTeams.Add((teamQuota, bestDist));
                    }
                }

                if (!reachableTeams.Any())
                {
                    var un = new UnassignedTeamItem
                    {
                        PostId = post.Id,
                        Reason = $"No team within allowed distance ({SystemConfig.MaxDistanceKm} km)"
                    };
                    result.Unassigned.Add(un);
                    FakeDataSeeder.UnassignedTeamPosts.Add(un);
                    continue;
                }

                var chosen = reachableTeams
                    .OrderByDescending(t => t.team.Quota)
                    .ThenBy(t => t.minDistance)
                    .First();

                var chosenTeam = chosen.team;

                post.CollectionTeamId = chosenTeam.TeamId;
                chosenTeam.Quota--;

                result.Assigned.Add(new AssignedTeamItem
                {
                    PostId = post.Id,
                    TeamId = chosenTeam.TeamId,
                    RatioPercent = chosenTeam.Ratio,
                    DistanceKm = Math.Round(chosen.minDistance, 2),
                    Reason = $"Team can reach within {SystemConfig.MaxDistanceKm} km"
                });
            }

            result.Processed = selectedPosts.Count;
            return await Task.FromResult(result);
        }
    }
}
