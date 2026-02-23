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

        public async Task<Dictionary<string, double>> GetMatrixDistancesAsync(double originLat, double originLng, List<SmallCollectionPoints> destinations)
        {
            var result = new Dictionary<string, double>();
            if (destinations == null || !destinations.Any()) return result;

            var chunks = destinations.Chunk(24); // Mapbox giới hạn tối đa 25 điểm trên 1 request

            foreach (var chunk in chunks)
            {
                // 1. CHUẨN HÓA DỮ LIỆU TỌA ĐỘ (Fix 422)
                string originStr = $"{originLng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{originLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}";
                var destCoords = chunk.Select(d =>
                    $"{d.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{d.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");

                var coordinateString = $"{originStr};{string.Join(";", destCoords)}";
                var destIndices = string.Join(";", Enumerable.Range(1, chunk.Length));

                var url = $"https://api.mapbox.com/directions-matrix/v1/mapbox/driving/{coordinateString}" +
                          $"?sources=0&destinations={destIndices}&annotations=distance&access_token={_accessToken.Trim()}";

                // 2. CƠ CHẾ RETRY & BACKOFF (Fix 429)
                int maxRetries = 3;
                int delayMs = 1000;

                for (int retry = 0; retry <= maxRetries; retry++)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var data = await response.Content.ReadFromJsonAsync<MapboxMatrixResponse>();
                            if (data?.Distances != null && data.Distances.Length > 0)
                            {
                                var distancesFromOrigin = data.Distances[0];
                                for (int i = 0; i < chunk.Length; i++)
                                {
                                    if (distancesFromOrigin[i].HasValue)
                                        result[chunk[i].SmallCollectionPointsId] = distancesFromOrigin[i].Value / 1000.0;
                                }
                            }
                            break; // Thành công -> Thoát vòng lặp retry cho chunk này
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Lỗi 429
                        {
                            if (retry < maxRetries)
                            {
                                Console.WriteLine($"[Mapbox 429] Quá tải. Đang đợi {delayMs}ms để thử lại lần {retry + 1}...");
                                await Task.Delay(delayMs);
                                delayMs *= 2; // Tăng gấp đôi thời gian chờ
                                continue;
                            }
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity) // Lỗi 422
                        {
                            Console.WriteLine($"[Mapbox 422] Tọa độ không hợp lệ. Bỏ qua chunk này.");
                            break; // 422 là lỗi dữ liệu, retry không giải quyết được
                        }

                        break; // Các lỗi khác không xử lý retry
                    }
                    catch (Exception ex)
                    {
                        if (retry == maxRetries) Console.WriteLine($"[Mapbox Error]: {ex.Message}");
                        else await Task.Delay(delayMs);
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