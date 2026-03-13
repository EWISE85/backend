using ElecWasteCollection.Application.IServices;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService.Mapbox
{
	public class MapboxService : IMapboxService
	{
		private readonly HttpClient _httpClient;
		private readonly MapboxSettings _settings;
		public MapboxService(HttpClient httpClient, IOptions<MapboxSettings> settings)
		{
			_httpClient = httpClient;
			_settings = settings.Value;
		}
		public async Task<(double Latitude, double Longitude)?> GetCoordinatesFromAddressAsync(string address)
		{
			if (string.IsNullOrWhiteSpace(address)) return null;

			try
			{
				var encodedAddress = Uri.EscapeDataString(address);
				// Gọi API với AccessToken từ appsettings
				var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{encodedAddress}.json?access_token={_settings.AccessToken}&limit=1";

				var response = await _httpClient.GetAsync(url);
				if (!response.IsSuccessStatusCode) return null;

				var jsonString = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(jsonString);
				var root = doc.RootElement;

				if (root.TryGetProperty("features", out var features) && features.GetArrayLength() > 0)
				{
					var center = features[0].GetProperty("center");
					double longitude = center[0].GetDouble();
					double latitude = center[1].GetDouble();

					return (latitude, longitude);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Mapbox Geocoding: {ex.Message}");
			}

			return null;
		}
	}
}
