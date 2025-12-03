using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ElecWasteCollection.Application.Services
{
    public class MapboxMatrixClient
    {
        private readonly HttpClient _http;
        private readonly string _token;

        public MapboxMatrixClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _token = config["Mapbox:AccessToken"] ?? throw new Exception("Thiếu Token Mapbox");
        }

        public async Task<(long[,] distances, long[,] durations)> GetMatrixAsync(List<(double lat, double lng)> locations)
        {
            if (locations.Count < 2) return (new long[0, 0], new long[0, 0]);

            var coords = string.Join(";", locations.Select(l =>
                $"{l.lng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{l.lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            var url = $"https://api.mapbox.com/directions-matrix/v1/mapbox/driving/{coords}?annotations=distance,duration&access_token={_token}";

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("code", out var code) && code.GetString() != "Ok")
                throw new Exception("Lỗi Mapbox API");

            var dArr = doc.RootElement.GetProperty("distances");
            var tArr = doc.RootElement.GetProperty("durations");
            int n = locations.Count;

            var dist = new long[n, n];
            var time = new long[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dist[i, j] = (long)(dArr[i][j].ValueKind == JsonValueKind.Null ? 0 : dArr[i][j].GetDouble());
                    time[i, j] = (long)(tArr[i][j].ValueKind == JsonValueKind.Null ? 0 : tArr[i][j].GetDouble());
                }
            }
            return (dist, time);
        }
    }
}