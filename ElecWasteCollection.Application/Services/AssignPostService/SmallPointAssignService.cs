using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class SmallPointAssignService : ISmallPointAssignService
    {
        public async Task<AssignSmallPointResult> AssignSmallPointsAsync(int teamId)
        {
            if (teamId <= 0)
                throw new ArgumentException("teamId phải lớn hơn 0.");

            var result = new AssignSmallPointResult { TeamId = teamId };

            var teamPoints = FakeDataSeeder.smallCollectionPoints
                .Where(p => p.City_Team_Id == teamId)
                .ToList();

            if (!teamPoints.Any())
                throw new Exception("Team không có điểm thu gom nhỏ nào.");

            var posts = FakeDataSeeder.posts
                .Where(p => p.CollectionTeamId == teamId && p.AssignedSmallPointId == null)
                .ToList();

            FakeDataSeeder.OutOfRangeSmallPointPosts.Clear();

            foreach (var post in posts)
            {
                var user = FakeDataSeeder.userAddress.FirstOrDefault(u => u.UserId == post.SenderId);

                if (user == null || user.Iat == null || user.Ing == null)
                {
                    var outItem = new OutOfRangeSmallPointItem
                    {
                        PostId = post.Id,
                        Reason = "User missing or location missing"
                    };
                    result.OutOfRange.Add(outItem);
                    FakeDataSeeder.OutOfRangeSmallPointPosts.Add(outItem);
                    continue;
                }

                double bestDist = double.MaxValue;
                SmallCollectionPoints? bestPoint = null;

                foreach (var point in teamPoints)
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
                        bestPoint = point;
                    }
                }

                if (bestPoint == null)
                {
                    var outItem = new OutOfRangeSmallPointItem
                    {
                        PostId = post.Id,
                        Reason = $"All points farther than {SystemConfig.MaxDistanceKm} km"
                    };
                    result.OutOfRange.Add(outItem);
                    FakeDataSeeder.OutOfRangeSmallPointPosts.Add(outItem);
                    continue;
                }

                post.AssignedSmallPointId = bestPoint.Id;

                result.Assigned.Add(new AssignedSmallPointItem
                {
                    PostId = post.Id,
                    SmallPointId = bestPoint.Id,
                    SmallPointName = bestPoint.Name,
                    DistanceKm = Math.Round(bestDist, 2)
                });
            }

            return await Task.FromResult(result);
        }
    }
}
