using System.Net.Http;
using System.Text.Json;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

public class MapboxDirectionsClient
{
    private readonly HttpClient _http;
    private readonly string _token;

    public MapboxDirectionsClient(HttpClient http, IConfiguration config)
    {
        _http = http;

        _token = config["Mapbox:AccessToken"]
                 ?? throw new Exception("Missing Mapbox:AccessToken in appsettings.json");
    }

    public async Task<MapboxRoute?> GetRouteAsync(double startLat, double startLng, double endLat, double endLng)
    {
        string start = $"{startLng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}";
        string end = $"{endLng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}";

        string url = $"https://api.mapbox.com/directions/v5/mapbox/walking/{start};{end}?access_token={_token.Trim()}&geometries=geojson";

        try
        {
            var response = await _http.GetAsync(url);

            // Xử lý quá tải (429) ngay tại Client
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(2000); // Đợi 2 giây nếu bị chặn
                return null;
            }

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();

            // Sử dụng Options để không phân biệt hoa thường nếu cần
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<MapboxDirectionsResponse>(json, options);

            return data?.Routes?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public class MapboxDirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<MapboxRoute> Routes { get; set; }
    }

    public class MapboxRoute
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; } 

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }
}