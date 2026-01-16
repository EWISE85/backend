using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class MapboxDistanceCacheService : IMapboxDistanceCacheService
    {
        private readonly MapboxDirectionsClient _client;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private static readonly Dictionary<string, (double dist, double eta)> _cache = new();

        private readonly string _accessToken;

        public MapboxDistanceCacheService(
            MapboxDirectionsClient client,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _client = client;
            _httpClient = httpClient;
            _configuration = configuration;

            _accessToken = _configuration["Mapbox:AccessToken"]
                           ?? throw new ArgumentNullException("Mapbox:AccessToken");
        }

        public async Task<double> GetRoadDistanceKm(double latA, double lngA, double latB, double lngB)
        {
            var (dist, _) = await GetRoadDistanceAndEta(latA, lngA, latB, lngB);
            return dist;
        }

        public async Task<(double distanceKm, double durationMinutes)> GetRoadDistanceAndEta(
            double latA, double lngA, double latB, double lngB)
        {
            string key = $"{latA},{lngA}|{latB},{lngB}";
            if (_cache.ContainsKey(key)) return _cache[key];

            var route = await _client.GetRouteAsync(latA, lngA, latB, lngB);
            if (route != null && route.Distance > 0)
            {
                double distKm = route.Distance / 1000.0;
                double etaMin = route.Duration / 60.0;
                _cache[key] = (distKm, etaMin);
                return (distKm, etaMin);
            }

            double fallback = GeoHelper.DistanceKm(latA, lngA, latB, lngB);
            _cache[key] = (fallback, 0);
            return (fallback, 0);
        }

        public async Task<Dictionary<string, double>> GetMatrixDistancesAsync( double originLat, double originLng,
            List<SmallCollectionPoints> destinations)
        {
            var result = new Dictionary<string, double>();
            if (destinations == null || !destinations.Any()) return result;

            var chunks = destinations.Chunk(24);

            foreach (var chunk in chunks)
            {
                var coords = new List<string> { $"{originLng},{originLat}" };
                coords.AddRange(chunk.Select(d => $"{d.Longitude},{d.Latitude}"));
                var coordinateString = string.Join(";", coords);
                var destIndices = string.Join(";", Enumerable.Range(1, chunk.Length));

                var url = $"https://api.mapbox.com/directions-matrix/v1/mapbox/driving/{coordinateString}" +
                          $"?sources=0&destinations={destIndices}&annotations=distance&access_token={_accessToken}";

                // -- RETRY LOGIC --
                int maxRetries = 3;
                int currentRetry = 0;
                bool success = false;

                while (currentRetry <= maxRetries && !success)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(url);

                        // Nếu gặp lỗi 429 -> Đợi và thử lại
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            currentRetry++;
                            int delayMs = 1000 * (int)Math.Pow(2, currentRetry); // 2s, 4s, 8s...
                            await Task.Delay(delayMs);
                            continue;
                        }

                        response.EnsureSuccessStatusCode();

                        var data = await response.Content.ReadFromJsonAsync<MapboxMatrixResponse>();
                        if (data?.Distances != null && data.Distances.Length > 0)
                        {
                            var distances = data.Distances[0];
                            for (int i = 0; i < chunk.Length; i++)
                            {
                                if (distances[i].HasValue)
                                {
                                    double km = distances[i].Value / 1000.0;
                                    var pointId = chunk[i].SmallCollectionPointsId;
                                    if (!result.ContainsKey(pointId)) result.Add(pointId, km);
                                }
                            }
                        }
                        success = true;
                    }
                    catch (Exception)
                    {
                        if (currentRetry < maxRetries)
                        {
                            currentRetry++;
                            await Task.Delay(1000);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private class MapboxMatrixResponse
        {
            public double?[][] Distances { get; set; }
        }
    }
}