using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Data
{
	public static class FakeDataSeeder
	{
		private static readonly Guid size_TiviVua = Guid.Parse("235406ca-2b0f-4bd7-94a6-c9bc23096a43");
		private static readonly Guid prod_TiviMoi = Guid.Parse("b1111111-1111-1111-1111-000000000013");
		private static readonly Guid post_TiviMoi = Guid.Parse("a0000000-0000-0000-0000-000000000004");
		// === 1. USERS ===
		public static List<User> users = new()
		{
			// ... (Giữ nguyên 5 users) ...
			new User
			{
				UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),
				Name = "Trần Văn An",
				Email = "tran.van.an@example.com",
				Phone = "0901234567",
				Address = "Vinhomes Grand Park – Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Avatar = "https://picsum.photos/id/1011/200/200",
				Iat = 10.842003,
				Ing = 106.829580
			},
			new User
			{
				UserId = Guid.Parse("b73a62a7-8b90-43cf-9ad7-2abf96f34a52"),
				Name = "Lê Thị Mai",
				Email = "le.thi.mai@example.com",
				Phone = "0987654321",
				Address = "Vincom Mega Mall Grand Park – Đường Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Avatar = "https://picsum.photos/id/1025/200/200",
				Iat = 10.843450,
				Ing = 106.829900
			},
			new User
			{
				UserId = Guid.Parse("e9b4b9de-b3b0-49ad-b90c-74c24a26b57a"),
				Name = "Nguyễn Minh Khôi",
				Email = "nguyen.minh.khoi@example.com",
				Phone = "0908123456",
				Address = "Trường THCS Long Thạnh Mỹ – Đường Long Thạnh Mỹ, TP. Thủ Đức",
				Avatar = "https://picsum.photos/id/1033/200/200",
				Iat = 10.845900,
				Ing = 106.833400
			},
			new User
			{
				UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"),
				Name = "Phạm Thị Hằng",
				Email = "pham.thi.hang@example.com",
				Phone = "0911222333",

				Address = "UBND Phường Long Thạnh Mỹ – 86 Nguyễn Xiển, TP. Thủ Đức",
				Avatar = "https://picsum.photos/id/1045/200/200",
				Iat = 10.841000,
				Ing = 106.830000
			},
			new User
			{
				UserId = Guid.Parse("c40deff9-163b-49e8-b967-238f22882b63"),
				Name = "Đỗ Quốc Bảo",
				Email = "do.quoc.bao@example.com",
				Phone = "0977222333",
				Address = "Công viên Ánh Sáng Vinhomes – Khu đô thị Vinhomes Grand Park",
				Avatar = "https://picsum.photos/id/1059/200/200",
				Iat = 10.839000,
				Ing = 106.833800
			}
		};

		// === 2. HELPER FUNCTION (TẠO LỊCH) ===
		private static string CreateSchedule(int daysFromNow, string start, string end)
		{
			var schedule = new List<DailyTimeSlots>
			{
				new DailyTimeSlots
				{
					DayName = $"Thứ {((int)DateTime.Now.AddDays(daysFromNow).DayOfWeek == 0 ? 8 : (int)DateTime.Now.AddDays(daysFromNow).DayOfWeek + 1)}",
					PickUpDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysFromNow)),
					Slots = new TimeSlotDetail { StartTime = start, EndTime = end }
				}
			};
			return JsonSerializer.Serialize(schedule);
		}

		// === 3. CATEGORIES (VÀ HELPER IDs) ===
		private static readonly Guid parent1_Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
		private static readonly Guid parent2_Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
		private static readonly Guid parent3_Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
		private static readonly Guid parent4_Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
		private static readonly Guid parent5_Id = Guid.Parse("55555555-5555-5555-5555-555555555555");

		public static List<Category> categories = new()
		{
			// ... (Giữ nguyên Categories) ...
			new Category { Id = parent1_Id, Name = "Đồ gia dụng lớn", ParentCategoryId = null },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000001"), Name = "Tủ lạnh", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000002"), Name = "Máy giặt", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000003"), Name = "Máy sấy quần áo", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000004"), Name = "Máy rửa bát", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000005"), Name = "Máy điều hòa", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000006"), Name = "Bình nước nóng", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000007"), Name = "Lò nướng", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000008"), Name = "Lò vi sóng", ParentCategoryId = parent1_Id },

			new Category { Id = parent2_Id, Name = "Đồ điện tử Tiêu dùng & Giải trí", ParentCategoryId = null },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000001"), Name = "Tivi", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000002"), Name = "Màn hình máy tính", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000003"), Name = "Dàn âm thanh (Loa, Amply)", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000004"), Name = "Máy chơi game (Console)", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000005"), Name = "Đầu đĩa (DVD, VCD, Blu-ray)", ParentCategoryId = parent2_Id },

			new Category { Id = parent3_Id, Name = "Thiết bị IT và Viễn thông", ParentCategoryId = null },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000001"), Name = "Máy tính để bàn (PC)", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000002"), Name = "Laptop (Máy tính xách tay)", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000003"), Name = "Điện thoại di động", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000004"), Name = "Máy tính bảng (Tablet)", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000005"), Name = "Máy in", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000006"), Name = "Máy scan", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000007"), Name = "Thiết bị mạng (Router, Modem)", ParentCategoryId = parent3_Id },

			new Category { Id = parent4_Id, Name = "Đồ gia dụng nhỏ", ParentCategoryId = null },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000001"), Name = "Nồi cơm điện", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000002"), Name = "Ấm đun nước", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000003"), Name = "Máy xay sinh tố", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000004"), Name = "Quạt điện", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000005"), Name = "Máy hút bụi", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000006"), Name = "Bàn là (Bàn ủi)", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000007"), Name = "Máy sấy tóc", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000008"), Name = "Máy pha cà phê", ParentCategoryId = parent4_Id },

			new Category { Id = parent5_Id, Name = "Phụ kiện và Pin", ParentCategoryId = null },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000001"), Name = "Pin (các loại)", ParentCategoryId = parent5_Id },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000002"), Name = "Cáp sạc, Bộ sạc", ParentCategoryId = parent5_Id },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000003"), Name = "Tai nghe", ParentCategoryId = parent5_Id },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000004"), Name = "Chuột máy tính", ParentCategoryId = parent5_Id },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000005"), Name = "Bàn phím", ParentCategoryId = parent5_Id },
			new Category { Id = Guid.Parse("55555555-5555-5555-5555-000000000006"), Name = "Điều khiển (Remote)", ParentCategoryId = parent5_Id }
		};

		// === 4. ATTRIBUTES (VÀ HELPER IDs) ===
		public static List<Attributes> attributes = new()
		{
			// ... (Giữ nguyên Attributes) ...
			new Attributes { Id = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000001"), Name = "Kích thước màn hình (inch)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000001"), Name = "Chiều dài (cm)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002"), Name = "Chiều rộng (cm)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003"), Name = "Chiều cao (cm)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000001"), Name = "Dung tích (lít)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0004-0004-0004-000000000001"), Name = "Khối lượng giặt (kg)", },
			new Attributes { Id = Guid.Parse("a1a1a1a1-0009-0009-0009-000000000001"), Name = "Trọng lượng (kg)", }
		};

		// --- Helper IDs (để dùng trong CategoryAttributes, SizeTiers, Products...) ---
		private static readonly Guid cat_TuLanh = Guid.Parse("11111111-1111-1111-1111-000000000001");
		private static readonly Guid cat_MayGiat = Guid.Parse("11111111-1111-1111-1111-000000000002");
		private static readonly Guid cat_BinhNuocNong = Guid.Parse("11111111-1111-1111-1111-000000000006");
		private static readonly Guid cat_LoViSong = Guid.Parse("11111111-1111-1111-1111-000000000008");
		private static readonly Guid cat_Tivi = Guid.Parse("22222222-2222-2222-2222-000000000001");
		private static readonly Guid cat_ManHinhMayTinh = Guid.Parse("22222222-2222-2222-2222-000000000002");
		private static readonly Guid cat_Loa = Guid.Parse("22222222-2222-2222-2222-000000000003");
		private static readonly Guid cat_MayTinhDeBan = Guid.Parse("33333333-3333-3333-3333-000000000001");
		private static readonly Guid cat_Laptop = Guid.Parse("33333333-3333-3333-3333-000000000002");
		private static readonly Guid cat_DienThoai = Guid.Parse("33333333-3333-3333-3333-000000000003");
		private static readonly Guid cat_MayIn = Guid.Parse("33333333-3333-3333-3333-000000000005");
		private static readonly Guid cat_QuatDien = Guid.Parse("44444444-4444-4444-4444-000000000004");
		private static readonly Guid cat_MayHutBui = Guid.Parse("44444444-4444-4444-4444-000000000005");

		private static readonly Guid att_KichThuocManHinh = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000001");
		private static readonly Guid att_ChieuDai = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000001");
		private static readonly Guid att_ChieuRong = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002");
		private static readonly Guid att_ChieuCao = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003");
		private static readonly Guid att_DungTich = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000001");
		private static readonly Guid att_KhoiLuongGiat = Guid.Parse("a1a1a1a1-0004-0004-0004-000000000001");
		private static readonly Guid att_TrongLuong = Guid.Parse("a1a1a1a1-0009-0009-0009-000000000001");


		// === 5. CATEGORY_ATTRIBUTES (Bảng "Luật") ===
		public static List<CategoryAttributes> categoryAttributes = new()
		{
			// ... (Giữ nguyên CategoryAttributes) ...
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_Tivi, AttributeId = att_KichThuocManHinh },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_Tivi, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, AttributeId = att_KichThuocManHinh },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_Laptop, AttributeId = att_KichThuocManHinh },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_Laptop, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayGiat, AttributeId = att_KhoiLuongGiat },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayGiat, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayGiat, AttributeId = att_ChieuDai },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayGiat, AttributeId = att_ChieuRong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayGiat, AttributeId = att_ChieuCao },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_TuLanh, AttributeId = att_DungTich },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_TuLanh, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_TuLanh, AttributeId = att_ChieuDai },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_TuLanh, AttributeId = att_ChieuRong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_TuLanh, AttributeId = att_ChieuCao },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_LoViSong, AttributeId = att_DungTich },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_LoViSong, AttributeId = att_TrongLuong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_LoViSong, AttributeId = att_ChieuDai },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_LoViSong, AttributeId = att_ChieuRong },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_LoViSong, AttributeId = att_ChieuCao },
			new CategoryAttributes { Id = Guid.NewGuid(), CategoryId = cat_MayHutBui, AttributeId = att_TrongLuong },
		};


		// === 6. SIZETIER (Bảng "Quy ước" S/M/L) ===
		private static readonly Guid st_Tivi_TrungBinh = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000002");
		private static readonly Guid st_TuLanh_Lon = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003");
		private static readonly Guid st_MayGiat_TrungBinh = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000002");
		private static readonly Guid st_Laptop_MongNhe = Guid.Parse("a1a1a1a1-0006-0006-0006-000000000001");

		public static List<SizeTier> sizeTiers = new()
		{
			// ... (Giữ nguyên SizeTiers) ...
			new SizeTier { SizeTierId = size_TiviVua, CategoryId = cat_Tivi, Name = "Nhỏ (Dưới 32 inch)", EstimatedWeight = 5, EstimatedVolume = 0.1 },
			new SizeTier { SizeTierId = st_Tivi_TrungBinh, CategoryId = cat_Tivi, Name = "Trung bình (32-55 inch)", EstimatedWeight = 15, EstimatedVolume = 0.3 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_Tivi, Name = "Lớn (Trên 55 inch)", EstimatedWeight = 30, EstimatedVolume = 0.6 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "Nhỏ (Mini, Dưới 150L)", EstimatedWeight = 30, EstimatedVolume = 0.5 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "Trung bình (150-300L)", EstimatedWeight = 50, EstimatedVolume = 1.0 },
			new SizeTier { SizeTierId = st_TuLanh_Lon, CategoryId = cat_TuLanh, Name = "Lớn (Trên 300L)", EstimatedWeight = 80, EstimatedVolume = 1.5 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_MayGiat, Name = "Nhỏ (Dưới 7kg)", EstimatedWeight = 35, EstimatedVolume = 0.4 },
			new SizeTier { SizeTierId = st_MayGiat_TrungBinh, CategoryId = cat_MayGiat, Name = "Trung bình (7-10kg)", EstimatedWeight = 50, EstimatedVolume = 0.6 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_MayGiat, Name = "Lớn (Trên 10kg)", EstimatedWeight = 70, EstimatedVolume = 0.8 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_LoViSong, Name = "Nhỏ (Dưới 20L)", EstimatedWeight = 10, EstimatedVolume = 0.05 },
			new SizeTier { SizeTierId = Guid.Parse("f3c8c4ef-56f3-433e-b210-3f900248ffae"), CategoryId = cat_LoViSong, Name = "Lớn (Trên 20L)", EstimatedWeight = 15, EstimatedVolume = 0.1 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Nhỏ (Dưới 24 inch)", EstimatedWeight = 3, EstimatedVolume = 0.05 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Lớn (Từ 24 inch trở lên)", EstimatedWeight = 7, EstimatedVolume = 0.1 },
			new SizeTier { SizeTierId = st_Laptop_MongNhe, CategoryId = cat_Laptop, Name = "Mỏng nhẹ (Dưới 2kg)", EstimatedWeight = 1.5, EstimatedVolume = 0.01 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Thường/Gaming (Từ 2kg trở lên)", EstimatedWeight = 3, EstimatedVolume = 0.02 },
		};

		// === 7. PRODUCTS (ĐÃ CẬP NHẬT 'Status' ĐỂ NHẤT QUÁN) ===
		public static List<Products> products = new List<Products>
		{
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000001"),
				CategoryId = cat_Tivi,
				SizeTierId = st_Tivi_TrungBinh,
				Description = "Tivi Samsung 42 inch hỏng màn hình.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã đóng gói" // Cập nhật: Đã thu gom hôm qua, đã nhập kho, đã đóng gói
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000002"),
				CategoryId = cat_MayGiat,
				SizeTierId = st_MayGiat_TrungBinh,
				Description = "Máy giặt Toshiba không hoạt động nữa.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã nhập kho" // Cập nhật: Đã thu gom hôm qua, đã nhập kho
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000003"),
				CategoryId = cat_MayTinhDeBan,
				SizeTierId = null,
				Description = "CPU Intel i3 đời cũ, màn hình Dell 19 inch.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã nhập kho" // Cập nhật: Đã thu gom hôm qua, đã nhập kho
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000004"),
				CategoryId = cat_TuLanh,
				SizeTierId = st_TuLanh_Lon,
				Description = "Tủ lạnh Panasonic không còn làm lạnh.",
				// Status gốc: "Chờ thu gom"
				Status = "Chờ thu gom" // Giữ nguyên: Post bị rejected, route bị hủy
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000005"),
				CategoryId = cat_Laptop,
				SizeTierId = st_Laptop_MongNhe,
				Description = "Laptop Acer bị vỡ màn hình.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã nhập kho" // Cập nhật: Đã thu gom hôm qua, đã nhập kho
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000006"),
				CategoryId = cat_DienThoai,
				SizeTierId = null,
				Description = "iPhone 7 bị chai pin.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã thu gom" // Cập nhật: Đã thu gom hôm nay
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000007"),
				CategoryId = cat_Loa,
				SizeTierId = null,
				Description = "Loa JBL mini không sạc được.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã thu gom" // Cập nhật: Đã thu gom hôm nay
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000008"),
				CategoryId = cat_BinhNuocNong,
				SizeTierId = null,
				Description = "Bình Ariston bị rò điện.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã thu gom" // Cập nhật: Đã thu gom hôm nay
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000009"),
				CategoryId = cat_MayIn,
				SizeTierId = null,
				Description = "Máy in HP cũ, không còn dùng.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã thu gom" // Cập nhật: Đã thu gom hôm nay
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000010"),
				CategoryId = cat_QuatDien,
				SizeTierId = null,
				Description = "Quạt Asia cũ, gãy cánh.",
				// Status gốc: "Chờ thu gom"
				Status = "Đã thu gom" // Cập nhật: Đã thu gom hôm nay
			},
			new Products
			{
				Id = Guid.Parse("b1111111-1111-1111-1111-000000000012"),
				CategoryId = cat_LoViSong, // ID: 1111...0008
				SizeTierId = Guid.Parse("f3c8c4ef-56f3-433e-b210-3f900248ffae"),
				Description = "bị hư",
				Status = "Chờ Duyệt"
			},
			new Products // Item 12
			{
				Id = prod_TiviMoi,
				CategoryId = cat_Tivi, // ID: 2222...0001
				SizeTierId = size_TiviVua,
				Description = "bị hư",
				Status = "Chờ Duyệt" // Vì AI confidence thấp
			}
		};

		// === 8. PRODUCT_VALUES ===
		public static List<ProductValues> productValues = new List<ProductValues>
		{
			// ... (Giữ nguyên ProductValues) ...
			// Values cho Product 3 (Máy tính)
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[2].Id, AttributeId = att_KichThuocManHinh, Value = 19 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[2].Id, AttributeId = att_TrongLuong, Value = 7 },
			// Values cho Product 8 (Bình nước nóng)
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[7].Id, AttributeId = att_DungTich, Value = 30 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[7].Id, AttributeId = att_TrongLuong, Value = 15 },
			// Values cho Product 9 (Máy in)
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_TrongLuong, Value = 8 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuDai, Value = 40 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuRong, Value = 35 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuCao, Value = 20 },
		};

		// === 9. POSTS ===
		public static List<Post> posts = new()
		{
			// ... (Giữ nguyên 10 posts) ...
			// Post 1 (Liên kết Product 1 - Tivi)
			new Post
			{
				Id = Guid.Parse("a2d7b801-b0fb-4f7d-9b83-b741d23666a1"), // ID CŨ
				SenderId = users[0].UserId,
				ProductId = products[0].Id,
				Name = "Thu gom tivi cũ",
				Description = "Vui lòng đến vào buổi chiều.",
				Date = DateTime.Now.AddDays(-2), // Cập nhật: 2 ngày trước
				Address = users[0].Address,
				ScheduleJson = CreateSchedule(0, "08:00", "09:00"), // Cập nhật: Lịch hôm qua
				Status = "Đã Duyệt"
			},
			// Post 2 (Liên kết Product 2 - Máy giặt)
			new Post
			{
				Id = Guid.Parse("b34c1223-7545-41d2-9e42-67d75e3c2a31"), // ID CŨ
				SenderId = users[1].UserId,
				ProductId = products[1].Id,
				Name = "Máy giặt hỏng cần thu gom",
				Description = "Máy giặt Toshiba, lấy cẩn thận giúp.",
				Date = DateTime.Now.AddDays(-5),
				Address = users[1].Address,
				ScheduleJson = CreateSchedule(0, "09:00", "10:00"), // Cập nhật: Lịch hôm qua
				Status = "Đã Duyệt"
			},
			// Post 3 (Liên kết Product 3 - Máy tính)
			new Post
			{
				Id = Guid.Parse("c1b63fa1-ec52-44a0-8a9c-8b83f8d1b8c3"), // ID CŨ
				SenderId = users[2].UserId,
				ProductId = products[2].Id,
				Name = "Máy tính cũ không dùng nữa",
				Description = "Gồm 1 case và 1 màn hình.",
				Date = DateTime.Now.AddDays(-2),
				Address = users[2].Address,
				ScheduleJson = CreateSchedule(0, "10:00", "11:00"), // Cập nhật: Lịch hôm qua
				Status = "Đã Duyệt"
			},
			// Post 4 (Liên kết Product 4 - Tủ lạnh)
			new Post
			{
				Id = Guid.Parse("d9a86de5-7d27-43d0-9f55-49094b30947d"), // ID CŨ
				SenderId = users[3].UserId,
				ProductId = products[3].Id,
				Name = "Tủ lạnh hỏng cần xử lý",
				Description = "Tủ lạnh to, cần 2 người khiêng.",
				Date = DateTime.Now.AddDays(-8),
				Address = users[3].Address,
				ScheduleJson = CreateSchedule(0, "11:00", "12:00"), // Cập nhật: Lịch hôm qua
				Status = "Đã Từ Chối",
				RejectMessage = "Hình ảnh không rõ ràng."
			},
			// Post 5 (Liên kết Product 5 - Laptop)
			new Post
			{
				Id = Guid.Parse("e0f92a77-188b-402b-a0ea-3b1c68891ac0"), // ID CŨ
				SenderId = users[4].UserId,
				ProductId = products[4].Id,
				Name = "Laptop bị vỡ màn hình",
				Description = "Chỉ thu gom laptop, không kèm sạc.",
				Date = DateTime.Now.AddDays(-1),
				Address = users[4].Address,
				ScheduleJson = CreateSchedule(0, "13:00", "14:00"), // Cập nhật: Lịch hôm qua
				Status = "Đã Duyệt"
			},
			// Post 6 (Liên kết Product 6 - Điện thoại)
			new Post
			{
				Id = Guid.Parse("f2c3cc25-f7d7-4b0a-bd1c-69a2dfb6b211"), // ID CŨ
				SenderId = users[0].UserId,
				ProductId = products[5].Id,
				Name = "Điện thoại cũ bị chai pin",
				Description = "iPhone 7.",
				Date = DateTime.Now.AddDays(-4),
				Address = users[0].Address,
				ScheduleJson = CreateSchedule(1, "14:00", "15:00"), // Cập nhật: Lịch hôm nay
				Status = "Đã Duyệt"
			},
			// Post 7 (Liên kết Product 7 - Loa)
			new Post
			{
				Id = Guid.Parse("a82d6f7b-f1e7-45dc-83ec-7b3e2db21a4f"), // ID CŨ
				SenderId = users[1].UserId,
				ProductId = products[6].Id,
				Name = "Loa Bluetooth bị hỏng",
				Description = "Loa JBL.",
				Date = DateTime.Now.AddDays(-6),
				Address = users[1].Address,
				ScheduleJson = CreateSchedule(1, "15:00", "16:00"), // Cập nhật: Lịch hôm nay
				Status = "Đã Duyệt"
			},
			// Post 8 (Liên kết Product 8 - Bình nước nóng)
			new Post
			{
				Id = Guid.Parse("b0b8c58b-4921-4e7d-9b09-0840f994e98e"), // ID CŨ
				SenderId = users[2].UserId,
				ProductId = products[7].Id,
				Name = "Bình nước nóng hỏng",
				Description = "Bình Ariston, vẫn còn trên tường, cần gỡ.",
				Date = DateTime.Now.AddDays(-9),
				Address = users[2].Address,
				ScheduleJson = CreateSchedule(1, "16:00", "17:00"), // Cập nhật: Lịch hôm nay
				Status = "Đã Duyệt"
			},
			// Post 9 (Liên kết Product 9 - Máy in)
			new Post
			{
				Id = Guid.Parse("c9955eab-20a8-463f-b6db-4d20382195c3"), // ID CŨ
				SenderId = users[3].UserId,
				ProductId = products[8].Id,
				Name = "Máy in văn phòng cũ",
				Description = "Máy in HP.",
				Date = DateTime.Now.AddDays(-7),
				Address = users[3].Address,
				ScheduleJson = CreateSchedule(1, "17:00", "18:00"), // Cập nhật: Lịch hôm nay
				Status = "Đã Duyệt"
			},
			// Post 10 (Liên kết Product 10 - Quạt điện)
			new Post
			{
				Id = Guid.Parse("e62aefc7-0e61-4b35-9d59-6b8e10d2b01e"), // ID CŨ
				SenderId = users[4].UserId,
				ProductId = products[9].Id,
				Name = "Quạt điện hỏng cánh",
				Description = "Quạt Asia.",
				Date = DateTime.Now.AddDays(-10),
				Address = users[4].Address,
				ScheduleJson = CreateSchedule(1, "18:00", "19:00"), // Cập nhật: Lịch hôm nay
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"),
				SenderId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),
				ProductId = products[10].Id, // Trỏ đến Product 12 (lò vi sóng)
				Name = "lò vi sóng ko xài được nữa",
				Description = "",
				Date = DateTime.Now.AddDays(-3),
				Address = "string",
				ScheduleJson = "[{\"dayName\":\"T6\",\"pickUpDate\":\"2025-11-02\",\"slots\":{\"startTime\":\"09:00\",\"endTime\":\"10:00\"}}]",
				Status = "Chờ Duyệt", // <-- Vì AI confidence < 80%
				RejectMessage = null,
				CheckMessage = new List<string>()
			},
			new Post // Item 12
			{
				Id = post_TiviMoi,
				SenderId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),
				ProductId = prod_TiviMoi,
				Name = "tivi ko xài được nữa",
				Description = "",
				Date = DateTime.Now.AddDays(-1), // Vừa tạo
				Address = "string",
				ScheduleJson = "[{\"dayName\":\"T6\",\"pickUpDate\":\"2025-11-02\",\"slots\":{\"startTime\":\"09:00\",\"endTime\":\"10:00\"}}]",
				Status = "Chờ Duyệt", // <-- TRẠNG THÁI CHÍNH
				RejectMessage = null,
				CheckMessage = new List<string>()
			}
		};

		// === 10. POST IMAGES ===
		public static List<PostImages> postImages = new()
		{
			// ... (Giữ nguyên PostImages) ...
			// Ảnh cho Post 1 (Tivi)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[0].Id, // a2d7b801...
				ImageUrl = "https://tse4.mm.bing.net/th/id/OIP.LuRXEsdA9472ZA06zqLEswHaHa?pid=Api&P=0&h=180",
				AiDetectedLabelsJson = "[{\"Tag\":\"television\",\"Confidence\":98.5},{\"Tag\":\"screen\",\"Confidence\":92.1},{\"Tag\":\"electronics\",\"Confidence\":85.0}]"
			},
			// Ảnh cho Post 2 (Máy giặt)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[1].Id, // b34c1223...
				ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.nqDpXYFDMJ4J3SHRuHJfCAHaF7?pid=Api&P=0&h=180",
				AiDetectedLabelsJson = "[{\"Tag\":\"washing machine\",\"Confidence\":99.2},{\"Tag\":\"home appliance\",\"Confidence\":95.0},{\"Tag\":\"laundry\",\"Confidence\":70.1}]"
			},
			// Ảnh cho Post 3 (Máy tính)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[2].Id, // c1b63fa1...
				ImageUrl = "https://mccvietnam.vn/media/lib/26-09-2022/b-pc-mcc-1920x1080.png",
				AiDetectedLabelsJson = "[{\"Tag\":\"computer\",\"Confidence\":97.0},{\"Tag\":\"monitor\",\"Confidence\":90.3},{\"Tag\":\"desktop computer\",\"Confidence\":88.0},{\"Tag\":\"electronics\",\"Confidence\":85.0}]"
			},
			// Ảnh cho Post 4 (Tủ lạnh - Bị từ chối)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[3].Id, // d9a86de5...
				ImageUrl = "https://picsum.photos/id/203/400/300",
				AiDetectedLabelsJson = "[{\"Tag\":\"blurry\",\"Confidence\":70.0},{\"Tag\":\"dark\",\"Confidence\":65.0},{\"Tag\":\"unclear\",\"Confidence\":50.0}]"
			},
			// Ảnh cho Post 5 (Laptop)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[4].Id, // e0f92a77...
				ImageUrl = "https://vinhphatstore.vn/wp-content/uploads/2022/09/cach-sua-man-hinh-laptop-bi-vo-hieu-qua-triet-de-3-1.jpg",
				AiDetectedLabelsJson = "[{\"Tag\":\"laptop\",\"Confidence\":99.0},{\"Tag\":\"computer\",\"Confidence\":95.0},{\"Tag\":\"broken screen\",\"Confidence\":92.0}]"
			},
			// Ảnh cho Post 6 (Điện thoại)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[5].Id, // f2c3cc25...
				ImageUrl = "https://cdn.nguyenkimmall.com/images/product/829/dien-thoai-iphone-14-pro-max-1tb-tim-1.jpg",
				AiDetectedLabelsJson = "[{\"Tag\":\"smartphone\",\"Confidence\":99.8},{\"Tag\":\"iphone\",\"Confidence\":95.0},{\"Tag\":\"mobile phone\",\"Confidence\":90.0}]"
			},
			// Ảnh cho Post 7 (Loa)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[6].Id, // a82d6f7b...
				ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.h0WESAKXTusQdzs5QSsLVAHaHa?pid=Api&P=0&h=180",
				AiDetectedLabelsJson = "[{\"Tag\":\"speaker\",\"Confidence\":96.0},{\"Tag\":\"bluetooth\",\"Confidence\":80.0},{\"Tag\":\"audio\",\"Confidence\":75.0}]"
			},
			// Ảnh cho Post 8 (Bình nước nóng)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[7].Id, // b0b8c58b...
				ImageUrl = "https://media.eproshop.vn/file/Ggw3EQpfr",
				AiDetectedLabelsJson = "[{\"Tag\":\"water heater\",\"Confidence\":94.0},{\"Tag\":\"boiler\",\"Confidence\":85.0},{\"Tag\":\"home appliance\",\"Confidence\":80.0}]"
			},
			// Ảnh cho Post 9 (Máy in)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[8].Id, // c9955eab...
				ImageUrl = "https://cdn.tgdd.vn/Files/2019/01/24/1146335/may-in-da-nang-la-gi.jpg",
				AiDetectedLabelsJson = "[{\"Tag\":\"printer\",\"Confidence\":99.0},{\"Tag\":\"office equipment\",\"Confidence\":90.0},{\"Tag\":\"copier\",\"Confidence\":85.0}]"
			},
			// Ảnh cho Post 10 (Quạt điện)
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[9].Id, // e62aefc7...
				ImageUrl = "https://meta.vn/Data/image/2020/07/01/quat-dung-dien-co-91-qd-cn450p5.jpg",
				AiDetectedLabelsJson = "[{\"Tag\":\"fan\",\"Confidence\":98.0},{\"Tag\":\"electric fan\",\"Confidence\":92.0}]"
			},
			new PostImages
			{
				PostImageId = Guid.NewGuid(),
				PostId = posts[10].Id, // Trỏ đến Post 12 (lò vi sóng)
				ImageUrl = "https://cdn.nguyenkimmall.com/images/detailed/616/10042970-lo-vi-song-sharp-23l-r-31a2vn-s-01.jpg",
				AiDetectedLabelsJson = "[{\"tag\":\"equipment\",\"confidence\":50.56,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"technology\",\"confidence\":49.04,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"microwave\",\"confidence\":45.45,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"screen\",\"confidence\":40.33,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"kitchen appliance\",\"confidence\":34.71,\"status\":\"Phù hợp với danh mục\"}]"
			},
			new PostImages // Ảnh cho Item 12
			{
				PostImageId = Guid.NewGuid(),
				PostId = post_TiviMoi,
				ImageUrl = "https://img.lovepik.com/free-png/20220125/lovepik-tv-monitor-png-image_401728080_wh1200.png",
				AiDetectedLabelsJson = "[{\"tag\":\"monitor\",\"confidence\":51.47,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"screen\",\"confidence\":39.17,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"television\",\"confidence\":36.41,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"display\",\"confidence\":34.05,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"computer\",\"confidence\":31.71,\"status\":\"Không phù hợp với danh mục\"}]"
			}
		};

		// === 10. COLLECTOR & ROUTES (ĐÃ CẬP NHẬT 'Status' ĐỂ NHẤT QUÁN) ===
		public static Collector collector = new()
		{
			CollectorId = Guid.Parse("6df4af85-6a59-4a0a-8513-1d7859fbd789"),
			Name = "Ngô Văn Dũng",
			Email = "ngo.van.dung@ewc.vn",
			Phone = "0905999888",
			Avatar = "https://picsum.photos/id/1062/200/200"
		};

		public static List<CollectionRoutes> collectionRoutes = new()
		{
			// === 5 ngày hôm qua (Đã hoàn thành/hủy) ===
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d1f2cde2-0e2a-4a8e-b5a0-60d34e8d3b90"),
				PostId = posts[0].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(8, 0),
				Actual_Time = new TimeOnly(8, 20),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/301/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d2f2cde2-0e2a-4a8e-b5a0-60d34e8d3b91"),
				PostId = posts[1].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(9, 0),
				Actual_Time = new TimeOnly(9, 15),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/302/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d3f2cde2-0e2a-4a8e-b5a0-60d34e8d3b92"),
				PostId = posts[2].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(10, 0),
				Actual_Time = new TimeOnly(10, 10),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/303/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d4f2cde2-0e2a-4a8e-b5a0-60d34e8d3b93"),
				PostId = posts[3].Id, // Post Tủ lạnh (bị reject)
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(11, 0),
				Actual_Time = new TimeOnly(11, 30),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/304/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hủy bỏ",
				RejectMessage = "Rác không phù hợp loại đăng ký."
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d5f2cde2-0e2a-4a8e-b5a0-60d34e8d3b94"),
				PostId = posts[4].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(13, 0),
				Actual_Time = new TimeOnly(13, 15),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/305/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành"
			},

			// === 5 ngày hôm nay (Cũng đã hoàn thành, vì giờ là buổi tối) ===
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("e1f2cde2-0e2a-4a8e-b5a0-60d34e8d3b95"),
				PostId = posts[5].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now),
				EstimatedTime = new TimeOnly(14, 0), // Sửa giờ cho hợp lý
				Actual_Time = new TimeOnly(14, 10),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/306/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành" // Cập nhật (vì đã cuối ngày)
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("e2f2cde2-0e2a-4a8e-b5a0-60d34e8d3b96"),
				PostId = posts[6].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now),
				EstimatedTime = new TimeOnly(15, 0),
				Actual_Time = new TimeOnly(15, 5),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/307/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành" // Cập nhật
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("e3f2cde2-0e2a-4a8e-b5a0-60d34e8d3b97"),
				PostId = posts[7].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now),
				EstimatedTime = new TimeOnly(16, 0),
				Actual_Time = new TimeOnly(16, 20),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/308/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành" // Cập nhật
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("e4f2cde2-0e2a-4a8e-b5a0-60d34e8d3b98"),
				PostId = posts[8].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now),
				EstimatedTime = new TimeOnly(17, 0),
				Actual_Time = new TimeOnly(17, 10),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/309/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành" // Cập nhật
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("e5f2cde2-0e2a-4a8e-b5a0-60d34e8d3b99"),
				PostId = posts[9].Id,
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now),
				EstimatedTime = new TimeOnly(18, 0),
				Actual_Time = new TimeOnly(18, 15),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/310/400/300" },
				LicensePlate = "51A-12345",
				Status = "Hoàn thành" // Cập nhật
			},

			// === 5 ngày mai (Chưa bắt đầu) ===
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f1f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba0"),
				PostId = posts[0].Id, // Tái sử dụng post 1
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(8, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/311/400/300" },
				LicensePlate = "51A-12345",
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f2f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba1"),
				PostId = posts[1].Id, // Tái sử dụng post 2
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(9, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/312/400/300" },
				LicensePlate = "51A-12345",
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f3f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba2"),
				PostId = posts[2].Id, // Tái sử dụng post 3
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(10, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/313/400/300" },
				LicensePlate = "51A-12345",
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f4f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba3"),
				PostId = posts[3].Id, // Tái sử dụng post 4
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(11, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/314/400/300" },
				LicensePlate = "51A-12345",
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f5f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba4"),
				PostId = posts[4].Id, // Tái sử dụng post 5
				CollectorId = collector.CollectorId,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(13, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/315/400/300" },
				LicensePlate = "51A-12345",
				Status = "Chưa bắt đầu"
			}
		};

		// === 11. SMALL COLLECTION POINTS ===
		public static List<SmallCollectionPoints> smallCollectionPoints = new()
		{
			// ... (Giữ nguyên SmallCollectionPoints) ...
			new SmallCollectionPoints
			{
				Id = 1,
				Name = "Trạm Thu Gom Vinhomes Grand Park",
				Address = "Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Latitude = 10.841500,
				Longitude = 106.830200,
				Status = "active",
				City_Team_Id = 1,
				Created_At = DateTime.Now.AddMonths(-1),
				Updated_At = DateTime.Now
			},
			new SmallCollectionPoints
			{
				Id = 2,
				Name = "Trạm Thu Gom Khu Công Nghệ Cao",
				Address = "Xa lộ Hà Nội, P. Hiệp Phú, TP. Thủ Đức",
				Latitude = 10.850300,
				Longitude = 106.787800,
				Status = "active",
				City_Team_Id = 1,
				Created_At = DateTime.Now.AddMonths(-1),
				Updated_At = DateTime.Now
			}
		};

		// === 12. COLLECTION TEAMS ===
		public static List<CollectionTeams> collectionTeams = new()
		{
			// ... (Giữ nguyên CollectionTeams) ...
			new CollectionTeams
			{
				Id = 1,
				Name = "Đội Thu Gom TP. Thủ Đức",
				Contact_Person = "Nguyễn Văn Hùng",
				Phone = "0909123123",
				City = "TP. Hồ Chí Minh",
				Status = "active",
				Created_At = DateTime.Now.AddMonths(-2),
				Updated_At = DateTime.Now
			}
		};

		// === 13. VEHICLES ===
		public static List<Vehicles> vehicles = new()
		{
			// ... (Giữ nguyên Vehicles) ...
			new Vehicles
			{
				Id = 1,
				Plate_Number = "51A-12345",
				Vehicle_Type = "Xe tải nhỏ",
				Capacity_Kg = 1000,
				Capacity_M3 = 8,
				Radius_Km = 10,
				Status = "active",
				Small_Collection_Point = 1
			},
			new Vehicles
			{
				Id = 2,
				Plate_Number = "51B-67890",
				Vehicle_Type = "Xe tải lớn",
				Capacity_Kg = 2000,
				Capacity_M3 = 15,
				Radius_Km = 15,
				Status = "active",
				Small_Collection_Point = 1
			}
		};

		// === 14. SHIFTS ===
		public static List<Shifts> shifts = new()
		{
			// ... (Giữ nguyên Shifts) ...
			// --- Ca hôm nay ---
			new Shifts
			{
				Id = 1,
				User_Id = users.First().UserId.GetHashCode(),
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today),
				Shift_Start_Time = DateTime.Today.AddHours(7),
				Shift_End_Time = DateTime.Today.AddHours(15)
			},
			new Shifts
			{
				Id = 2,
				User_Id = users.Last().UserId.GetHashCode(),
				Vehicle_Id = 2,
				WorkDate = DateOnly.FromDateTime(DateTime.Today),
				Shift_Start_Time = DateTime.Today.AddHours(8),
				Shift_End_Time = DateTime.Today.AddHours(16)
			},

			// --- Ca ngày mai ---
			new Shifts
			{
				Id = 3,
				User_Id = users.First().UserId.GetHashCode(),
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
				Shift_Start_Time = DateTime.Today.AddDays(1).AddHours(7),
				Shift_End_Time = DateTime.Today.AddDays(1).AddHours(15)
			},
			new Shifts
			{
				Id = 4,
				User_Id = users.Last().UserId.GetHashCode(),
				Vehicle_Id = 2,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
				Shift_Start_Time = DateTime.Today.AddDays(1).AddHours(8),
				Shift_End_Time = DateTime.Today.AddDays(1).AddHours(16)
			},

			// --- Ca ngày kia ---
			new Shifts
			{
				Id = 5,
				User_Id = users.First().UserId.GetHashCode(),
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
				Shift_Start_Time = DateTime.Today.AddDays(2).AddHours(7),
				Shift_End_Time = DateTime.Today.AddDays(2).AddHours(15)
			},
		};

		// === 15. COLLECTION GROUPS & ROUTES (Dùng để lưu kết quả grouping) ===
		public static List<CollectionGroups> collectionGroups = new();
		// public static List<CollectionRoutes> collectionRoutes = new(); // ĐÃ KHAI BÁO Ở #10


		// ==================================================================
		// === 16. DATA FAKE MỚI CHO CÁC BẢNG HISTORY ===
		// ==================================================================



		public static List<ProductStatusHistory> productStatusHistories = new()
{
	// --- Product 1 (Tivi) - [DATA ĐẦY ĐỦ 7 BƯỚC] ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[0].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[0].Address}", ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].Actual_Time) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "at_warehouse", StatusDescription = "Đã nhập kho Trạm Vinhomes.", ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].Actual_Time).AddHours(4) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "packaged", StatusDescription = "Đã đóng gói vào thùng T-001.", ChangedAt = DateTime.Now.AddHours(-8) }, // 8 tiếng trước
	
    // === DỮ LIỆU MỚI THÊM VÀO ===
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "in_transit", StatusDescription = "Thùng T-001 đang được chuyển đến trung tâm tái chế.", ChangedAt = DateTime.Now.AddHours(-4) }, // 4 tiếng trước
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[0].Id, Status = "at_recycling_unit", StatusDescription = "Thùng T-001 đã đến trung tâm tái chế.", ChangedAt = DateTime.Now.AddHours(-1) }, // 1 tiếng trước
	// ============================

	// --- Product 2 (Máy giặt) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[1].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[1].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[1].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[1].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[1].Address}", ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].Actual_Time) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[1].Id, Status = "at_warehouse", StatusDescription = "Đã nhập kho Trạm Vinhomes.", ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].Actual_Time).AddHours(4) },

	// --- Product 3 (Máy tính) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[2].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[2].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[2].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[2].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[2].Address}", ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].Actual_Time) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[2].Id, Status = "at_warehouse", StatusDescription = "Đã nhập kho Trạm Vinhomes.", ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].Actual_Time).AddHours(4) },

	// --- Product 4 (Tủ lạnh) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[3].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[3].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[3].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[3].CollectionDate.ToDateTime(collectionRoutes[3].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[3].Id, Status = "collection_failed", StatusDescription = "Tài xế hủy: Rác không phù hợp loại đăng ký.", ChangedAt = collectionRoutes[3].CollectionDate.ToDateTime(collectionRoutes[3].Actual_Time) },

	// --- Product 5 (Laptop) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[4].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[4].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[4].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[4].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[4].Address}", ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].Actual_Time) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[4].Id, Status = "at_warehouse", StatusDescription = "Đã nhập kho Trạm Vinhomes.", ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].Actual_Time).AddHours(4) },

	// --- Product 6 (Điện thoại) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[5].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[5].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[5].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[5].CollectionDate.ToDateTime(collectionRoutes[5].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[5].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[0].Address}", ChangedAt = collectionRoutes[5].CollectionDate.ToDateTime(collectionRoutes[5].Actual_Time) },

	// --- Product 7 (Loa) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[6].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[6].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[6].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[6].CollectionDate.ToDateTime(collectionRoutes[6].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[6].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[1].Address}", ChangedAt = collectionRoutes[6].CollectionDate.ToDateTime(collectionRoutes[6].Actual_Time) },

	// --- Product 8 (Bình nước nóng) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[7].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[7].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[7].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[7].CollectionDate.ToDateTime(collectionRoutes[7].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[7].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[2].Address}", ChangedAt = collectionRoutes[7].CollectionDate.ToDateTime(collectionRoutes[7].Actual_Time) },

	// --- Product 9 (Máy in) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[8].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[8].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[8].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[8].CollectionDate.ToDateTime(collectionRoutes[8].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[8].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[3].Address}", ChangedAt = collectionRoutes[8].CollectionDate.ToDateTime(collectionRoutes[8].Actual_Time) },

	// --- Product 10 (Quạt điện) ---
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[9].Id, Status = "created", StatusDescription = "Người dùng đã tạo yêu cầu.", ChangedAt = posts[9].Date },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[9].Id, Status = "scheduled", StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.", ChangedAt = collectionRoutes[9].CollectionDate.ToDateTime(collectionRoutes[9].EstimatedTime) },
	new ProductStatusHistory { ProductStatusHistoryId = Guid.NewGuid(), ProductId = products[9].Id, Status = "collected", StatusDescription = $"Lấy hàng thành công tại: {users[4].Address}", ChangedAt = collectionRoutes[9].CollectionDate.ToDateTime(collectionRoutes[9].Actual_Time) },
};
	}
}