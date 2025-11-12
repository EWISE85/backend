using ElecWasteCollection.Application.Model; // Giả sử bạn vẫn dùng namespace này
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
		// === HELPER IDs (PHASE 1) ===
		private static readonly Guid size_TiviVua = Guid.Parse("235406ca-2b0f-4bd7-94a6-c9bc23096a43");
		private static readonly Guid prod_TiviMoi = Guid.Parse("b1111111-1111-1111-1111-000000000013");
		private static readonly Guid post_TiviMoi = Guid.Parse("a0000000-0000-0000-0000-000000000004");

		// Category IDs
		private static readonly Guid parent1_Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
		private static readonly Guid parent2_Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
		private static readonly Guid parent3_Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
		private static readonly Guid parent4_Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
		private static readonly Guid parent5_Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
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

		// Attribute IDs
		private static readonly Guid att_KichThuocManHinh = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000001");
		private static readonly Guid att_ChieuDai = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000001");
		private static readonly Guid att_ChieuRong = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002");
		private static readonly Guid att_ChieuCao = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003");
		private static readonly Guid att_DungTich = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000001");
		private static readonly Guid att_KhoiLuongGiat = Guid.Parse("a1a1a1a1-0004-0004-0004-000000000001");
		private static readonly Guid att_TrongLuong = Guid.Parse("a1a1a1a1-0009-0009-0009-000000000001");

		// SizeTier IDs
		private static readonly Guid st_Tivi_TrungBinh = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000002");
		private static readonly Guid st_TuLanh_Lon = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003");
		private static readonly Guid st_MayGiat_TrungBinh = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000002");
		private static readonly Guid st_Laptop_MongNhe = Guid.Parse("a1a1a1a1-0006-0006-0006-000000000001");

		// === HELPER IDs (PHASE 2 - Brands & Packages) ===
		// Brands (Dùng cho List<Products>)
		private static readonly Guid brand_Samsung_Tivi = Guid.Parse("b0100001-0001-0001-0001-000000000001");
		private static readonly Guid brand_Toshiba_MayGiat = Guid.Parse("b0100001-0002-0002-0002-000000000001");
		private static readonly Guid brand_Dell_PC = Guid.Parse("b0100001-0003-0003-0003-000000000001");
		private static readonly Guid brand_Pana_TuLanh = Guid.Parse("b0100001-0004-0004-0004-000000000001");
		private static readonly Guid brand_Acer_Laptop = Guid.Parse("b0100001-0005-0005-0005-000000000001");
		private static readonly Guid brand_Apple_DienThoai = Guid.Parse("b0100001-0006-0006-0006-000000000001");
		private static readonly Guid brand_JBL_Loa = Guid.Parse("b0100001-0007-0007-0007-000000000001");
		private static readonly Guid brand_Ariston_Binh = Guid.Parse("b0100001-0008-0008-0008-000000000001");
		private static readonly Guid brand_HP_MayIn = Guid.Parse("b0100001-0009-0009-0009-000000000001");
		private static readonly Guid brand_Asia_Quat = Guid.Parse("b0100001-0010-0010-0010-000000000001");
		private static readonly Guid brand_Sharp_LoViSong = Guid.Parse("b0100001-0011-0011-0011-000000000001");

		// Packages (Dùng cho List<Products>)
		private static readonly string pkg_T001 = "T-001-VHP"; // Thùng T-001 tại Vinhomes

		// === HELPER IDs (PHASE 3 - Groups & Collectors) ===
		// (CẬP NHẬT: Đã thêm group_Today_Dung)
		private static readonly int group_Yesterday = 1;
		private static readonly int group_Today_Tuan = 2; // Sửa tên: Gán cho Collector Tuấn (Shift 2)
		private static readonly int group_Tomorrow = 3;
		private static readonly int group_Today_Dung = 4; // MỚI: Gán cho Collector Dũng (Shift 1)


		// (CẬP NHẬT) IDs cho Collectors
		private static readonly Guid collector_Dung_Id = Guid.Parse("6df4af85-6a59-4a0a-8513-1d7859fbd789");
		private static readonly Guid collector_Tuan_Id = Guid.Parse("c011ec70-b861-468f-b648-812e90f01a7e");


		// === 1. USERS ===
		public static List<User> users = new()
		{
			// (Giữ nguyên data Users... đã thêm Role và SmallCollectionPointId)
			new User
			{
				UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),
				Name = "Trần Huỳnh Bảo Ngọc",
				Email = "ngocthbse183850@fpt.edu.vn",
				Phone = "0901234567",
				Address = "Vinhomes Grand Park – Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Avatar = "https://picsum.photos/id/1011/200/200",
				Iat = 10.842003,
				Ing = 106.829580,
				Role = "User"
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
				Ing = 106.829900,
				Role = "User"
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
				Ing = 106.833400,
				Role = "User"
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
				Ing = 106.830000,
				Role = "User"
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
				Ing = 106.833800,
				Role = "User"
			},
			new User
			{
				UserId = Guid.Parse("c20deff9-163b-49e8-b967-238f22882b65"),
				Name = "Admin thu gom nhỏ",
				Email = "adminthugomnho@gmail.com",
				Phone = "0977222333",
				Address = "Công viên Ánh Sáng Vinhomes – Khu đô thị Vinhomes Grand Park",
				Avatar = "https://picsum.photos/id/1059/200/200",
				Iat = 10.839000,
				Ing = 106.833800,
				Role = "Admin_SmallCollectionPoint",
				SmallCollectionPointId = 1 // Đã thêm
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

		// === 3. CATEGORIES ===
		public static List<Category> categories = new()
		{
			// (Giữ nguyên data Categories)
			new Category { Id = parent1_Id, Name = "Đồ gia dụng lớn", ParentCategoryId = null },
			new Category { Id = cat_TuLanh, Name = "Tủ lạnh", ParentCategoryId = parent1_Id },
			new Category { Id = cat_MayGiat, Name = "Máy giặt", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000003"), Name = "Máy sấy quần áo", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000004"), Name = "Máy rửa bát", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000005"), Name = "Máy điều hòa", ParentCategoryId = parent1_Id },
			new Category { Id = cat_BinhNuocNong, Name = "Bình nước nóng", ParentCategoryId = parent1_Id },
			new Category { Id = Guid.Parse("11111111-1111-1111-1111-000000000007"), Name = "Lò nướng", ParentCategoryId = parent1_Id },
			new Category { Id = cat_LoViSong, Name = "Lò vi sóng", ParentCategoryId = parent1_Id },
			new Category { Id = parent2_Id, Name = "Đồ điện tử Tiêu dùng & Giải trí", ParentCategoryId = null },
			new Category { Id = cat_Tivi, Name = "Tivi", ParentCategoryId = parent2_Id },
			new Category { Id = cat_ManHinhMayTinh, Name = "Màn hình máy tính", ParentCategoryId = parent2_Id },
			new Category { Id = cat_Loa, Name = "Dàn âm thanh (Loa, Amply)", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000004"), Name = "Máy chơi game (Console)", ParentCategoryId = parent2_Id },
			new Category { Id = Guid.Parse("22222222-2222-2222-2222-000000000005"), Name = "Đầu đĩa (DVD, VCD, Blu-ray)", ParentCategoryId = parent2_Id },
			new Category { Id = parent3_Id, Name = "Thiết bị IT và Viễn thông", ParentCategoryId = null },
			new Category { Id = cat_MayTinhDeBan, Name = "Máy tính để bàn (PC)", ParentCategoryId = parent3_Id },
			new Category { Id = cat_Laptop, Name = "Laptop (Máy tính xách tay)", ParentCategoryId = parent3_Id },
			new Category { Id = cat_DienThoai, Name = "Điện thoại di động", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000004"), Name = "Máy tính bảng (Tablet)", ParentCategoryId = parent3_Id },
			new Category { Id = cat_MayIn, Name = "Máy in", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000006"), Name = "Máy scan", ParentCategoryId = parent3_Id },
			new Category { Id = Guid.Parse("33333333-3333-3333-3333-000000000007"), Name = "Thiết bị mạng (Router, Modem)", ParentCategoryId = parent3_Id },
			new Category { Id = parent4_Id, Name = "Đồ gia dụng nhỏ", ParentCategoryId = null },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000001"), Name = "Nồi cơm điện", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000002"), Name = "Ấm đun nước", ParentCategoryId = parent4_Id },
			new Category { Id = Guid.Parse("44444444-4444-4444-4444-000000000003"), Name = "Máy xay sinh tố", ParentCategoryId = parent4_Id },
			new Category { Id = cat_QuatDien, Name = "Quạt điện", ParentCategoryId = parent4_Id },
			new Category { Id = cat_MayHutBui, Name = "Máy hút bụi", ParentCategoryId = parent4_Id },
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

		// === 4. ATTRIBUTES ===
		public static List<Attributes> attributes = new()
		{
			// (Giữ nguyên data Attributes)
			new Attributes { Id = att_KichThuocManHinh, Name = "Kích thước màn hình (inch)", },
			new Attributes { Id = att_ChieuDai, Name = "Chiều dài (cm)", },
			new Attributes { Id = att_ChieuRong, Name = "Chiều rộng (cm)", },
			new Attributes { Id = att_ChieuCao, Name = "Chiều cao (cm)", },
			new Attributes { Id = att_DungTich, Name = "Dung tích (lít)", },
			new Attributes { Id = att_KhoiLuongGiat, Name = "Khối lượng giặt (kg)", },
			new Attributes { Id = att_TrongLuong, Name = "Trọng lượng (kg)", }
		};

		// === 5. CATEGORY_ATTRIBUTES (Bảng "Luật") ===
		public static List<CategoryAttributes> categoryAttributes = new()
		{
			// (Giữ nguyên data CategoryAttributes)
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
		public static List<SizeTier> sizeTiers = new()
		{
			// (Giữ nguyên data SizeTiers)
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

		// === 7. BRANDS ===
		public static List<Brand> brands = new()
		{
			// (Giữ nguyên data Brands)
			new Brand { BrandId = brand_Samsung_Tivi, CategoryId = cat_Tivi, Name = "Samsung" },
			new Brand { BrandId = brand_Toshiba_MayGiat, CategoryId = cat_MayGiat, Name = "Toshiba" },
			new Brand { BrandId = brand_Dell_PC, CategoryId = cat_MayTinhDeBan, Name = "Dell" },
			new Brand { BrandId = brand_Pana_TuLanh, CategoryId = cat_TuLanh, Name = "Panasonic" },
			new Brand { BrandId = brand_Acer_Laptop, CategoryId = cat_Laptop, Name = "Acer" },
			new Brand { BrandId = brand_Apple_DienThoai, CategoryId = cat_DienThoai, Name = "Apple" },
			new Brand { BrandId = brand_JBL_Loa, CategoryId = cat_Loa, Name = "JBL" },
			new Brand { BrandId = brand_Ariston_Binh, CategoryId = cat_BinhNuocNong, Name = "Ariston" },
			new Brand { BrandId = brand_HP_MayIn, CategoryId = cat_MayIn, Name = "HP" },
			new Brand { BrandId = brand_Asia_Quat, CategoryId = cat_QuatDien, Name = "Asia" },
			new Brand { BrandId = brand_Sharp_LoViSong, CategoryId = cat_LoViSong, Name = "Sharp" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Tivi, Name = "LG" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Tivi, Name = "Sony" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Tivi, Name = "TCL" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Tivi, Name = "Panasonic" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Dell" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "LG" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Samsung" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Asus" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "Samsung" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "LG" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "Hitachi" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_TuLanh, Name = "Toshiba" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayGiat, Name = "LG" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayGiat, Name = "Samsung" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayGiat, Name = "Electrolux" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Dell" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "HP" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Lenovo" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Asus" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Apple" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "MSI" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_DienThoai, Name = "Samsung" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_DienThoai, Name = "Oppo" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_DienThoai, Name = "Xiaomi" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayIn, Name = "Canon" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayIn, Name = "Brother" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayIn, Name = "Epson" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Loa, Name = "Sony" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Loa, Name = "Bose" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_Loa, Name = "Marshall" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayHutBui, Name = "Dyson" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayHutBui, Name = "Philips" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_MayHutBui, Name = "Xiaomi" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_QuatDien, Name = "Senko" },
			new Brand { BrandId = Guid.NewGuid(), CategoryId = cat_QuatDien, Name = "Panasonic" },
		};

		// === 8. PACKAGES ===
		public static List<Packages> packages = new()
		{
			// (Giữ nguyên data Packages)
			new Packages
			{
				PackageId = pkg_T001,
				PackageName = "Thùng TV/Màn hình 01 (Vinhomes)",
				CreateAt = DateTime.Now.AddHours(-8),
				SmallCollectionPointsId = 1,
				Status = "in_transit"
			}
		};

		// === 9. PRODUCTS ===
		public static List<Products> products = new List<Products>
		{
			// (Giữ nguyên data Products)
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000001"),CategoryId = cat_Tivi,SizeTierId = st_Tivi_TrungBinh,BrandId = brand_Samsung_Tivi,PackageId = pkg_T001,Description = "Tivi Samsung 42 inch hỏng màn hình.",Status = "Đã đóng gói"},
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000002"),CategoryId = cat_MayGiat,SizeTierId = st_MayGiat_TrungBinh,BrandId = brand_Toshiba_MayGiat,PackageId = null,Description = "Máy giặt Toshiba không hoạt động nữa.",Status = "Đã nhập kho"},
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000003"),CategoryId = cat_MayTinhDeBan,SizeTierId = null,BrandId = brand_Dell_PC,PackageId = null,Description = "CPU Intel i3 đời cũ, màn hình Dell 19 inch.",Status = "Đã nhập kho"},
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000004"),CategoryId = cat_TuLanh,SizeTierId = st_TuLanh_Lon,BrandId = brand_Pana_TuLanh,PackageId = null,Description = "Tủ lạnh Panasonic không còn làm lạnh.",Status = "Chờ thu gom"},
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000005"),CategoryId = cat_Laptop,SizeTierId = st_Laptop_MongNhe,BrandId = brand_Acer_Laptop,PackageId = null,Description = "Laptop Acer bị vỡ màn hình.",Status = "Đã nhập kho"},

			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000006"),CategoryId = cat_DienThoai,SizeTierId = null,BrandId = brand_Apple_DienThoai,PackageId = null,Description = "iPhone 7 bị chai pin.",Status = "Hủy bỏ"}, 

// Đơn 6: Đã xong sáng nay
new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000007"),CategoryId = cat_Loa,SizeTierId = null,BrandId = brand_JBL_Loa,PackageId = null,Description = "Loa JBL mini không sạc được.",Status = "Đã thu gom"},

// Đơn 7: Chưa làm (Đang chờ) -> SỬA STATUS
new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000008"),CategoryId = cat_BinhNuocNong,SizeTierId = null,BrandId = brand_Ariston_Binh,PackageId = null,Description = "Bình Ariston bị rò điện.",Status = "Chờ thu gom"}, // <--- SỬA

// Đơn 8: Chưa làm (Đang chờ) -> SỬA STATUS
new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000009"),CategoryId = cat_MayIn,SizeTierId = null,BrandId = brand_HP_MayIn,PackageId = null,Description = "Máy in HP cũ, không còn dùng.",Status = "Chờ thu gom"}, // <--- SỬA

// Đơn 9: Chưa làm (Đang chờ) -> SỬA STATUS
new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000010"),CategoryId = cat_QuatDien,SizeTierId = null,BrandId = brand_Asia_Quat,PackageId = null,Description = "Quạt Asia cũ, gãy cánh.",Status = "Chờ thu gom"}, // <--- SỬA

			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000010"),CategoryId = cat_QuatDien,SizeTierId = null,BrandId = brand_Asia_Quat,PackageId = null,Description = "Quạt Asia cũ, gãy cánh.",Status = "Đã thu gom"},
			new Products{Id = Guid.Parse("b1111111-1111-1111-1111-000000000012"),CategoryId = cat_LoViSong,SizeTierId = Guid.Parse("f3c8c4ef-56f3-433e-b210-3f900248ffae"),BrandId = brand_Sharp_LoViSong,PackageId = null,Description = "bị hư",Status = "Chờ Duyệt"},
			new Products{Id = prod_TiviMoi,CategoryId = cat_Tivi,SizeTierId = size_TiviVua,BrandId = brand_Samsung_Tivi,PackageId = null,Description = "bị hư",Status = "Chờ Duyệt"}
		};

		// === 10. PRODUCT_VALUES ===
		public static List<ProductValues> productValues = new List<ProductValues>
		{
			// (Giữ nguyên data ProductValues)
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[2].Id, AttributeId = att_KichThuocManHinh, Value = 19 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[2].Id, AttributeId = att_TrongLuong, Value = 7 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[7].Id, AttributeId = att_DungTich, Value = 30 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[7].Id, AttributeId = att_TrongLuong, Value = 15 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_TrongLuong, Value = 8 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuDai, Value = 40 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuRong, Value = 35 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = products[8].Id, AttributeId = att_ChieuCao, Value = 20 },
		};

		// === 11. POSTS ===
		public static List<Post> posts = new()
		{
			// (Giữ nguyên data Posts - Đã thêm EstimatePoint)
			new Post{Id = Guid.Parse("a2d7b801-b0fb-4f7d-9b83-b741d23666a1"),SenderId = users[0].UserId,ProductId = products[0].Id,Name = "Thu gom tivi cũ",Description = "Vui lòng đến vào buổi chiều.",Date = DateTime.Now.AddDays(-2),Address = users[0].Address,ScheduleJson = CreateSchedule(0, "08:00", "09:00"),Status = "Đã Duyệt", EstimatePoint = 100},
			new Post{Id = Guid.Parse("b34c1223-7545-41d2-9e42-67d75e3c2a31"),SenderId = users[1].UserId,ProductId = products[1].Id,Name = "Máy giặt hỏng cần thu gom",Description = "Máy giặt Toshiba, lấy cẩn thận giúp.",Date = DateTime.Now.AddDays(-5),Address = users[1].Address,ScheduleJson = CreateSchedule(0, "09:00", "10:00"),Status = "Đã Duyệt", EstimatePoint = 150},
			new Post{Id = Guid.Parse("c1b63fa1-ec52-44a0-8a9c-8b83f8d1b8c3"),SenderId = users[2].UserId,ProductId = products[2].Id,Name = "Máy tính cũ không dùng nữa",Description = "Gồm 1 case và 1 màn hình.",Date = DateTime.Now.AddDays(-2),Address = users[2].Address,ScheduleJson = CreateSchedule(0, "10:00", "11:00"),Status = "Đã Duyệt", EstimatePoint = 200},
			new Post{Id = Guid.Parse("d9a86de5-7d27-43d0-9f55-49094b30947d"),SenderId = users[3].UserId,ProductId = products[3].Id,Name = "Tủ lạnh hỏng cần xử lý",Description = "Tủ lạnh to, cần 2 người khiêng.",Date = DateTime.Now.AddDays(-8),Address = users[3].Address,ScheduleJson = CreateSchedule(0, "11:00", "12:00"),Status = "Đã Từ Chối",RejectMessage = "Hình ảnh không rõ ràng.", EstimatePoint = 0},
			new Post{Id = Guid.Parse("e0f92a77-188b-402b-a0ea-3b1c68891ac0"),SenderId = users[4].UserId,ProductId = products[4].Id,Name = "Laptop bị vỡ màn hình",Description = "Chỉ thu gom laptop, không kèm sạc.",Date = DateTime.Now.AddDays(-1),Address = users[4].Address,ScheduleJson = CreateSchedule(0, "13:00", "14:00"),Status = "Đã Duyệt", EstimatePoint = 100},
			new Post{Id = Guid.Parse("f2c3cc25-f7d7-4b0a-bd1c-69a2dfb6b211"),SenderId = users[0].UserId,ProductId = products[5].Id,Name = "Điện thoại cũ bị chai pin",Description = "iPhone 7.",Date = DateTime.Now.AddDays(-4),Address = users[0].Address,ScheduleJson = CreateSchedule(1, "14:00", "15:00"),Status = "Đã Duyệt", EstimatePoint = 200},
			new Post{Id = Guid.Parse("a82d6f7b-f1e7-45dc-83ec-7b3e2db21a4f"),SenderId = users[1].UserId,ProductId = products[6].Id,Name = "Loa Bluetooth bị hỏng",Description = "Loa JBL.",Date = DateTime.Now.AddDays(-6),Address = users[1].Address,ScheduleJson = CreateSchedule(1, "15:00", "16:00"),Status = "Đã Duyệt", EstimatePoint = 120},
			new Post{Id = Guid.Parse("b0b8c58b-4921-4e7d-9b09-0840f994e98e"),SenderId = users[2].UserId,ProductId = products[7].Id,Name = "Bình nước nóng hỏng",Description = "Bình Ariston, vẫn còn trên tường, cần gỡ.",Date = DateTime.Now.AddDays(-9),Address = users[2].Address,ScheduleJson = CreateSchedule(1, "16:00", "17:00"),Status = "Đã Duyệt", EstimatePoint = 100},
			new Post{Id = Guid.Parse("c9955eab-20a8-463f-b6db-4d20382195c3"),SenderId = users[3].UserId,ProductId = products[8].Id,Name = "Máy in văn phòng cũ",Description = "Máy in HP.",Date = DateTime.Now.AddDays(-7),Address = users[3].Address,ScheduleJson = CreateSchedule(1, "17:00", "18:00"),Status = "Đã Duyệt", EstimatePoint = 100},
			new Post{Id = Guid.Parse("e62aefc7-0e61-4b35-9d59-6b8e10d2b01e"),SenderId = users[4].UserId,ProductId = products[9].Id,Name = "Quạt điện hỏng cánh",Description = "Quạt Asia.",Date = DateTime.Now.AddDays(-10),Address = users[4].Address,ScheduleJson = CreateSchedule(1, "18:00", "19:00"),Status = "Đã Duyệt", EstimatePoint = 100},
			new Post{Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"),SenderId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),ProductId = products[10].Id,Name = "lò vi sóng ko xài được nữa",Description = "",Date = DateTime.Now.AddDays(-3),Address = "string",ScheduleJson = "[{\"dayName\":\"T6\",\"pickUpDate\":\"2025-11-02\",\"slots\":{\"startTime\":\"09:00\",\"endTime\":\"10:00\"}}]",Status = "Chờ Duyệt",RejectMessage = null,CheckMessage = new List<string>(), EstimatePoint = 100},
			new Post{Id = post_TiviMoi,SenderId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),ProductId = prod_TiviMoi,Name = "tivi ko xài được nữa",Description = "",Date = DateTime.Now.AddDays(-1),Address = "string",ScheduleJson = "[{\"dayName\":\"T6\",\"pickUpDate\":\"2025-11-02\",\"slots\":{\"startTime\":\"09:00\",\"endTime\":\"10:00\"}}]",Status = "Chờ Duyệt",RejectMessage = null,CheckMessage = new List<string>(), EstimatePoint = 100}
		};

		// === 12. POST IMAGES ===
		public static List<PostImages> postImages = new()
		{
			// (Giữ nguyên data PostImages)
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[0].Id,ImageUrl = "https://tse4.mm.bing.net/th/id/OIP.LuRXEsdA9472ZA06zqLEswHaHa?pid=Api&P=0&h=180",AiDetectedLabelsJson = "[{\"Tag\":\"television\",\"Confidence\":98.5},{\"Tag\":\"screen\",\"Confidence\":92.1},{\"Tag\":\"electronics\",\"Confidence\":85.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[1].Id,ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.nqDpXYFDMJ4J3SHRuHJfCAHaF7?pid=Api&P=0&h=180",AiDetectedLabelsJson = "[{\"Tag\":\"washing machine\",\"Confidence\":99.2},{\"Tag\":\"home appliance\",\"Confidence\":95.0},{\"Tag\":\"laundry\",\"Confidence\":70.1}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[2].Id,ImageUrl = "https://mccvietnam.vn/media/lib/26-09-2022/b-pc-mcc-1920x1080.png",AiDetectedLabelsJson = "[{\"Tag\":\"computer\",\"Confidence\":97.0},{\"Tag\":\"monitor\",\"Confidence\":90.3},{\"Tag\":\"desktop computer\",\"Confidence\":88.0},{\"Tag\":\"electronics\",\"Confidence\":85.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[3].Id,ImageUrl = "https://picsum.photos/id/203/400/300",AiDetectedLabelsJson = "[{\"Tag\":\"blurry\",\"Confidence\":70.0},{\"Tag\":\"dark\",\"Confidence\":65.0},{\"Tag\":\"unclear\",\"Confidence\":50.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[4].Id,ImageUrl = "https://vinhphatstore.vn/wp-content/uploads/2022/09/cach-sua-man-hinh-laptop-bi-vo-hieu-qua-triet-de-3-1.jpg",AiDetectedLabelsJson = "[{\"Tag\":\"laptop\",\"Confidence\":99.0},{\"Tag\":\"computer\",\"Confidence\":95.0},{\"Tag\":\"broken screen\",\"Confidence\":92.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[5].Id,ImageUrl = "https://cdn.nguyenkimmall.com/images/product/829/dien-thoai-iphone-14-pro-max-1tb-tim-1.jpg",AiDetectedLabelsJson = "[{\"Tag\":\"smartphone\",\"Confidence\":99.8},{\"Tag\":\"iphone\",\"Confidence\":95.0},{\"Tag\":\"mobile phone\",\"Confidence\":90.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[6].Id,ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.h0WESAKXTusQdzs5QSsLVAHaHa?pid=Api&P=0&h=180",AiDetectedLabelsJson = "[{\"Tag\":\"speaker\",\"Confidence\":96.0},{\"Tag\":\"bluetooth\",\"Confidence\":80.0},{\"Tag\":\"audio\",\"Confidence\":75.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[7].Id,ImageUrl = "https://media.eproshop.vn/file/Ggw3EQpfr",AiDetectedLabelsJson = "[{\"Tag\":\"water heater\",\"Confidence\":94.0},{\"Tag\":\"boiler\",\"Confidence\":85.0},{\"Tag\":\"home appliance\",\"Confidence\":80.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[8].Id,ImageUrl = "https://cdn.tgdd.vn/Files/2019/01/24/1146335/may-in-da-nang-la-gi.jpg",AiDetectedLabelsJson = "[{\"Tag\":\"printer\",\"Confidence\":99.0},{\"Tag\":\"office equipment\",\"Confidence\":90.0},{\"Tag\":\"copier\",\"Confidence\":85.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[9].Id,ImageUrl = "https://meta.vn/Data/image/2020/07/01/quat-dung-dien-co-91-qd-cn450p5.jpg",AiDetectedLabelsJson = "[{\"Tag\":\"fan\",\"Confidence\":98.0},{\"Tag\":\"electric fan\",\"Confidence\":92.0}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = posts[10].Id,ImageUrl = "https://cdn.nguyenkimmall.com/images/detailed/616/10042970-lo-vi-song-sharp-23l-r-31a2vn-s-01.jpg",AiDetectedLabelsJson = "[{\"tag\":\"equipment\",\"confidence\":50.56,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"technology\",\"confidence\":49.04,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"microwave\",\"confidence\":45.45,\"status\":\"Phù hợp với danhK mục\"},{\"tag\":\"screen\",\"confidence\":40.33,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"kitchen appliance\",\"confidence\":34.71,\"status\":\"Phù hợp với danh mục\"}]"},
			new PostImages{PostImageId = Guid.NewGuid(),PostId = post_TiviMoi,ImageUrl = "https://img.lovepik.com/free-png/20220125/lovepik-tv-monitor-png-image_401728080_wh1200.png",AiDetectedLabelsJson = "[{\"tag\":\"monitor\",\"confidence\":51.47,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"screen\",\"confidence\":39.17,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"television\",\"confidence\":36.41,\"status\":\"Phù hợp với danh mục\"},{\"tag\":\"display\",\"confidence\":34.05,\"status\":\"Không phù hợp với danh mục\"},{\"tag\":\"computer\",\"confidence\":31.71,\"status\":\"Không phù hợp với danh mục\"}]"}
		};

		// === 13. COLLECTORS ===
		public static List<Collector> collectors = new()
		{
			// (Giữ nguyên data Collectors)
			new Collector
			{
				CollectorId = collector_Dung_Id,
				Name = "Ngô Văn Dũng",
				Email = "ngo.van.dung@ewc.vn",
				Phone = "0905999888",
				Avatar = "https://picsum.photos/id/1062/200/200",
				SmallColltionId = 1 // Làm việc tại Trạm Vinhomes
			},
			new Collector
			{
				CollectorId = collector_Tuan_Id,
				Name = "Lê Minh Tuấn",
				Email = "le.minh.tuan@ewc.vn",
				Phone = "0905111222",
				Avatar = "https://picsum.photos/id/1063/200/200",
				SmallColltionId = 1 // Cùng làm việc tại Trạm Vinhomes
			}
		};

		// === 14. SMALL COLLECTION POINTS ===
		public static List<SmallCollectionPoints> smallCollectionPoints = new()
		{
			// (Giữ nguyên data SmallCollectionPoints)
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

		// === 15. COLLECTION TEAMS ===
		public static List<CollectionTeams> collectionTeams = new()
		{
			// (Giữ nguyên data CollectionTeams)
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

		// === 16. VEHICLES ===
		public static List<Vehicles> vehicles = new()
		{
			// (Giữ nguyên data Vehicles)
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

		// === 17. SHIFTS ===
		public static List<Shifts> shifts = new()
		{
			// (Giữ nguyên data Shifts - Cả 2 collector đều có ca hôm nay)
			// --- Ca hôm nay ---
			new Shifts
			{
				Id = 1,
				CollectorId = collector_Dung_Id, // Sửa: Dùng Guid của Collector
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today),
				Shift_Start_Time = DateTime.Today.AddHours(7),
				Shift_End_Time = DateTime.Today.AddHours(15)
			},
			new Shifts
			{
				Id = 2,
				CollectorId = collector_Tuan_Id, // Sửa: Dùng Guid của Collector
				Vehicle_Id = 2,
				WorkDate = DateOnly.FromDateTime(DateTime.Today),
				Shift_Start_Time = DateTime.Today.AddHours(8),
				Shift_End_Time = DateTime.Today.AddHours(16)
			},

			// --- Ca ngày mai ---
			new Shifts
			{
				Id = 3,
				CollectorId = collector_Dung_Id, // Sửa: Dùng Guid của Collector
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
				Shift_Start_Time = DateTime.Today.AddDays(1).AddHours(7),
				Shift_End_Time = DateTime.Today.AddDays(1).AddHours(15)
			},
			new Shifts
			{
				Id = 4,
				CollectorId = collector_Tuan_Id, // Sửa: Dùng Guid của Collector
				Vehicle_Id = 2,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
				Shift_Start_Time = DateTime.Today.AddDays(1).AddHours(8),
				Shift_End_Time = DateTime.Today.AddDays(1).AddHours(16)
			},

			// --- Ca ngày kia ---
			new Shifts
			{
				Id = 5,
				CollectorId = collector_Dung_Id, // Sửa: Dùng Guid của Collector
				Vehicle_Id = 1,
				WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
				Shift_Start_Time = DateTime.Today.AddDays(2).AddHours(7),
				Shift_End_Time = DateTime.Today.AddDays(2).AddHours(15)
			},
		};

		// === 18. COLLECTION GROUPS (ĐÃ CẬP NHẬT) ===
		public static List<CollectionGroups> collectionGroups = new()
		{
			new CollectionGroups
			{
				Id = group_Yesterday,
				Group_Code = "D1-S1-VHP",
				Name = "Tuyến hôm qua (Ca 1)",
				Shift_Id = 1, // (Giả sử Shift 1 là của Dũng hôm qua)
				Created_At = DateTime.Now.AddDays(-1)
			},
			new CollectionGroups
			{
				Id = group_Today_Tuan, // Sửa tên
				Group_Code = "D2-S2-VHP",
				Name = "Tuyến hôm nay (Ca 2 - Tuấn)", // Sửa tên
				Shift_Id = 2, // Liên kết với Shift #2 (của Lê Minh Tuấn)
				Created_At = DateTime.Now
			},
			new CollectionGroups
			{
				Id = group_Tomorrow,
				Group_Code = "D3-S1-VHP",
				Name = "Tuyến ngày mai (Ca 1)",
				Shift_Id = 3, // Liên kết với Shift #3 (của Ngô Văn Dũng)
				Created_At = DateTime.Now
			},
			// (CẬP NHẬT MỚI)
			new CollectionGroups
			{
				Id = group_Today_Dung, // MỚI
				Group_Code = "D2-S1-VHP",
				Name = "Tuyến hôm nay (Ca 1 - Dũng)", // MỚI
				Shift_Id = 1, // Liên kết với Shift #1 (của Ngô Văn Dũng)
				Created_At = DateTime.Now
			}
		};


		// === 19. COLLECTION ROUTES (ĐÃ CẬP NHẬT) ===
		public static List<CollectionRoutes> collectionRoutes = new()
		{
			// === 5 ngày hôm qua (Đã hoàn thành/hủy) ===
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d1f2cde2-0e2a-4a8e-b5a0-60d34e8d3b90"),
				PostId = posts[0].Id,
				CollectionGroupId = group_Yesterday,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(8, 0),
				Actual_Time = new TimeOnly(8, 20),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/301/400/300" },
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d2f2cde2-0e2a-4a8e-b5a0-60d34e8d3b91"),
				PostId = posts[1].Id,
				CollectionGroupId = group_Yesterday,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(9, 0),
				Actual_Time = new TimeOnly(9, 15),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/302/400/300" },
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d3f2cde2-0e2a-4a8e-b5a0-60d34e8d3b92"),
				PostId = posts[2].Id,
				CollectionGroupId = group_Yesterday,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(10, 0),
				Actual_Time = new TimeOnly(10, 10),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/303/400/300" },
				Status = "Hoàn thành"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d4f2cde2-0e2a-4a8e-b5a0-60d34e8d3b93"),
				PostId = posts[3].Id,
				CollectionGroupId = group_Yesterday,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(11, 0),
				Actual_Time = new TimeOnly(11, 30),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/304/400/300" },
				Status = "Hủy bỏ",
				RejectMessage = "Rác không phù hợp loại đăng ký."
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("d5f2cde2-0e2a-4a8e-b5a0-60d34e8d3b94"),
				PostId = posts[4].Id,
				CollectionGroupId = group_Yesterday,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
				EstimatedTime = new TimeOnly(13, 0),
				Actual_Time = new TimeOnly(13, 15),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/305/400/300" },
				Status = "Hoàn thành"
			},

			// === 5 ngày hôm nay (Đang tiến hành) - (CẬP NHẬT) ===
			new CollectionRoutes
{
	CollectionRouteId = Guid.Parse("e1f2cde2-0e2a-4a8e-b5a0-60d34e8d3b95"),
	PostId = posts[5].Id,
	CollectionGroupId = group_Today_Tuan,
	CollectionDate = DateOnly.FromDateTime(DateTime.Now),
	EstimatedTime = new TimeOnly(8, 30), // Sáng sớm
    Actual_Time = new TimeOnly(8, 45),   // <--- ĐÃ CÓ GIỜ THỰC TẾ
    ConfirmImages = new List<string>{ "https://picsum.photos/id/401/400/300" }, // <--- CÓ ẢNH
    Status = "Hoàn thành" // <--- SỬA THÀNH HOÀN THÀNH
},

// Route 6: Đã xong (Khớp với Product 6 "Đã thu gom")
new CollectionRoutes
{
	CollectionRouteId = Guid.Parse("e2f2cde2-0e2a-4a8e-b5a0-60d34e8d3b96"),
	PostId = posts[6].Id,
	CollectionGroupId = group_Today_Tuan,
	CollectionDate = DateOnly.FromDateTime(DateTime.Now),
	EstimatedTime = new TimeOnly(9, 0),
	Actual_Time = new TimeOnly(9, 10),   // <--- ĐÃ CÓ GIỜ THỰC TẾ
    ConfirmImages = new List<string>{ "https://picsum.photos/id/402/400/300" }, // <--- CÓ ẢNH
	RejectMessage = "Không có hàng",
	Status = "Hủy bỏ" // <--- SỬA THÀNH HOÀN THÀNH
},

// Route 7: Đang chạy (Khớp với Product 7 "Chờ thu gom")
new CollectionRoutes
{
	CollectionRouteId = Guid.Parse("e3f2cde2-0e2a-4a8e-b5a0-60d34e8d3b97"),
	PostId = posts[7].Id,
	CollectionGroupId = group_Today_Tuan,
	CollectionDate = DateOnly.FromDateTime(DateTime.Now),
	EstimatedTime = new TimeOnly(14, 0), // Chiều nay
    Actual_Time = null, // <--- CHƯA XONG
    ConfirmImages = new List<string>(),
	Status = "Đang tiến hành" // <--- TÀI XẾ ĐANG ĐẾN
},

// Route 8: Đang chờ (Khớp với Product 8 "Chờ thu gom")
new CollectionRoutes
{
	CollectionRouteId = Guid.Parse("e4f2cde2-0e2a-4a8e-b5a0-60d34e8d3b98"),
	PostId = posts[8].Id,
	CollectionGroupId = group_Today_Dung,
	CollectionDate = DateOnly.FromDateTime(DateTime.Now),
	EstimatedTime = new TimeOnly(15, 0),
	Actual_Time = null,
	ConfirmImages = new List<string>(),
	Status = "Đang tiến hành"
},

// Route 9: Đang chờ (Khớp với Product 9 "Chờ thu gom")
new CollectionRoutes
{
	CollectionRouteId = Guid.Parse("e5f2cde2-0e2a-4a8e-b5a0-60d34e8d3b99"),
	PostId = posts[9].Id,
	CollectionGroupId = group_Today_Dung,
	CollectionDate = DateOnly.FromDateTime(DateTime.Now),
	EstimatedTime = new TimeOnly(16, 0),
	Actual_Time = null,
	ConfirmImages = new List<string>(),
	Status = "Đang tiến hành"
},

			// === 5 ngày mai (Chưa bắt đầu) ===
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f1f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba0"),
				PostId = posts[0].Id, // Tái sử dụng post 1
				CollectionGroupId = group_Tomorrow,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(8, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/311/400/300" },
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f2f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba1"),
				PostId = posts[1].Id, // Tái sử dụng post 2
				CollectionGroupId = group_Tomorrow,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(9, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/312/400/300" },
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f3f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba2"),
				PostId = posts[2].Id, // Tái sử dụng post 3
				CollectionGroupId = group_Tomorrow,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(10, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/313/400/300" },
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f4f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba3"),
				PostId = posts[3].Id, // Tái sử dụng post 4
				CollectionGroupId = group_Tomorrow,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(11, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/314/400/300" },
				Status = "Chưa bắt đầu"
			},
			new CollectionRoutes
			{
				CollectionRouteId = Guid.Parse("f5f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba4"),
				PostId = posts[4].Id, // Tái sử dụng post 5
				CollectionGroupId = group_Tomorrow,
				CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
				EstimatedTime = new TimeOnly(13, 0),
				ConfirmImages = new List<string>{ "https://picsum.photos/id/315/400/300" },
				Status = "Chưa bắt đầu"
			}
		};

		// === 20. PRODUCT STATUS HISTORY ===
		public static List<ProductStatusHistory> productStatusHistories = new()
		{
			// (Giữ nguyên data ProductStatusHistory)
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[0].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[0].Address}",ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].Actual_Time.Value)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "at_warehouse",StatusDescription = "Đã nhập kho Trạm Vinhomes.",ChangedAt = collectionRoutes[0].CollectionDate.ToDateTime(collectionRoutes[0].Actual_Time.Value).AddHours(4)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "packaged",StatusDescription = $"Đã đóng gói vào thùng {pkg_T001}.",ChangedAt = DateTime.Now.AddHours(-8)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "in_transit",StatusDescription = $"Thùng {pkg_T001} đang được chuyển đến trung tâm tái chế.",ChangedAt = DateTime.Now.AddHours(-4)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[0].Id,Status = "at_recycling_unit",StatusDescription = $"Thùng {pkg_T001} đã đến trung tâm tái chế.",ChangedAt = DateTime.Now.AddHours(-1)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[1].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[1].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[1].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[1].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[1].Address}",ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].Actual_Time.Value)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[1].Id,Status = "at_warehouse",StatusDescription = "Đã nhập kho Trạm Vinhomes.",ChangedAt = collectionRoutes[1].CollectionDate.ToDateTime(collectionRoutes[1].Actual_Time.Value).AddHours(4)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[2].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[2].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[2].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[2].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[2].Address}",ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].Actual_Time.Value)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[2].Id,Status = "at_warehouse",StatusDescription = "Đã nhập kho Trạm Vinhomes.",ChangedAt = collectionRoutes[2].CollectionDate.ToDateTime(collectionRoutes[2].Actual_Time.Value).AddHours(4)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[3].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[3].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[3].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[3].CollectionDate.ToDateTime(collectionRoutes[3].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[3].Id,Status = "collection_failed",StatusDescription = "Tài xế hủy: Rác không phù hợp loại đăng ký.",ChangedAt = collectionRoutes[3].CollectionDate.ToDateTime(collectionRoutes[3].Actual_Time.Value)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[4].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[4].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[4].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[4].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[4].Address}",ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].Actual_Time.Value)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[4].Id,Status = "at_warehouse",StatusDescription = "Đã nhập kho Trạm Vinhomes.",ChangedAt = collectionRoutes[4].CollectionDate.ToDateTime(collectionRoutes[4].Actual_Time.Value).AddHours(4)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[5].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[5].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[5].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[5].CollectionDate.ToDateTime(collectionRoutes[5].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[5].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[0].Address}",ChangedAt = collectionRoutes[5].CollectionDate.ToDateTime(collectionRoutes[5].EstimatedTime).AddMinutes(10)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[6].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[6].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[6].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[6].CollectionDate.ToDateTime(collectionRoutes[6].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[6].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[1].Address}",ChangedAt = collectionRoutes[6].CollectionDate.ToDateTime(collectionRoutes[6].EstimatedTime).AddMinutes(5)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[7].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu cầu.",ChangedAt = posts[7].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[7].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[7].CollectionDate.ToDateTime(collectionRoutes[7].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[7].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[2].Address}",ChangedAt = collectionRoutes[7].CollectionDate.ToDateTime(collectionRoutes[7].EstimatedTime).AddMinutes(20)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[8].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêuYêu cầu.",ChangedAt = posts[8].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[8].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[8].CollectionDate.ToDateTime(collectionRoutes[8].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[8].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[3].Address}",ChangedAt = collectionRoutes[8].CollectionDate.ToDateTime(collectionRoutes[8].EstimatedTime).AddMinutes(10)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[9].Id,Status = "created",StatusDescription = "Người dùng đã tạo yêu̟ cầu.",ChangedAt = posts[9].Date},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[9].Id,Status = "scheduled",StatusDescription = "Tài xế Ngô Văn Dũng đã nhận tuyến.",ChangedAt = collectionRoutes[9].CollectionDate.ToDateTime(collectionRoutes[9].EstimatedTime)},
			new ProductStatusHistory{ProductStatusHistoryId = Guid.NewGuid(),ProductId = products[9].Id,Status = "collected",StatusDescription = $"Lấy hàng thành công tại: {users[4].Address}",ChangedAt = collectionRoutes[9].CollectionDate.ToDateTime(collectionRoutes[9].EstimatedTime).AddMinutes(15)},
		};

		// === 21. POINT TRANSACTIONS ===
		public static List<PointTransactions> points = new List<PointTransactions>();

		// === 22. USER POINTS ===
		public static List<UserPoints> userPoints = new()
		{
			// (Giữ nguyên data UserPoints)
			new UserPoints
			{
				Id = Guid.NewGuid(),
				UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"), // Trần Huỳnh Bảo Ngọc
				Points = 150
			},
			new UserPoints
			{
				Id = Guid.NewGuid(),
				UserId = Guid.Parse("b73a62a7-8b90-43cf-9ad7-2abf96f34a52"), // Lê Thị Mai
				Points = 75.5
			},
			new UserPoints
			{
				Id = Guid.NewGuid(),
				UserId = Guid.Parse("e9b4b9de-b3b0-49ad-b90c-74c24a26b57a"), // Nguyễn Minh Khôi
				Points = 220
			},
			new UserPoints
			{
				Id = Guid.NewGuid(),
				UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"), // Phạm Thị Hằng
				Points = 0 // User mới, chưa có điểm
			},
			new UserPoints
			{
				Id = Guid.NewGuid(),
				UserId = Guid.Parse("c40deff9-163b-49e8-b967-238f22882b63"), // Đỗ Quốc Bảo
				Points = 50
			}
		};
	}
}