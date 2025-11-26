using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class TeamRatioService : ITeamRatioService
    {
        public Task<TeamRatioConfigResponse> UpdateRatios(TeamRatioConfigRequest request)
        {
            if (request == null || !request.Teams.Any())
                throw new Exception("Danh sách tỉ lệ team không được rỗng.");

            double total = request.Teams.Sum(x => x.RatioPercent);
            if (Math.Abs(total - 100) > 0.0001)
                throw new Exception("Tổng tỷ lệ phải bằng 100%.");

            FakeDataSeeder.TeamRatios = request.Teams;

            return Task.FromResult(new TeamRatioConfigResponse
            {
                Message = "Updated successfully",
                Ratios = request.Teams
            });
        }
    }
}
