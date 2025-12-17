using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElecWasteCollection.API.Helper
{
	public class VietnamDateTimeJsonConverter : JsonConverter<DateTime>
	{
		private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

		private static TimeZoneInfo GetVietnamTimeZone()
		{
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
			}
			catch (TimeZoneNotFoundException)
			{
				return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
			}
		}

		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return reader.GetDateTime().ToUniversalTime();
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			DateTime utcValue = value.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(value, DateTimeKind.Utc)
				: value.ToUniversalTime();

			DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcValue, VietnamTimeZone);

			writer.WriteStringValue(vietnamTime.ToString("yyyy-MM-ddTHH:mm:ss"));
		}
	}
}
