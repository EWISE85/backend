using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class SettingsService : ISettingsService
    {
        public DistanceSettingsResponse GetDistanceSettings()
        {
            return new DistanceSettingsResponse
            {
                MaxDistanceKm = SystemConfig.MaxDistanceKm
            };
        }

        public DistanceSettingsResponse UpdateDistanceSettings(DistanceSettingsRequest request)
        {
            if (request == null)
                throw new ArgumentException("Request cannot be null.");

            if (request.MaxDistanceKm <= 0 || request.MaxDistanceKm > 1000)
                throw new ArgumentException("MaxDistanceKm must be between 0.1 and 1000.");

            SystemConfig.MaxDistanceKm = request.MaxDistanceKm;

            return new DistanceSettingsResponse
            {
                MaxDistanceKm = SystemConfig.MaxDistanceKm
            };
        }
    }
}
