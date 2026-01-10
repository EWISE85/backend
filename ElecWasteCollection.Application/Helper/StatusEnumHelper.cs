using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Helper
{
	public static class StatusEnumHelper
    {
		public static string GetDescription<T>(T enumValue) where T : Enum
		{
			FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());

			if (field == null) return enumValue.ToString();

			var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);

			return attributes.Length > 0 ? attributes[0].Description : enumValue.ToString();
		}

		// Lấy giá trị Enum từ mô tả
		// vd: "Chờ duyệt" => ProductStatus.CHO_DUYET
		public static T GetValueFromDescription<T>(string description) where T : Enum
		{
			foreach (var field in typeof(T).GetFields())
			{
				var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

				if (attribute != null)
				{
					if (attribute.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
						return (T)field.GetValue(null);
				}
				else
				{
					if (field.Name.Equals(description, StringComparison.OrdinalIgnoreCase))
						return (T)field.GetValue(null);
				}
			}

			throw new ArgumentException($"Không tìm thấy Enum nào khớp với mô tả: {description}", nameof(description));
		}

		// Chuyển mã trạng thái từ database thành tên tiếng Việt
		// vd: "CHO_DUYET" => "Chờ duyệt"
		public static string ConvertDbCodeToVietnameseName<T>(string dbCode) where T : struct, Enum
		{
			if (Enum.TryParse<T>(dbCode, true, out var result))
			{
				return GetDescription(result);
			}
			return dbCode; 
		}
	}
}
