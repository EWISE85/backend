using System.Net.Http;
using System.Text.Json;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.Extensions.Configuration;

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

    public async Task<MapboxRoute?> GetRouteAsync(
        double startLat, double startLng,
        double endLat, double endLng)
    {
        string start = $"{startLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        string end = $"{endLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        string url =
            $"https://api.mapbox.com/directions/v5/mapbox/driving/{start};{end}" +
            $"?access_token={_token}" +
            $"&geometries=geojson&overview=full&language=vi&steps=false";

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<MapboxDirectionsResponse>(json);

        return data?.Routes?.FirstOrDefault();
    }
}