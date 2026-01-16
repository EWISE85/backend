using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IMapboxDistanceCacheService
    {
        Task<double> GetRoadDistanceKm(double latA, double lngA, double latB, double lngB);
        Task<(double distanceKm, double durationMinutes)> GetRoadDistanceAndEta( double latA, double lngA, double latB, double lngB);
        Task<Dictionary<string, double>> GetMatrixDistancesAsync( double originLat, double originLng, List<SmallCollectionPoints> destinations );
    }
}
