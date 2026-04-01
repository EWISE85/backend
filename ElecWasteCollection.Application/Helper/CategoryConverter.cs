using System;
using System.Collections.Generic;
using System.Linq;

namespace ElecWasteCollection.Application.Helper
{
	public static class CategoryConverter
	{
		private static readonly Dictionary<string, List<string>> CategoryTagsMap =
			new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
		{
			// === 1. Đồ gia dụng lớn ===
			{ "Tủ lạnh", new List<string> { "refrigerator", "fridge", "freezer", "home appliance", "white goods", "refrigeration system" } },
			{ "Máy giặt", new List<string> { "washing machine", "washer", "laundry", "home appliance", "white goods" } },
			{ "Máy sấy quần áo", new List<string> { "clothes dryer", "dryer", "laundry", "home appliance", "white goods" } },
			{ "Máy rửa bát", new List<string> { "dishwasher", "home appliance" , "white goods" } },
			{ "Máy điều hòa", new List<string> { "air conditioner", "air conditioning", "home appliance", "white goods" } },
			{ "Bình nước nóng", new List<string> { "water heater", "boiler", "white goods" } },
			{ "Lò nướng", new List<string> { "oven", "kitchen appliance", "white goods"  } },
			{ "Lò vi sóng", new List<string> { "microwave", "microwave oven", "kitchen appliance", "white goods" } },

			// === 2. Đồ điện tử Tiêu dùng & Giải trí ===
			{ "Tivi", new List<string> { "television", "tv set", "monitor", "screen", "consumer electronics" } },
			{ "Màn hình máy tính", new List<string> { "monitor", "screen", "computer monitor", "display" } },
			{ "Dàn âm thanh (Loa, Amply)", new List<string> { "audio system", "speaker", "amplifier", "sound system" } },
			{ "Máy chơi game (Console)", new List<string> { "game console", "playstation", "xbox", "nintendo" } },
			{ "Đầu đĩa (DVD, VCD, Blu-ray)", new List<string> { "dvd player", "blu-ray player", "player" } },

			// === 3. Thiết bị IT và Viễn thông ===
			{ "Máy tính để bàn (PC)", new List<string> { "computer", "desktop computer", "pc", "personal computer" } },
			{ "Laptop (Máy tính xách tay)", new List<string> { "laptop", "notebook", "computer" } },
			{ "Điện thoại di động", new List<string> { "smartphone", "mobile phone", "cellphone", "phone" } },
			{ "Máy tính bảng (Tablet)", new List<string> { "tablet", "tablet computer", "ipad" } },
			{ "Máy in", new List<string> { "printer", "office equipment" } },
			{ "Máy scan", new List<string> { "scanner", "office equipment" } },
			{ "Thiết bị mạng (Router, Modem)", new List<string> { "router", "modem", "network switch" } },

			// === 4. Đồ gia dụng nhỏ ===
			{ "Nồi cơm điện", new List<string> { "rice cooker", "kitchen appliance" } },
			{ "Ấm đun nước", new List<string> { "kettle", "electric kettle", "kitchen appliance" } },
			{ "Máy xay sinh tố", new List<string> { "blender", "kitchen appliance" } },
			{ "Quạt điện", new List<string> { "fan", "electric fan" } },
			{ "Máy hút bụi", new List<string> { "vacuum cleaner", "vacuum" } },
			{ "Bàn là (Bàn ủi)", new List<string> { "iron", "steam iron" } },
			{ "Máy sấy tóc", new List<string> { "hair dryer" } },
			{ "Máy pha cà phê", new List<string> { "coffee maker", "coffee machine" } },

			// === 5. Phụ kiện và Pin ===
			{ "Pin (các loại)", new List<string> { "battery", "batteries", "power" } },
			{ "Cáp sạc, Bộ sạc", new List<string> { "charger", "cable", "adapter" } },
			{ "Tai nghe", new List<string> { "headphones", "earphones", "headset" } },
			{ "Chuột máy tính", new List<string> { "mouse", "computer mouse" } },
			{ "Bàn phím", new List<string> { "keyboard", "computer keyboard" } },
			{ "Điều khiển (Remote)", new List<string> { "remote control" } }
		};

		public static List<string> GetAcceptedEnglishTags(string vietnameseCategory)
		{
			if (string.IsNullOrWhiteSpace(vietnameseCategory))
			{
				return new List<string> { "electronics", "appliance" }; // Mặc định chung
			}

			string normalizedCategory = vietnameseCategory.Trim();

			if (CategoryTagsMap.TryGetValue(normalizedCategory, out List<string> acceptedTags))
			{
				return acceptedTags.Select(tag => tag.ToLower()).ToList();
			}

			// Mặc định an toàn
			return new List<string> { "electronics", "appliance" };
		}
	}
}