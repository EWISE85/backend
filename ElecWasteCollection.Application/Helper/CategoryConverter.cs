using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Helper
{
	public static class CategoryConverter
	{
		private static readonly Dictionary<string, List<string>> CategoryTagsMap =
	new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
{
	{ "Điện tử Tiêu dùng", new List<string> {
		"consumer electronics",
		"television", 
        "monitor",
		"smartphone",
		"digital camera"
	}},
    
    { "Điện tử Gia dụng", new List<string> {
		"home appliance",
		"refrigerator",
		"washing machine",
		"microwave"
	}},

	{ "Điện tử Máy tính/Viễn thông", new List<string> {
		"it and telecom",
		"computer",
		"laptop",
		"network"
	}},
};

		public static List<string> GetAcceptedEnglishTags(string vietnameseCategory)
		{
			if (string.IsNullOrWhiteSpace(vietnameseCategory))
			{
				return new List<string> { "electronics" };
			}

			string normalizedCategory = vietnameseCategory.Trim();

			if (CategoryTagsMap.TryGetValue(normalizedCategory, out List<string> acceptedTags))
			{
				return acceptedTags.Select(tag => tag.ToLower()).ToList();
			}

			// Mặc định an toàn
			return new List<string> { "electronics" };
		}
	}
}
