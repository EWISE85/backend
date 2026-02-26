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

            // Mapbox Matrix API giới hạn tối đa 25 điểm tọa độ trong 1 request
            var chunks = destinations.Chunk(24);

            foreach (var chunk in chunks)
            {
                try
                {
                    // 1. CHUẨN HÓA TỌA ĐỘ (Lng,Lat + F6 + InvariantCulture)
                    // Phải dùng .ToString("F6") và InvariantCulture để tránh lỗi dấu phẩy ở máy chủ VN
                    string originStr = $"{originLng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{originLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}";

                    var destCoords = chunk.Select(d =>
                        $"{d.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{d.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");

                    // Nối chuỗi tọa độ: Origin đứng đầu (index 0), sau đó là các Destination
                    var coordinateString = $"{originStr};{string.Join(";", destCoords)}";

                    // sources=0 trỏ vào originStr
                    // destinations=1;2;3... trỏ vào danh sách destCoords
                    var destIndices = string.Join(";", Enumerable.Range(1, chunk.Length));

                    // 2. CẤU HÌNH RADIUSES (Rất quan trọng để fix 422)
                    // "unlimited" ép Mapbox tìm con đường gần nhất bất kể tọa độ User nằm sâu trong nhà/hẻm
                    // Số lượng phần tử trong radiuses phải bằng tổng số tọa độ (Nguồn + Các Đích)
                    var radiusList = string.Join(";", Enumerable.Repeat("unlimited", chunk.Length + 1));

                    // 3. XÂY DỰNG URL CHUẨN (Profile Walking cho xe máy/hẻm nhỏ)
                    var url = $"https://api.mapbox.com/directions-matrix/v1/mapbox/walking/{coordinateString}" +
                              $"?sources=0" +
                              $"&destinations={destIndices}" +
                              $"&radiuses={radiusList}" +
                              $"&annotations=distance" +
                              $"&access_token={_accessToken.Trim()}";

                    // --- CƠ CHẾ RETRY THÔNG MINH ---
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
                                    // Mapbox trả về mảng 2 chiều [nguồn][đích], ta lấy hàng đầu tiên (index 0)
                                    var distancesFromOrigin = data.Distances[0];
                                    for (int i = 0; i < chunk.Length; i++)
                                    {
                                        if (distancesFromOrigin[i].HasValue)
                                        {
                                            // Mapbox trả về mét -> Đổi sang Km
                                            result[chunk[i].SmallCollectionPointsId] = distancesFromOrigin[i].Value / 1000.0;
                                        }
                                    }
                                }
                                break; // Thoát vòng lặp retry nếu thành công
                            }

                            // Xử lý lỗi 429 (Rate Limit)
                            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            {
                                if (retry < maxRetries)
                                {
                                    Console.WriteLine($"[Mapbox 429] Quá tải. Đang đợi {delayMs}ms để thử lại lần {retry + 1}...");
                                    await Task.Delay(delayMs);
                                    delayMs *= 2;
                                    continue;
                                }
                            }

                            // Xử lý lỗi 422 (Tọa độ lỗi Snap - dù đã dùng unlimited)
                            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                            {
                                Console.WriteLine($"[Mapbox 422] Bỏ qua chunk này do không thể tìm đường đi bộ.");
                                break;
                            }

                            break; // Các lỗi khác không cần retry
                        }
                        catch (Exception ex)
                        {
                            if (retry == maxRetries) Console.WriteLine($"[Mapbox Exception] Post {chunk[0].SmallCollectionPointsId}: {ex.Message}");
                            else await Task.Delay(delayMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Critical Mapbox Error] Chunk processing failed: {ex.Message}");
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