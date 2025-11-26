using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using OpenCvSharp.Features2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Data
{
	public static class FakeDataSeeder
	{
		// =========================================================================
		// 1. LOGIC THỜI GIAN ĐỘNG (VIETNAM TIME)
		// =========================================================================

		// Lấy giờ UTC hiện tại cộng 7 tiếng để ra giờ VN.
		// Biến này sẽ được khởi tạo ngay khi ứng dụng chạy (hoặc khi class được gọi lần đầu).
		// Dữ liệu sẽ luôn là: Hôm qua, Hôm nay, Ngày mai tính từ lúc chạy app.
		private static readonly DateTime _vnNow = DateTime.UtcNow.AddHours(7);

		// =========================================================================
		// 2. HELPER CONSTANTS & IDs
		// =========================================================================

		// Size & Prod IDs (Helpers)
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
		private static readonly Guid cat_NoiComDien = Guid.Parse("44444444-4444-4444-4444-000000000001");

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
		private static readonly Guid st_ManHinhMayTinh_nho = Guid.Parse("a1a1a1a1-0006-0006-0006-000000000012");


		// Brands
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
		private static readonly Guid brand_Dyson = Guid.Parse("b0100001-0012-0012-0012-000000000001");
		private static readonly Guid brand_Cuckoo = Guid.Parse("b0100001-0013-0013-0013-000000000001");

		// Package
		private static readonly string pkg_T001 = "T-001-VHP";

		
		// =========================================================================
		// 3. DYNAMIC ID GENERATION
		// =========================================================================

		private static readonly Guid[] prodIds = Enumerable.Range(0, 15).Select(_ => Guid.NewGuid()).ToArray();
		private static readonly Guid[] postIds = Enumerable.Range(0, 15).Select(_ => Guid.NewGuid()).ToArray();

        static FakeDataSeeder()
        {
            InitPostImages();
            
            //AddPostsForDay16();
            //AddPostsForDay22();


            AddPostsForDay27();

            //AddLoadBalancingTestData();
            //AddFixedAssignTestData();
            //AddFullGroupingDemoData();

            //SeedGroupingServiceTestData();
        }

		// =========================================================================
		// 4. USERS
		// =========================================================================
		private static readonly Guid collector_Dung_Id = Guid.Parse("6df4af85-6a59-4a0a-8513-1d7859fbd789");
		private static readonly Guid collector_Tuan_Id = Guid.Parse("c011ec70-b861-468f-b648-812e90f01a7e");
		public static List<User> users = new()
		{
			new User { UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"), Name = "Trần Huỳnh Bảo Ngọc", Email = "ngocthbse183850@fpt.edu.vn", Phone = "0901234567", Avatar = "https://picsum.photos/id/1011/200/200",Role = "User" },
			new User { UserId = Guid.Parse("b73a62a7-8b90-43cf-9ad7-2abf96f34a52"), Name = "Lê Thị Mai", Email = "le.thi.mai@example.com", Phone = "0987654321", Avatar = "https://picsum.photos/id/1025/200/200", Role = "User" },
			new User { UserId = Guid.Parse("e9b4b9de-b3b0-49ad-b90c-74c24a26b57a"), Name = "Nguyễn Minh Khôi", Email = "nguyen.minh.khoi@example.com", Phone = "0908123456", Avatar = "https://picsum.photos/id/1033/200/200", Role = "User" },
			new User { UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"), Name = "Phạm Thị Hằng", Email = "pham.thi.hang@example.com", Phone = "0911222333",  Avatar = "https://picsum.photos/id/1045/200/200",  Role = "User" },
			new User { UserId = Guid.Parse("c40deff9-163b-49e8-b967-238f22882b63"), Name = "Đỗ Quốc Bảo", Email = "do.quoc.bao@example.com", Phone = "0977222333",  Avatar = "https://picsum.photos/id/1059/200/200",Role = "User" },
			new User { UserId = Guid.Parse("c20deff9-163b-49e8-b967-238f22882b65"), Name = "Admin thu gom nhỏ", Email = "adminthugomnho@gmail.com", Phone = "0977222333", Avatar = "https://picsum.photos/id/1059/200/200", Role = "AdminWarehouse", SmallCollectionPointId = 1 },
			new User
	{
		UserId = collector_Dung_Id,
		Name = "Ngô Văn Dũng",
		Email = "ngo.van.dung@ewc.vn",
		Phone = "0905999888",
		Avatar = "https://picsum.photos/id/1062/200/200",
		SmallCollectionPointId = 1, // Thuộc trạm 1 (Vinhomes)
		Role = UserRole.Collector.ToString()
	},
	new User
	{
		UserId = collector_Tuan_Id,
		Name = "Lê Minh Tuấn",
		Email = "le.minh.tuan@ewc.vn",
		Phone = "0905111222",
		Avatar = "https://picsum.photos/id/1063/200/200",
		SmallCollectionPointId = 1, // Thuộc trạm 1 (Vinhomes)
		Role = UserRole.Collector.ToString()

	}

                //new User { UserId = Guid.Parse("c20deff9-163b-49e8-b967-238f22882b66"), Name = "Admin thu gom nhỏ 9001", Email = "adminthugomnho9001@gmail.com", Phone = "0977222333", Address = "Công viên Bóng tối Vinhomes – Khu đô thị Vinhomes Grand Park", Avatar = "https://picsum.photos/id/1059/200/200", Iat = 10.839000, Ing = 106.833800, Role = "Admin_SmallCollectionPoint", SmallCollectionPointId = 9001 }
        };
		// Collectors
		
//		public static List<User> collectors = new()
//{
//	new User
//	{
//		UserId = collector_Dung_Id,
//		Name = "Ngô Văn Dũng",
//		Email = "ngo.van.dung@ewc.vn",
//		Phone = "0905999888",
//		Avatar = "https://picsum.photos/id/1062/200/200",
//		SmallCollectionPointId = 1, // Thuộc trạm 1 (Vinhomes)
//		Role = UserRole.Collector.ToString()
//	},
//	new User
//	{
//		UserId = collector_Tuan_Id,
//		Name = "Lê Minh Tuấn",
//		Email = "le.minh.tuan@ewc.vn",
//		Phone = "0905111222",
//		Avatar = "https://picsum.photos/id/1063/200/200",
//		SmallCollectionPointId = 1, // Thuộc trạm 1 (Vinhomes)
//		Role = UserRole.Collector.ToString()

//	}
//};
		public static List<Account> accounts = new()
		{
			new Account { AccountId = Guid.NewGuid(), UserId = collector_Dung_Id, Username = "collector.dung", PasswordHash = "123456"},
			new Account { AccountId = Guid.NewGuid(), UserId = collector_Tuan_Id, Username = "collector.tuan", PasswordHash = "123456"},
			new Account { AccountId = Guid.NewGuid(), UserId = Guid.Parse("c20deff9-163b-49e8-b967-238f22882b65"), Username = "adminwarehouse", PasswordHash = "123456"},
		};
		public static List<UserAddress> userAddress = new()
		{
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"),
				Address = "Vinhomes Grand Park – Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Iat = 10.842003, 
				Ing = 106.829580,
				isDefault = true
			},
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("b73a62a7-8b90-43cf-9ad7-2abf96f34a52"),
				Address = "Vincom Mega Mall Grand Park – Đường Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
				Iat = 10.843450, 
				Ing = 106.829900,
				isDefault = true
			},
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("e9b4b9de-b3b0-49ad-b90c-74c24a26b57a"),
				Address = "Trường THCS Long Thạnh Mỹ – Đường Long Thạnh Mỹ, TP. Thủ Đức",
				Iat = 10.845900,
				Ing = 106.833400,
				isDefault = true
			},
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"),
				Address = "UBND Phường Long Thạnh Mỹ – 86 Nguyễn Xiển, TP. Thủ Đức",
				Iat = 10.841000, 
				Ing = 106.830000,
				isDefault = true
			},
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"),
				Address = "Công viên Ánh Sáng Vinhomes – Khu đô thị Vinhomes Grand Park",
				 Iat = 10.839000, 
				Ing = 106.833800,
				isDefault = true
			},
			new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = Guid.Parse("c20deff9-163b-49e8-b967-238f22882b65"),
				Address = "Công viên Ánh Sáng Vinhomes – Khu đô thị Vinhomes Grand Park",
				Iat = 10.839000,
				Ing = 106.833800,
				isDefault = true
			},

		};
		private static string CreateSchedule(int daysFromNow, string start, string end)
		{
			// Sử dụng _vnNow để tính ngày cho lịch trình
			var targetDate = _vnNow.AddDays(daysFromNow);
			var schedule = new List<DailyTimeSlots>
			{
				new DailyTimeSlots
				{
					DayName = $"Thứ {((int)targetDate.DayOfWeek == 0 ? 8 : (int)targetDate.DayOfWeek + 1)}",
					PickUpDate = DateOnly.FromDateTime(targetDate),
					Slots = new TimeSlotDetail { StartTime = start, EndTime = end }
				}
			};
			return JsonSerializer.Serialize(schedule);
		}

		// =========================================================================
		// 5. STATIC DATA LISTS
		// =========================================================================
		public static List<Category> categories = new()
		{
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
			new Category { Id = cat_NoiComDien, Name = "Nồi cơm điện", ParentCategoryId = parent4_Id },
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

		public static List<Attributes> attributes = new()
		{
			new Attributes { Id = att_KichThuocManHinh, Name = "Kích thước màn hình (inch)", },
			new Attributes { Id = att_ChieuDai, Name = "Chiều dài (cm)", },
			new Attributes { Id = att_ChieuRong, Name = "Chiều rộng (cm)", },
			new Attributes { Id = att_ChieuCao, Name = "Chiều cao (cm)", },
			new Attributes { Id = att_DungTich, Name = "Dung tích (lít)", },
			new Attributes { Id = att_KhoiLuongGiat, Name = "Khối lượng giặt (kg)", },
			new Attributes { Id = att_TrongLuong, Name = "Trọng lượng (kg)", }
		};

		public static List<CategoryAttributes> categoryAttributes = new()
		{
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

		public static List<SizeTier> sizeTiers = new()
		{
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
			new SizeTier { SizeTierId = st_ManHinhMayTinh_nho, CategoryId = cat_ManHinhMayTinh, Name = "Nhỏ (Dưới 24 inch)", EstimatedWeight = 3, EstimatedVolume = 0.05 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_ManHinhMayTinh, Name = "Lớn (Từ 24 inch trở lên)", EstimatedWeight = 7, EstimatedVolume = 0.1 },
			new SizeTier { SizeTierId = st_Laptop_MongNhe, CategoryId = cat_Laptop, Name = "Mỏng nhẹ (Dưới 2kg)", EstimatedWeight = 1.5, EstimatedVolume = 0.01 },
			new SizeTier { SizeTierId = Guid.NewGuid(), CategoryId = cat_Laptop, Name = "Thường/Gaming (Từ 2kg trở lên)", EstimatedWeight = 3, EstimatedVolume = 0.02 },
		};

		public static List<Brand> brands = new()
		{
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
			new Brand { BrandId = brand_Dyson, CategoryId = cat_MayHutBui, Name = "Dyson" },
			new Brand { BrandId = brand_Cuckoo, CategoryId = cat_NoiComDien, Name = "Cuckoo" },

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

		public static List<Packages> packages = new()
		{
			new Packages
			{
				PackageId = pkg_T001,
				PackageName = "Thùng TV/Màn hình 01 (Vinhomes)",
				CreateAt = _vnNow.AddHours(-8), // SỬA
                SmallCollectionPointsId = 1,
				Status = "Đang vận chuyển"
			}
		};

		// =========================================================================
		// 6. PRODUCTS - POSTS - ROUTES (15 Items) - DYNAMIC DATES
		// =========================================================================

		public static List<Products> products = new List<Products>
		{
            // --- HÔM QUA (0-4) ---
            new Products { Id = prodIds[0], CategoryId = cat_Tivi, SizeTierId = st_Tivi_TrungBinh, BrandId = brand_Samsung_Tivi, PackageId = pkg_T001, Status = "Đang vận chuyển", Description = "Hỏng màn hình", QRCode = "product1" },
			new Products { Id = prodIds[1], CategoryId = cat_TuLanh, SizeTierId = st_TuLanh_Lon, BrandId = brand_Pana_TuLanh, PackageId = null,  Status = "Đã thu gom", Description = "Không lạnh ngăn mát", QRCode = "product2" },
			new Products { Id = prodIds[2], CategoryId = cat_Laptop, SizeTierId = st_Laptop_MongNhe, BrandId = brand_Acer_Laptop, PackageId = null, Status = "Hủy bỏ", Description = "Khách hủy yêu cầu" },
			new Products { Id = prodIds[3], CategoryId = cat_QuatDien, SizeTierId = null, BrandId = brand_Asia_Quat, PackageId = null,  Status = "Đã thu gom", Description = "Gãy cánh", QRCode = "product4" },
			new Products { Id = prodIds[4], CategoryId = cat_MayGiat, SizeTierId = st_MayGiat_TrungBinh, BrandId = brand_Toshiba_MayGiat, PackageId = null, Status = "Đã thu gom", Description = "Kêu to khi vắt", QRCode = "product5" },

            // --- HÔM NAY (5-9) ---
            new Products { Id = prodIds[5], CategoryId = cat_ManHinhMayTinh, SizeTierId = st_ManHinhMayTinh_nho, BrandId = brand_Dell_PC, PackageId = null, Status = "Đã thu gom", Description = "Sọc màn hình", QRCode = "product6" },
			new Products { Id = prodIds[6], CategoryId = cat_LoViSong, SizeTierId = null, BrandId = brand_Sharp_LoViSong, PackageId = null, Status = "Chờ thu gom", Description = "Không nóng" },
			new Products { Id = prodIds[7], CategoryId = cat_BinhNuocNong, SizeTierId = null, BrandId = brand_Ariston_Binh, PackageId = null,  Status = "Chờ thu gom", Description = "Rò điện" },
			new Products { Id = prodIds[8], CategoryId = cat_MayIn, SizeTierId = null, BrandId = brand_HP_MayIn, PackageId = null, Status = "Đã thu gom", Description = "Kẹt giấy liên tục", QRCode = "product9" },
			new Products { Id = prodIds[9], CategoryId = cat_DienThoai, SizeTierId = null, BrandId = brand_Apple_DienThoai, PackageId = null,  Status = "Chờ thu gom", Description = "Vỡ màn hình" },

            // --- NGÀY MAI (10-14) ---
            new Products { Id = prodIds[10], CategoryId = cat_MayHutBui, SizeTierId = null, BrandId = brand_Dyson, PackageId = null, Status = "Chờ thu gom", Description = "Hỏng pin" },
			new Products { Id = prodIds[11], CategoryId = cat_Loa, SizeTierId = null, BrandId = brand_JBL_Loa, PackageId = null,Status = "Chờ thu gom", Description = "Mất tiếng bass" },
			new Products { Id = prodIds[12], CategoryId = cat_LoViSong, SizeTierId = null, BrandId = brand_Sharp_LoViSong, PackageId = null, Status = "Chờ thu gom", Description = "Hỏng rơ le" },
			new Products { Id = prodIds[13], CategoryId = cat_MayTinhDeBan, SizeTierId = null, BrandId = brand_Dell_PC, PackageId = null,  Status = "Chờ thu gom", Description = "Main hỏng" },
			new Products { Id = prodIds[14], CategoryId = cat_NoiComDien, SizeTierId = null, BrandId = brand_Cuckoo, PackageId = null,  Status = "Chờ thu gom", Description = "Không chín cơm" }
		};

		public static List<ProductValues> productValues = new List<ProductValues>
		{
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodIds[0], AttributeId = att_KichThuocManHinh, Value = 42 },
			new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodIds[4], AttributeId = att_KhoiLuongGiat, Value = 9 }
		};

		public static List<Post> posts = new List<Post>
		{
            // --- HÔM QUA (-1 day) ---
            // Sử dụng _vnNow.AddDays(-X) để đảm bảo thời gian luôn trôi theo ngày hiện tại
            new Post { Id = postIds[0], ProductId = prodIds[0], SenderId = users[0].UserId, Name = "Thanh lý Tivi hỏng", Date = _vnNow.AddDays(-3), Status = "Đã duyệt", ScheduleJson = CreateSchedule(-1, "08:00", "09:00"), Address = userAddress[0].Address, EstimatePoint = 100 },
			new Post { Id = postIds[1], ProductId = prodIds[1], SenderId = users[1].UserId, Name = "Tủ lạnh cũ cần bỏ", Date = _vnNow.AddDays(-2), Status = "Đã duyệt", ScheduleJson = CreateSchedule(-1, "09:30", "10:30"), Address = userAddress[1].Address, EstimatePoint = 200 },
			new Post { Id = postIds[2], ProductId = prodIds[2], SenderId = users[2].UserId, Name = "Laptop cũ", Date = _vnNow.AddDays(-4), Status = "Đã duyệt", ScheduleJson = CreateSchedule(-1, "10:00", "11:00"), Address = userAddress[2].Address, EstimatePoint = 150 },
			new Post { Id = postIds[3], ProductId = prodIds[3], SenderId = users[3].UserId, Name = "Quạt hỏng", Date = _vnNow.AddDays(-2), Status = "Đã duyệt", ScheduleJson = CreateSchedule(-1, "13:00", "14:00"), Address = userAddress[3].Address, EstimatePoint = 50 },
			new Post { Id = postIds[4], ProductId = prodIds[4], SenderId = users[4].UserId, Name = "Máy giặt cũ", Date = _vnNow.AddDays(-5), Status = "Đã duyệt", ScheduleJson = CreateSchedule(-1, "15:00", "16:00"), Address = userAddress[4].Address, EstimatePoint = 180 },

            // --- HÔM NAY (0 day) ---
            new Post { Id = postIds[5], ProductId = prodIds[5], SenderId = users[0].UserId, Name = "Màn hình máy tính", Date = _vnNow.AddDays(-1), Status = "Đã duyệt", ScheduleJson = CreateSchedule(0, "08:30", "09:30"), Address = userAddress[0].Address, EstimatePoint = 80 },
            new Post { Id = postIds[6], ProductId = prodIds[6], SenderId = users[0].UserId, Name = "Lò vi sóng hư", Date = _vnNow.AddDays(-2), Status = "Đã Duyệt", ScheduleJson = CreateSchedule(0, "10:00", "11:00"), Address = userAddress[0].Address, EstimatePoint = 120 },
            new Post { Id = postIds[7], ProductId = prodIds[7], SenderId = users[2].UserId, Name = "Bình nước nóng", Date = _vnNow.AddDays(-3), Status = "Đã Duyệt", ScheduleJson = CreateSchedule(0, "14:00", "15:00"), Address = userAddress[2].Address, EstimatePoint = 100 },
            new Post { Id = postIds[8], ProductId = prodIds[8], SenderId = users[3].UserId, Name = "Máy in văn phòng", Date = _vnNow.AddDays(-1), Status = "Đã duyệt", ScheduleJson = CreateSchedule(0, "09:00", "10:00"), Address = userAddress[3].Address, EstimatePoint = 90 },
            new Post { Id = postIds[9], ProductId = prodIds[9], SenderId = users[4].UserId, Name = "Điện thoại cũ", Date = _vnNow.AddDays(-2), Status = "Đã Duyệt", ScheduleJson = CreateSchedule(0, "16:00", "17:00"), Address = userAddress[4].Address, EstimatePoint = 200 },

            // --- NGÀY MAI (+1 day) ---
            new Post { Id = postIds[10], ProductId = prodIds[10], SenderId = users[0].UserId, Name = "Máy hút bụi", Date = _vnNow, Status = "Đã duyệt", ScheduleJson = CreateSchedule(1, "08:00", "09:00"), Address = userAddress[0].Address, EstimatePoint = 110 },
			new Post { Id = postIds[11], ProductId = prodIds[11], SenderId = users[1].UserId, Name = "Loa cũ", Date = _vnNow, Status = "Đã duyệt", ScheduleJson = CreateSchedule(1, "09:00", "10:00"), Address = userAddress[1].Address, EstimatePoint = 70 },
			new Post { Id = postIds[12], ProductId = prodIds[12], SenderId = users[2].UserId, Name = "Lò vi sóng", Date = _vnNow, Status = "Đã duyệt", ScheduleJson = CreateSchedule(1, "10:00", "11:00"), Address = userAddress[2].Address, EstimatePoint = 100 },
			new Post { Id = postIds[13], ProductId = prodIds[13], SenderId = users[3].UserId, Name = "Máy tính bàn", Date = _vnNow, Status = "Đã duyệt", ScheduleJson = CreateSchedule(1, "14:00", "15:00"), Address = userAddress[3].Address, EstimatePoint = 150 },
			new Post { Id = postIds[14], ProductId = prodIds[14], SenderId = users[4].UserId, Name = "Nồi cơm điện", Date = _vnNow, Status = "Đã duyệt", ScheduleJson = CreateSchedule(1, "15:30", "16:30"), Address = userAddress[4].Address, EstimatePoint = 60 }
		};

		public static List<ProductImages> productImages = new();

		static void InitPostImages()
		{
			var defaultImg = "https://picsum.photos/id/1/200/200";
			foreach (var product in products)
			{
				productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = product.Id, ImageUrl = defaultImg, AiDetectedLabelsJson = "[{\"Tag\":\"electronics\",\"Confidence\":90.0}]" });
			}
		}
		public static List<Vehicles> vehicles = new()
{
	new Vehicles
	{
		Id = 1,
		Plate_Number = "51A-12345",
		Vehicle_Type = "Xe tải nhỏ",
		Capacity_Kg = 1000,
		Capacity_M3 = 8,
		Radius_Km = 10,
		Status = "active",
		Small_Collection_Point = 1 // Thuộc trạm 1
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
		Small_Collection_Point = 1 // Thuộc trạm 1
    }
};
		// =========================================================================
		// 7. INFRASTRUCTURE & OPERATIONS (Shifts, Groups, Routes)
		// =========================================================================

		public static List<Shifts> shifts = new()
		{
            // --- HÔM QUA ---
            new Shifts { Id = 1, CollectorId = collector_Dung_Id, Vehicle_Id = 1, WorkDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), Shift_Start_Time = _vnNow.AddDays(-1).AddHours(7), Shift_End_Time = _vnNow.AddDays(-1).AddHours(15) },
			new Shifts { Id = 2, CollectorId = collector_Tuan_Id, Vehicle_Id = 2, WorkDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), Shift_Start_Time = _vnNow.AddDays(-1).AddHours(8), Shift_End_Time = _vnNow.AddDays(-1).AddHours(16) },

            // --- HÔM NAY ---
            new Shifts { Id = 3, CollectorId = collector_Dung_Id, Vehicle_Id = 1, WorkDate = DateOnly.FromDateTime(_vnNow), Shift_Start_Time = _vnNow.AddHours(7), Shift_End_Time = _vnNow.AddHours(15) },
			new Shifts { Id = 4, CollectorId = collector_Tuan_Id, Vehicle_Id = 2, WorkDate = DateOnly.FromDateTime(_vnNow), Shift_Start_Time = _vnNow.AddHours(8), Shift_End_Time = _vnNow.AddHours(16) },

            // --- NGÀY MAI ---
            new Shifts { Id = 5, CollectorId = collector_Dung_Id, Vehicle_Id = 1, WorkDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), Shift_Start_Time = _vnNow.AddDays(1).AddHours(7), Shift_End_Time = _vnNow.AddDays(1).AddHours(15) },
			new Shifts { Id = 6, CollectorId = collector_Tuan_Id, Vehicle_Id = 2, WorkDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), Shift_Start_Time = _vnNow.AddDays(1).AddHours(8), Shift_End_Time = _vnNow.AddDays(1).AddHours(16) }
		};

		public static List<CollectionGroups> collectionGroups = new()
		{
            // Hôm qua
            new CollectionGroups { Id = 1, Shift_Id = 1, Group_Code = "YESTERDAY-S1-DUNG", Name = "Tuyến Hôm Qua (Dũng)", Created_At = _vnNow.AddDays(-2) },
			new CollectionGroups { Id = 2, Shift_Id = 2, Group_Code = "YESTERDAY-S2-TUAN", Name = "Tuyến Hôm Qua (Tuấn)", Created_At = _vnNow.AddDays(-2) },

            // Hôm nay
            new CollectionGroups { Id = 3, Shift_Id = 3, Group_Code = "TODAY-S1-DUNG", Name = "Tuyến Hôm Nay (Dũng)", Created_At = _vnNow.AddHours(-10) },
			new CollectionGroups { Id = 4, Shift_Id = 4, Group_Code = "TODAY-S2-TUAN", Name = "Tuyến Hôm Nay (Tuấn)", Created_At = _vnNow.AddHours(-10) },

            // Ngày mai
            new CollectionGroups { Id = 5, Shift_Id = 5, Group_Code = "TOMORROW-S1-DUNG", Name = "Tuyến Ngày Mai (Dũng)", Created_At = _vnNow },
			new CollectionGroups { Id = 6, Shift_Id = 6, Group_Code = "TOMORROW-S2-TUAN", Name = "Tuyến Ngày Mai (Tuấn)", Created_At = _vnNow }
		};

		public static List<CollectionRoutes> collectionRoutes = new()
		{
            // --- HÔM QUA (5 Posts) ---
            new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[0], CollectionGroupId = 1, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), EstimatedTime = new TimeOnly(8, 30), Actual_Time = new TimeOnly(8, 45), Status = "Hoàn thành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[1], CollectionGroupId = 1, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), EstimatedTime = new TimeOnly(10, 0), Actual_Time = new TimeOnly(10, 15), Status = "Hoàn thành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[2], CollectionGroupId = 2, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), EstimatedTime = new TimeOnly(10, 30), Actual_Time = null, Status = "Hủy bỏ", RejectMessage = "Khách hàng vắng mặt" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[3], CollectionGroupId = 2, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), EstimatedTime = new TimeOnly(13, 30), Actual_Time = new TimeOnly(13, 45), Status = "Hoàn thành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[4], CollectionGroupId = 2, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(-1)), EstimatedTime = new TimeOnly(15, 30), Actual_Time = new TimeOnly(15, 45), Status = "Hoàn thành" },

            // --- HÔM NAY (5 Posts) ---
            new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[5], CollectionGroupId = 3, CollectionDate = DateOnly.FromDateTime(_vnNow), EstimatedTime = new TimeOnly(9, 0), Actual_Time = new TimeOnly(9, 15), Status = "Hoàn thành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[6], CollectionGroupId = 3, CollectionDate = DateOnly.FromDateTime(_vnNow), EstimatedTime = new TimeOnly(10, 30), Actual_Time = null, Status = "Đang tiến hành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[7], CollectionGroupId = 3, CollectionDate = DateOnly.FromDateTime(_vnNow), EstimatedTime = new TimeOnly(14, 30), Actual_Time = null, Status = "Đang tiến hành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[8], CollectionGroupId = 4, CollectionDate = DateOnly.FromDateTime(_vnNow), EstimatedTime = new TimeOnly(9, 30), Actual_Time = new TimeOnly(9, 45), Status = "Hoàn thành" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[9], CollectionGroupId = 4, CollectionDate = DateOnly.FromDateTime(_vnNow), EstimatedTime = new TimeOnly(16, 30), Actual_Time = null, Status = "Đang tiến hành" },

            // --- NGÀY MAI (5 Posts) ---
            new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[10], CollectionGroupId = 5, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), EstimatedTime = new TimeOnly(8, 30), Status = "Chưa bắt đầu" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[11], CollectionGroupId = 5, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), EstimatedTime = new TimeOnly(9, 30), Status = "Chưa bắt đầu" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[12], CollectionGroupId = 5, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), EstimatedTime = new TimeOnly(10, 30), Status = "Chưa bắt đầu" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[13], CollectionGroupId = 6, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), EstimatedTime = new TimeOnly(14, 30), Status = "Chưa bắt đầu" },
			new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), PostId = postIds[14], CollectionGroupId = 6, CollectionDate = DateOnly.FromDateTime(_vnNow.AddDays(1)), EstimatedTime = new TimeOnly(16, 0), Status = "Chưa bắt đầu" }
		};

		// =========================================================================
		// 8. HISTORY & POINTS
		// =========================================================================
		public static List<ProductStatusHistory> productStatusHistories = new()
		{
			new ProductStatusHistory
	{
		ProductStatusHistoryId = Guid.NewGuid(),
		ProductId = prodIds[0],
		Status = "Chờ duyệt",
		StatusDescription = "Người dùng tạo yêu cầu thu gom.",
		ChangedAt = _vnNow.AddDays(-3) // Khớp với ngày tạo Post
    },

    // 2. Admin duyệt bài - Chuyển sang Chờ thu gom
    new ProductStatusHistory
	{
		ProductStatusHistoryId = Guid.NewGuid(),
		ProductId = prodIds[0],
		Status = "Chờ thu gom",
		StatusDescription = "Admin đã duyệt yêu cầu. Đang điều phối người thu gom.",
		ChangedAt = _vnNow.AddDays(-2).AddHours(2) // Duyệt sau khi đăng khoảng 1 ngày
    },

    // 3. Collector đến lấy hàng - Đã thu gom
    new ProductStatusHistory
	{
		ProductStatusHistoryId = Guid.NewGuid(),
		ProductId = prodIds[0],
		Status = "Đã thu gom",
		StatusDescription = "Đã thu gom thành công tại địa chỉ của khách hàng.",
		ChangedAt = _vnNow.AddDays(-1).AddHours(9) // Thu gom vào khung giờ sáng hôm qua
    },

    // 4. Mang về kho tập kết - Nhập kho
    new ProductStatusHistory
	{
		ProductStatusHistoryId = Guid.NewGuid(),
		ProductId = prodIds[0],
		Status = "Nhập kho",
		StatusDescription = "Sản phẩm đã được vận chuyển về kho trung tâm.",
		ChangedAt = _vnNow.AddDays(-1).AddHours(14) // Về kho vào chiều hôm qua
    },

    // 5. Trạng thái hiện tại - Đã đóng gói (trùng với status trong object Products)
    new ProductStatusHistory
	{
		ProductStatusHistoryId = Guid.NewGuid(),
		ProductId = prodIds[0],
		Status = "Đã đóng thùng",
		StatusDescription = "Sản phẩm đã được phân loại và đóng gói kỹ càng.",
		ChangedAt = _vnNow.AddHours(-2) // Mới đóng gói cách đây 2 tiếng
    }
		};
		
		


        public static List<PointTransactions> points = new List<PointTransactions>()
        {
          new PointTransactions{ PointTransactionId = Guid.NewGuid(), PostId = posts[0].Id, CreatedAt = _vnNow.AddDays(-3), Point = 100, Desciption = "Thu gom thành công", UserId = posts[0].SenderId, ProductId = products[0].Id, TransactionType = "Earned"  },
        };

        public static List<UserPoints> userPoints = new()
        {
            new UserPoints { Id = Guid.NewGuid(), UserId = Guid.Parse("7f5c8b33-1b52-4d11-91b0-932c3d243c71"), Points = 100 },
            new UserPoints { Id = Guid.NewGuid(), UserId = Guid.Parse("b73a62a7-8b90-43cf-9ad7-2abf96f34a52"), Points = 75.5 },
            new UserPoints { Id = Guid.NewGuid(), UserId = Guid.Parse("e9b4b9de-b3b0-49ad-b90c-74c24a26b57a"), Points = 220 },
            new UserPoints { Id = Guid.NewGuid(), UserId = Guid.Parse("72b4ad6a-0b5b-45a3-bb6b-6e1790c84b45"), Points = 0 },
            new UserPoints { Id = Guid.NewGuid(), UserId = Guid.Parse("c40deff9-163b-49e8-b967-238f22882b63"), Points = 50 }
        };
        

		// =========================================================================
		// 9. SMALL COLLECTION POINTS – THÊM TRẠM MỚI CHO VINGHOMES GRAND PARK
		// =========================================================================

		public static List<SmallCollectionPoints> smallCollectionPoints = new()
		{
			new SmallCollectionPoints
			{
		Id = 1,
		Name = "Trạm Thu Gom Mini – Vinhomes Grand Park",
		Address = "Khu trung tâm – Nguyễn Xiển, Phường Long Thạnh Mỹ, TP. Thủ Đức",
		Latitude = 10.8420,
		Longitude = 106.8310,
		Status = "active",
		City_Team_Id = 1,
		Created_At = _vnNow.AddDays(-1),
		Updated_At = _vnNow
			}
		};

		// ======================================================================
		// 10. ADD EXTRA DATA FOR DAY 16 (NEW METHOD – SAFE EXTENSION)
		// ======================================================================
//		public static void AddPostsForDay16()
//		{
//			// 🔥 Tính đúng ngày 16 theo tháng hiện tại 
//			var day16DateTime = _vnNow.AddDays(27 - _vnNow.Day);
//			var day16 = DateOnly.FromDateTime(day16DateTime);

//			// ==================================================================
//			// A) USER MỚI
//			// ==================================================================
//			var extraUsers = new List<User>
//	{
//		new User { UserId = Guid.NewGuid(), Name = "User Test 01", Email = "u01@test.com", Phone = "0901111111",
//			Address = "Park 1 – Vinhomes GP", Avatar = "https://picsum.photos/id/31/200/200", Iat = 10.842500, Ing = 106.831500, Role = "User" },

//		new User { UserId = Guid.NewGuid(), Name = "User Test 02", Email = "u02@test.com", Phone = "0902222222",
//			Address = "Park 2 – Vinhomes GP", Avatar = "https://picsum.photos/id/32/200/200", Iat = 10.843200, Ing = 106.832200, Role = "User" },

//		new User { UserId = Guid.NewGuid(), Name = "User Test 03", Email = "u03@test.com", Phone = "0903333333",
//			Address = "Park 3 – Vinhomes GP", Avatar = "https://picsum.photos/id/33/200/200", Iat = 10.842900, Ing = 106.833000, Role = "User" },

//		new User { UserId = Guid.NewGuid(), Name = "User Test 04", Email = "u04@test.com", Phone = "0904444444",
//			Address = "Park 5 – Vinhomes GP", Avatar = "https://picsum.photos/id/34/200/200", Iat = 10.843600, Ing = 106.833400, Role = "User" }
//	};

//			users.AddRange(extraUsers);

//			var u1 = extraUsers[0].UserId;
//			var u2 = extraUsers[1].UserId;
//			var u3 = extraUsers[2].UserId;
//			var u4 = extraUsers[3].UserId;

//			// ==================================================================
//			// B) PRODUCTS
//			// ==================================================================
//			//        var prodA = Guid.NewGuid();
//			//        var prodB = Guid.NewGuid();
//			//        var prodC = Guid.NewGuid();
//			//        var prodD = Guid.NewGuid();

//			//        products.AddRange(new List<Products>
//			//{
//			//    new Products { Id = prodA, CategoryId = cat_LoViSong, BrandId = brand_Sharp_LoViSong, Status = "Chờ thu gom", Description = "Lò vi sóng hỏng" },
//			//    new Products { Id = prodB, CategoryId = cat_DienThoai, BrandId = brand_Apple_DienThoai, Status = "Chờ thu gom", Description = "Điện thoại vỡ" },
//			//    new Products { Id = prodC, CategoryId = cat_QuatDien, BrandId = brand_Asia_Quat, Status = "Chờ thu gom", Description = "Quạt không quay" },
//			//    new Products { Id = prodD, CategoryId = cat_MayHutBui, BrandId = brand_Dyson, Status = "Chờ thu gom", Description = "Máy hút bụi yếu" }
//			//});
//			var size_LoViSong = Guid.Parse("f3c8c4ef-56f3-433e-b210-3f900248ffae"); // >20L

//			// Tạo size tier tạm cho 3 loại chưa có tier
//			var size_DienThoai = Guid.NewGuid();
//			var size_QuatDien = Guid.NewGuid();
//			var size_MayHutBui = Guid.NewGuid();

//			// Thêm 3 size tier mới vào list chung
//			sizeTiers.AddRange(new List<SizeTier>
//{
//	new SizeTier { SizeTierId = size_DienThoai, CategoryId = cat_DienThoai, Name = "Điện thoại nhỏ", EstimatedWeight = 1, EstimatedVolume = 0.01 },
//	new SizeTier { SizeTierId = size_QuatDien, CategoryId = cat_QuatDien, Name = "Quạt đứng nhỏ", EstimatedWeight = 5, EstimatedVolume = 0.05 },
//	new SizeTier { SizeTierId = size_MayHutBui, CategoryId = cat_MayHutBui, Name = "Máy hút bụi tiêu chuẩn", EstimatedWeight = 6, EstimatedVolume = 0.07 }
//});

//			// --- Tạo product IDs ---
//			var prodA = Guid.NewGuid();
//			var prodB = Guid.NewGuid();
//			var prodC = Guid.NewGuid();
//			var prodD = Guid.NewGuid();

//			products.AddRange(new List<Products>
//{
//	new Products { Id = prodA, CategoryId = cat_LoViSong, BrandId = brand_Sharp_LoViSong, SizeTierId = size_LoViSong, Status = "Chờ gom nhóm", Description = "Lò vi sóng hỏng" },

//	new Products { Id = prodB, CategoryId = cat_DienThoai, BrandId = brand_Apple_DienThoai, SizeTierId = size_DienThoai, Status = "Chờ gom nhóm", Description = "Điện thoại vỡ" },

//	new Products { Id = prodC, CategoryId = cat_QuatDien, BrandId = brand_Asia_Quat, SizeTierId = size_QuatDien, Status = "Chờ gom nhóm", Description = "Quạt không quay" },

//	new Products { Id = prodD, CategoryId = cat_MayHutBui, BrandId = brand_Dyson, SizeTierId = size_MayHutBui, Status = "Chờ gom nhóm", Description = "Máy hút bụi yếu" }
//});

//			// ==================================================================
//			// C) POSTS NGÀY 16 – GIỜ THEO YÊU CẦU
//			// ==================================================================
//			var postA = Guid.NewGuid(); // 17–18
//			var postB = Guid.NewGuid(); // 18–20
//			var postC = Guid.NewGuid(); // 18–21
//			var postD = Guid.NewGuid(); // 19–20

//			posts.AddRange(new List<Post>
//	{
//		new Post { Id = postA, ProductId = prodA, SenderId = u1, Name = "Lò vi sóng – thu gom ngày 16",
//			Date = day16DateTime, Status = "Đã duyệt",
//			ScheduleJson = CreateScheduleJson(day16, "17:00", "18:00"),
//			Address = extraUsers[0].Address, EstimatePoint = 100 },

//		new Post { Id = postB, ProductId = prodB, SenderId = u2, Name = "Điện thoại – thu gom ngày 16",
//			Date = day16DateTime, Status = "Đã duyệt",
//			ScheduleJson = CreateScheduleJson(day16, "18:00", "20:00"),
//			Address = extraUsers[1].Address, EstimatePoint = 120 },

//		new Post { Id = postC, ProductId = prodC, SenderId = u3, Name = "Quạt điện – thu gom ngày 16",
//			Date = day16DateTime, Status = "Đã duyệt",
//			ScheduleJson = CreateScheduleJson(day16, "18:00", "21:00"),
//			Address = extraUsers[2].Address, EstimatePoint = 90 },

//		new Post { Id = postD, ProductId = prodD, SenderId = u4, Name = "Máy hút bụi – thu gom ngày 16",
//			Date = day16DateTime, Status = "Đã duyệt",
//			ScheduleJson = CreateScheduleJson(day16, "19:00", "20:00"),
//			Address = extraUsers[3].Address, EstimatePoint = 140 }
//	});

//			// ==================================================================
//			// D) HÌNH ẢNH
//			// ==================================================================
//			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodA, ImageUrl = "https://picsum.photos/id/41/200/200", AiDetectedLabelsJson = "[]" });
//			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodB, ImageUrl = "https://picsum.photos/id/42/200/200", AiDetectedLabelsJson = "[]" });
//			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodC, ImageUrl = "https://picsum.photos/id/43/200/200", AiDetectedLabelsJson = "[]" });
//			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodD, ImageUrl = "https://picsum.photos/id/44/200/200", AiDetectedLabelsJson = "[]" });

//			// ==================================================================
//			// E) SHIFT NGÀY 16: 16:00 – 22:00
//			// ==================================================================
//			var shiftId1 = shifts.Count + 1;
//			var shiftId2 = shifts.Count + 2;

//			shifts.Add(new Shifts
//			{
//				Id = shiftId1,
//				CollectorId = collector_Dung_Id,
//				Vehicle_Id = 1,
//				WorkDate = day16,
//				Shift_Start_Time = day16DateTime.Date.AddHours(16),
//				Shift_End_Time = day16DateTime.Date.AddHours(22)
//			});

//			shifts.Add(new Shifts
//			{
//				Id = shiftId2,
//				CollectorId = collector_Tuan_Id,
//				Vehicle_Id = 2,
//				WorkDate = day16,
//				Shift_Start_Time = day16DateTime.Date.AddHours(16),
//				Shift_End_Time = day16DateTime.Date.AddHours(22)
//			});

//			// ==================================================================
//			// F) GROUP NGÀY 16
//			// ==================================================================
//			var group1 = new CollectionGroups
//			{
//				Id = collectionGroups.Count + 1,
//				Shift_Id = shiftId1,
//				Group_Code = $"DAY16-S1-DUNG",
//				Name = "Tuyến Ngày 16 – Dũng",
//				Created_At = _vnNow
//			};

//			var group2 = new CollectionGroups
//			{
//				Id = collectionGroups.Count + 2,
//				Shift_Id = shiftId2,
//				Group_Code = $"DAY16-S2-TUAN",
//				Name = "Tuyến Ngày 16 – Tuấn",
//				Created_At = _vnNow
//			};

//            collectionGroups.Add(group1);
//            collectionGroups.Add(group2);


//            // ==================================================================
//            // G) ROUTES – THỜI GIAN TƯƠNG ỨNG
//            // ==================================================================
//            //collectionRoutes.Add(new CollectionRoutes
//            //{
//            //    CollectionRouteId = Guid.NewGuid(),
//            //    PostId = postA,
//            //    CollectionGroupId = group1.Id,
//            //    CollectionDate = day16,
//            //    EstimatedTime = new TimeOnly(17, 00),
//            //    Status = "Chưa bắt đầu"
//            //});

//            //collectionRoutes.Add(new CollectionRoutes
//            //{
//            //    CollectionRouteId = Guid.NewGuid(),
//            //    PostId = postB,
//            //    CollectionGroupId = group1.Id,
//            //    CollectionDate = day16,
//            //    EstimatedTime = new TimeOnly(18, 00),
//            //    Status = "Chưa bắt đầu"
//            //});

//            //collectionRoutes.Add(new CollectionRoutes
//            //{
//            //    CollectionRouteId = Guid.NewGuid(),
//            //    PostId = postC,
//            //    CollectionGroupId = group2.Id,
//            //    CollectionDate = day16,
//            //    EstimatedTime = new TimeOnly(18, 00),
//            //    Status = "Chưa bắt đầu"
//            //});

//            //collectionRoutes.Add(new CollectionRoutes
//            //{
//            //    CollectionRouteId = Guid.NewGuid(),
//            //    PostId = postD,
//            //    CollectionGroupId = group2.Id,
//            //    CollectionDate = day16,
//            //    EstimatedTime = new TimeOnly(19, 00),
//            //    Status = "Chưa bắt đầu"
//            //});
//        }

		// ======================================================================
		// HELPER TẠO SCHEDULE NGÀY 16
		// ======================================================================
		private static string CreateScheduleJson(DateOnly date, string start, string end)
		{
			return JsonSerializer.Serialize(new List<DailyTimeSlots>
	{
		new DailyTimeSlots
		{
			DayName = $"Ngày {date.Day}",
			PickUpDate = date,
			Slots = new TimeSlotDetail { StartTime = start, EndTime = end }
		}
	});
		}


		//Asign Day
		public class StagingAssignDay
		{
			public DateOnly Date { get; set; }
			public int PointId { get; set; }
			public int VehicleId { get; set; }
			public List<Guid> PostIds { get; set; } = new();
		}

		public static List<StagingAssignDay> stagingAssignDays = new();

        //      // ======================================================================
        //      // HELPER SINH ID CỐ ĐỊNH (Để test dễ dàng)
        //      // Format: 000000{day}-000{type}-0000-0000-{index:12số}
        //      // Type: 1=Product, 2=Post, 3=Image
        //      // ======================================================================
        //      private static Guid MakeFixedId(int day, int type, int index)
        //      {
        //          string guidString = $"{day:D8}-{type:D4}-{type:D4}-{type:D4}-{index:D12}";
        //          return Guid.Parse(guidString);
        //      }

        //      // ======================================================================
        //      // 11. DATA TEST TẢI TRỌNG & GOM NHÓM (NGÀY 21 - 24) - PHIÊN BẢN CHUẨN
        //      // ======================================================================
        //      public static void AddLoadBalancingTestData()
        //      {
        //          var currentMonth = _vnNow.Month;
        //          var currentYear = _vnNow.Year;

        //          var date21 = new DateTime(currentYear, currentMonth, 21);
        //          var date22 = new DateTime(currentYear, currentMonth, 22);
        //          var date23 = new DateTime(currentYear, currentMonth, 23);
        //          var date24 = new DateTime(currentYear, currentMonth, 24);

        //          // -------------------------------------------------------------------
        //          // 1. TẠO CA LÀM VIỆC (SHIFTS)
        //          // -------------------------------------------------------------------
        //          var testDates = new List<DateTime> { date21, date22, date23, date24 };
        //          foreach (var d in testDates)
        //          {
        //              var dateOnly = DateOnly.FromDateTime(d);
        //              // Ca 1: Xe tải nhỏ (Dũng) - 07:00 đến 15:00
        //              shifts.Add(new Shifts { Id = shifts.Count + 1, CollectorId = collector_Dung_Id, Vehicle_Id = 1, WorkDate = dateOnly, Shift_Start_Time = d.Date.AddHours(7), Shift_End_Time = d.Date.AddHours(15) });
        //              // Ca 2: Xe tải lớn (Tuấn) - 08:00 đến 17:00
        //              shifts.Add(new Shifts { Id = shifts.Count + 2, CollectorId = collector_Tuan_Id, Vehicle_Id = 2, WorkDate = dateOnly, Shift_Start_Time = d.Date.AddHours(8), Shift_End_Time = d.Date.AddHours(17) });
        //          }

        //          // -------------------------------------------------------------------
        //          // 2. TẠO USER TEST
        //          // -------------------------------------------------------------------
        //          var bulkUsers = new List<User>();
        //          for (int i = 1; i <= 10; i++)
        //          {
        //              bulkUsers.Add(new User { UserId = Guid.NewGuid(), Name = $"Test User {i}", Email = $"t{i}@test.com", Phone = "0909", Address = $"Block C{i}", Role = "User", Iat = 10.8400 + (i * 0.0002), Ing = 106.8300 + (i * 0.0002) });
        //          }
        //          users.AddRange(bulkUsers);

        //          // -------------------------------------------------------------------
        //          // SCENARIO 1: NGÀY 21 - TEST QUÁ TẢI TRỌNG (20 Tủ lạnh x 80kg = 1600kg)
        //          // ID: 00000021-...
        //          // -------------------------------------------------------------------
        //          var idsDay21 = new List<Guid>();
        //          for (int i = 1; i <= 20; i++)
        //          {
        //              var prodId = MakeFixedId(21, 1, i);
        //              var postId = MakeFixedId(21, 2, i);
        //              var sender = bulkUsers[i % 10];

        //              products.Add(new Products { Id = prodId, CategoryId = cat_TuLanh, BrandId = brand_Pana_TuLanh, SizeTierId = st_TuLanh_Lon, Status = "Chờ gom nhóm", Description = $"Test Tủ lạnh {i}" });

        //              posts.Add(new Post
        //              {
        //                  Id = postId,
        //                  ProductId = prodId,
        //                  SenderId = sender.UserId,
        //                  Name = $"Tủ Lạnh Lớn {i}",
        //                  Date = date21,
        //                  Status = "Đã duyệt",
        //                  ScheduleJson = CreateScheduleJson(DateOnly.FromDateTime(date21), "08:00", "17:00"),
        //                  Address = sender.Address,
        //                  EstimatePoint = 200
        //              });
        //              postImages.Add(new PostImages { PostImageId = MakeFixedId(21, 3, i), PostId = postId, ImageUrl = "https://picsum.photos/200", AiDetectedLabelsJson = "[]" });

        //              idsDay21.Add(postId);
        //          }
        //          // Assign cho Xe Lớn (ID 2)
        //          stagingAssignDays.Add(new StagingAssignDay { Date = DateOnly.FromDateTime(date21), PointId = 1, VehicleId = 2, PostIds = idsDay21 });


        //          // -------------------------------------------------------------------
        //          // SCENARIO 2: NGÀY 22 - TEST GOM ĐƯỜNG (15 Màn hình)
        //          // ID: 00000022-...
        //          // -------------------------------------------------------------------
        //          var idsDay22 = new List<Guid>();
        //          for (int i = 1; i <= 15; i++)
        //          {
        //              var prodId = MakeFixedId(22, 1, i);
        //              var postId = MakeFixedId(22, 2, i);
        //              var sender = bulkUsers[i % 5];

        //              products.Add(new Products { Id = prodId, CategoryId = cat_ManHinhMayTinh, BrandId = brand_Dell_PC, SizeTierId = sizeTiers.First(x => x.CategoryId == cat_ManHinhMayTinh).SizeTierId, Status = "Chờ gom nhóm", Description = $"Màn hình {i}" });

        //              posts.Add(new Post
        //              {
        //                  Id = postId,
        //                  ProductId = prodId,
        //                  SenderId = sender.UserId,
        //                  Name = $"Màn hình cũ {i}",
        //                  Date = date22,
        //                  Status = "Đã duyệt",
        //                  ScheduleJson = CreateScheduleJson(DateOnly.FromDateTime(date22), "09:00", "11:00"),
        //                  Address = sender.Address,
        //                  EstimatePoint = 50
        //              });
        //              postImages.Add(new PostImages { PostImageId = MakeFixedId(22, 3, i), PostId = postId, ImageUrl = "https://picsum.photos/200", AiDetectedLabelsJson = "[]" });

        //              idsDay22.Add(postId);
        //          }
        //          // Assign cho Xe Nhỏ (ID 1)
        //          stagingAssignDays.Add(new StagingAssignDay { Date = DateOnly.FromDateTime(date22), PointId = 1, VehicleId = 1, PostIds = idsDay22 });


        //          // -------------------------------------------------------------------
        //          // SCENARIO 3: NGÀY 23 - TEST CA CHIỀU (5 Máy giặt)
        //          // ID: 00000023-...
        //          // -------------------------------------------------------------------
        //          var idsDay23 = new List<Guid>();
        //          for (int i = 1; i <= 5; i++)
        //          {
        //              var prodId = MakeFixedId(23, 1, i);
        //              var postId = MakeFixedId(23, 2, i);

        //              products.Add(new Products { Id = prodId, CategoryId = cat_MayGiat, BrandId = brand_Toshiba_MayGiat, SizeTierId = st_MayGiat_TrungBinh, Status = "Chờ gom nhóm", Description = $"Máy giặt {i}" });
        //              posts.Add(new Post
        //              {
        //                  Id = postId,
        //                  ProductId = prodId,
        //                  SenderId = bulkUsers[i].UserId,
        //                  Name = $"Máy giặt {i}",
        //                  Date = date23,
        //                  Status = "Đã duyệt",
        //                  ScheduleJson = CreateScheduleJson(DateOnly.FromDateTime(date23), "13:00", "16:00"),
        //                  Address = bulkUsers[i].Address,
        //                  EstimatePoint = 150
        //              });
        //              postImages.Add(new PostImages { PostImageId = MakeFixedId(23, 3, i), PostId = postId, ImageUrl = "https://picsum.photos/200", AiDetectedLabelsJson = "[]" });
        //              idsDay23.Add(postId);
        //          }
        //          stagingAssignDays.Add(new StagingAssignDay { Date = DateOnly.FromDateTime(date23), PointId = 1, VehicleId = 1, PostIds = idsDay23 });


        //          // -------------------------------------------------------------------
        //          // SCENARIO 4: NGÀY 24 - TEST CA SÁNG (5 Laptop)
        //          // ID: 00000024-...
        //          // -------------------------------------------------------------------
        //          var idsDay24 = new List<Guid>();
        //          for (int i = 5; i < 10; i++)
        //          {
        //              var prodId = MakeFixedId(24, 1, i);
        //              var postId = MakeFixedId(24, 2, i);

        //              products.Add(new Products { Id = prodId, CategoryId = cat_Laptop, BrandId = brand_Acer_Laptop, SizeTierId = st_Laptop_MongNhe, Status = "Chờ gom nhóm", Description = $"Laptop {i}" });
        //              posts.Add(new Post
        //              {
        //                  Id = postId,
        //                  ProductId = prodId,
        //                  SenderId = bulkUsers[i].UserId,
        //                  Name = $"Laptop {i}",
        //                  Date = date24,
        //                  Status = "Đã duyệt",
        //                  ScheduleJson = CreateScheduleJson(DateOnly.FromDateTime(date24), "08:00", "12:00"),
        //                  Address = bulkUsers[i].Address,
        //                  EstimatePoint = 100
        //              });
        //              postImages.Add(new PostImages { PostImageId = MakeFixedId(24, 3, i), PostId = postId, ImageUrl = "https://picsum.photos/200", AiDetectedLabelsJson = "[]" });
        //              idsDay24.Add(postId);
        //          }
        //          stagingAssignDays.Add(new StagingAssignDay { Date = DateOnly.FromDateTime(date24), PointId = 1, VehicleId = 1, PostIds = idsDay24 });
        //      }


        public static List<TeamRatioItem> TeamRatios = new();

		//      // ======================================================================
		//      // 13. FIXED TEST DATA – 10 POSTS WITH FIXED GUID
		//      // ======================================================================

		//      public static List<Guid> FixedTestPostIds = new();

		//      public static void AddFixedAssignTestData()
		//      {
		//          // 1) Create 10 users test
		//          var fixedUsers = new List<User>();
		//          for (int i = 1; i <= 10; i++)
		//          {
		//              fixedUsers.Add(new User
		//              {
		//                  UserId = Guid.NewGuid(),
		//                  Name = $"Fixed User {i}",
		//                  Email = $"fixed{i}@test.com",
		//                  Phone = "09090099",
		//                  Address = $"Fixed Address {i}",
		//                  Role = "User",
		//                  Iat = 10.8400 + (i * 0.0001),
		//                  Ing = 106.8300 + (i * 0.0001)
		//              });
		//          }
		//          users.AddRange(fixedUsers);

		//          // 2) Create 10 products with fixed GUID
		//          var fixedProducts = new List<Guid>();
		//          for (int i = 1; i <= 10; i++)
		//          {
		//              var prodId = MakeFixedId(99, 1, i);
		//              fixedProducts.Add(prodId);

		//              products.Add(new Products
		//              {
		//                  Id = prodId,
		//                  CategoryId = cat_Laptop,
		//                  BrandId = brand_Acer_Laptop,
		//                  SizeTierId = st_Laptop_MongNhe,
		//                  Status = "Chờ assign",
		//                  Description = $"Fixed Product {i}"
		//              });
		//          }

		//          // 3) Create 10 posts with fixed GUID
		//          for (int i = 1; i <= 10; i++)
		//          {
		//              var postId = MakeFixedId(99, 2, i);
		//              FixedTestPostIds.Add(postId);

		//              posts.Add(new Post
		//              {
		//                  Id = postId,
		//                  ProductId = fixedProducts[i - 1],
		//                  SenderId = fixedUsers[i - 1].UserId,
		//                  Name = $"Fixed Test Post {i}",
		//                  Address = fixedUsers[i - 1].Address,
		//                  Date = DateTime.UtcNow.AddHours(7),
		//                  Status = "Đã duyệt",
		//                  EstimatePoint = 50 + i,
		//                  ScheduleJson = JsonSerializer.Serialize(new List<DailyTimeSlots>
		//          {
		//              new DailyTimeSlots
		//              {
		//                  DayName = "Test Day",
		//                  PickUpDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
		//                  Slots = new TimeSlotDetail { StartTime = "08:00", EndTime = "17:00" }
		//              }
		//          })
		//              });

		//              var imgId = MakeFixedId(99, 3, i);

		//              postImages.Add(new PostImages
		//              {
		//                  PostImageId = imgId,
		//                  PostId = postId,
		//                  ImageUrl = "https://picsum.photos/200",
		//                  AiDetectedLabelsJson = "[]"
		//              });
		//          }

		//          // ======================================================================
		//          // 4) ADD SMALL COLLECTION POINTS FOR TESTING TEAM ASSIGN (VERY IMPORTANT)
		//          // ======================================================================

		//          smallCollectionPoints.Add(new SmallCollectionPoints
		//          {
		//              Id = 1001,
		//              Name = "Fixed Small Point – Team 1 (A)",
		//              Address = "Q1 - Test Point A",
		//              Latitude = 10.8410,
		//              Longitude = 106.8310,
		//              Status = "active",
		//              City_Team_Id = 1,
		//              Created_At = DateTime.UtcNow.AddHours(7),
		//              Updated_At = DateTime.UtcNow.AddHours(7)
		//          });

		//          smallCollectionPoints.Add(new SmallCollectionPoints
		//          {
		//              Id = 1002,
		//              Name = "Fixed Small Point – Team 1 (B)",
		//              Address = "Q1 - Test Point B",
		//              Latitude = 10.8420,
		//              Longitude = 106.8320,
		//              Status = "active",
		//              City_Team_Id = 1,
		//              Created_At = DateTime.UtcNow.AddHours(7),
		//              Updated_At = DateTime.UtcNow.AddHours(7)
		//          });

		//          smallCollectionPoints.Add(new SmallCollectionPoints
		//          {
		//              Id = 2001,
		//              Name = "Fixed Small Point – Team 2 (A)",
		//              Address = "Q2 - Test Point A",
		//              Latitude = 10.8450,
		//              Longitude = 106.8350,
		//              Status = "active",
		//              City_Team_Id = 2,
		//              Created_At = DateTime.UtcNow.AddHours(7),
		//              Updated_At = DateTime.UtcNow.AddHours(7)
		//          });

		//          smallCollectionPoints.Add(new SmallCollectionPoints
		//          {
		//              Id = 2002,
		//              Name = "Fixed Small Point – Team 2 (B)",
		//              Address = "Q2 - Test Point B",
		//              Latitude = 10.8460,
		//              Longitude = 106.8360,
		//              Status = "active",
		//              City_Team_Id = 2,
		//              Created_At = DateTime.UtcNow.AddHours(7),
		//              Updated_At = DateTime.UtcNow.AddHours(7)
		//          });
		//      }

		//      public static void AddFullGroupingDemoData()
		//      {
		//          // -----------------------------
		//          // 1. NGÀY DEMO
		//          // -----------------------------
		//          var demoDate = new DateTime(_vnNow.Year, _vnNow.Month, 28);
		//          var dateOnly = DateOnly.FromDateTime(demoDate);

		//          // -----------------------------
		//          // 2. THÊM 1 SMALL COLLECTION POINT
		//          // -----------------------------
		//          var demoPointId = 5001;

		//          smallCollectionPoints.Add(new SmallCollectionPoints
		//          {
		//              Id = demoPointId,
		//              Name = "Demo Point – Full Grouping",
		//              Address = "Khu Demo – Vinhomes",
		//              Latitude = 10.84201,
		//              Longitude = 106.83201,
		//              Status = "active",
		//              City_Team_Id = 1,
		//              Created_At = _vnNow,
		//              Updated_At = _vnNow
		//          });

		//          // -----------------------------
		//          // 3. USER DEMO (6 user)
		//          // -----------------------------
		//          var demoUsers = new List<User>();
		//          for (int i = 1; i <= 6; i++)
		//          {
		//              var u = new User
		//              {
		//                  UserId = Guid.NewGuid(),
		//                  Name = $"Demo Group User {i}",
		//                  Email = $"guser{i}@demo.com",
		//                  Phone = $"09090{i}000",
		//                  Address = $"Demo Block A{i}",
		//                  Iat = 10.8420 + (i * 0.0003),
		//                  Ing = 106.8320 + (i * 0.0003),
		//                  Role = "User"
		//              };
		//              demoUsers.Add(u);
		//          }
		//          users.AddRange(demoUsers);

		//          // -----------------------------
		//          // 4. PRODUCT + POST (6 bài pending)
		//          // -----------------------------
		//          var demoPostIds = new List<Guid>();

		//          for (int i = 0; i < 6; i++)
		//          {
		//              var prodId = Guid.NewGuid();
		//              var postId = Guid.NewGuid();
		//              demoPostIds.Add(postId);

		//              products.Add(new Products
		//              {
		//                  Id = prodId,
		//                  CategoryId = cat_Laptop,
		//                  BrandId = brand_Acer_Laptop,
		//                  SizeTierId = st_Laptop_MongNhe,
		//                  Status = "Chờ gom nhóm",
		//                  Description = $"Demo Laptop {i + 1}"
		//              });

		//              posts.Add(new Post
		//              {
		//                  Id = postId,
		//                  ProductId = prodId,
		//                  SenderId = demoUsers[i].UserId,
		//                  Name = $"Demo Post {i + 1}",
		//                  Address = demoUsers[i].Address,
		//                  Date = demoDate,
		//                  Status = "Đã duyệt",
		//                  EstimatePoint = 60 + i * 5,
		//                  ScheduleJson = CreateScheduleJson(dateOnly, "08:00", "17:00")
		//              });

		//              postImages.Add(new PostImages
		//              {
		//                  PostImageId = Guid.NewGuid(),
		//                  PostId = postId,
		//                  ImageUrl = "https://picsum.photos/200",
		//                  AiDetectedLabelsJson = "[]"
		//              });
		//          }

		//          // -----------------------------
		//          // 5. SHIFT
		//          // -----------------------------
		//          var shiftId = shifts.Count + 1;

		//          shifts.Add(new Shifts
		//          {
		//              Id = shiftId,
		//              CollectorId = collector_Dung_Id,
		//              Vehicle_Id = 1,
		//              WorkDate = dateOnly,
		//              Shift_Start_Time = demoDate.Date.AddHours(8),
		//              Shift_End_Time = demoDate.Date.AddHours(17)
		//          });

		//          // -----------------------------
		//          // 6. GROUP - bạn sẽ tạo qua API
		//          // -----------------------------
		//          // Không tạo ở đây, API sẽ tự tạo
		//          // Nhưng chuẩn bị route sau tạo group

		//          // -----------------------------
		//          // 7. STAGING ASSIGN DAY (cho API /assign)
		//          // -----------------------------
		//          stagingAssignDays.Add(new StagingAssignDay
		//          {
		//              Date = dateOnly,
		//              PointId = demoPointId,
		//              VehicleId = 1,
		//              PostIds = demoPostIds
		//          });

		//          // -----------------------------
		//          // 8. Tạo trước GROUP & ROUTES để test GET
		//          //    (nếu bạn muốn API tự tạo, có thể comment)
		//          // -----------------------------
		//          var fullGroupId = collectionGroups.Count + 1;

		//          collectionGroups.Add(new CollectionGroups
		//          {
		//              Id = fullGroupId,
		//              Shift_Id = shiftId,
		//              Group_Code = "DEMO-GROUP-FULL",
		//              Name = "Demo Full Grouping 28",
		//              Created_At = _vnNow
		//          });

		//          for (int i = 0; i < demoPostIds.Count; i++)
		//          {
		//              collectionRoutes.Add(new CollectionRoutes
		//              {
		//                  CollectionRouteId = Guid.NewGuid(),
		//                  CollectionGroupId = fullGroupId,
		//                  PostId = demoPostIds[i],
		//                  CollectionDate = dateOnly,
		//                  EstimatedTime = new TimeOnly(8, 0).AddMinutes(i * 20),
		//                  Status = "Chưa bắt đầu"
		//              });
		//          }
		//      }

		// ==========================================================================
		// GROUPING TEST DATA – DÀNH RIÊNG CHO Service và Controller ở trên
		// Tất cả ID được cô lập để tránh xung đột hệ thống
		// ==========================================================================
		//public static void SeedGroupingServiceTestData()
		//{
		//    // Xóa mọi dataset test cũ
		//    smallCollectionPoints.RemoveAll(x => x.Id == 9001);
		//    vehicles.RemoveAll(x => x.Id == 91 || x.Id == 92);
		//    shifts.RemoveAll(x => x.Id >= 9000);
		//    stagingAssignDays.RemoveAll(x => x.PointId == 9001);

		//    posts.RemoveAll(p => p.Name.Contains("[GRP-TEST]"));
		//    products.RemoveAll(p => p.Description.Contains("[GRP-TEST]"));
		//    users.RemoveAll(u => u.Name.Contains("Grouping Test User"));

		//    // ==========================================================================
		//    // 1) TẠO SIZE TIER TEST ĐƠN GIẢN
		//    // ==========================================================================
		//    var testSizeTierId = Guid.NewGuid();
		//    sizeTiers.Add(new SizeTier
		//    {
		//        SizeTierId = testSizeTierId,
		//        CategoryId = cat_Laptop,
		//        Name = "Test Size",
		//        EstimatedWeight = 20,
		//        EstimatedVolume = 0.4
		//    });

		//    // ==========================================================================
		//    // 2) SMALL COLLECTION POINT CHO TEST
		//    // ==========================================================================
		//    smallCollectionPoints.Add(new SmallCollectionPoints
		//    {
		//        Id = 9001,
		//        Name = "Grouping Test Point",
		//        Address = "Test Street",
		//        Latitude = 10.84111,
		//        Longitude = 106.83111,
		//        City_Team_Id = 99,
		//        Status = "active",
		//        Created_At = _vnNow,
		//        Updated_At = _vnNow
		//    });

		//    // ==========================================================================
		//    // 3) VEHICLES TEST
		//    // ==========================================================================
		//    vehicles.Add(new Vehicles
		//    {
		//        Id = 91,
		//        Plate_Number = "TEST-91",
		//        Vehicle_Type = "Xe tải nhỏ",
		//        Capacity_Kg = 800,
		//        Capacity_M3 = 5,
		//        Radius_Km = 10,
		//        Status = "active",
		//        Small_Collection_Point = 9001
		//    });

		//    vehicles.Add(new Vehicles
		//    {
		//        Id = 92,
		//        Plate_Number = "TEST-92",
		//        Vehicle_Type = "Xe tải lớn",
		//        Capacity_Kg = 2000,
		//        Capacity_M3 = 12,
		//        Radius_Km = 15,
		//        Status = "active",
		//        Small_Collection_Point = 9001
		//    });

		//    // ==========================================================================
		//    // 4) USERS TEST
		//    // ==========================================================================
		//    List<User> testUsers = new();
		//    for (int i = 1; i <= 6; i++)
		//    {
		//        testUsers.Add(new User
		//        {
		//            UserId = Guid.NewGuid(),
		//            Name = $"Grouping Test User {i}",
		//            Email = $"gtu{i}@test.com",
		//            Phone = "0900",
		//            Address = $"Test Address {i}",
		//            Iat = 10.841000 + (i * 0.0002),
		//            Ing = 106.831000 + (i * 0.0002),
		//            Role = "User"
		//        });
		//    }
		//    users.AddRange(testUsers);

		//    // ==========================================================================
		//    // 5) PRODUCTS + POSTS TEST (6 posts)
		//    // ==========================================================================
		//    List<Guid> postIds = new();

		//    for (int i = 1; i <= 6; i++)
		//    {
		//        var prodId = Guid.NewGuid();
		//        var postId = Guid.NewGuid();
		//        postIds.Add(postId);

		//        products.Add(new Products
		//        {
		//            Id = prodId,
		//            CategoryId = cat_Laptop,
		//            BrandId = brand_Acer_Laptop,
		//            SizeTierId = testSizeTierId,
		//            Status = "Chờ gom nhóm",
		//            Description = $"[GRP-TEST] Product {i}"
		//        });

		//        posts.Add(new Post
		//        {
		//            Id = postId,
		//            ProductId = prodId,
		//            SenderId = testUsers[i - 1].UserId,
		//            Name = $"[GRP-TEST] Post {i}",
		//            Address = testUsers[i - 1].Address,
		//            Date = _vnNow.Date,
		//            Status = "Đã duyệt",
		//            EstimatePoint = 60 + i,
		//            ScheduleJson = JsonSerializer.Serialize(new List<DailyTimeSlots>
		//    {
		//        new DailyTimeSlots
		//        {
		//            DayName = "TestDay",
		//            PickUpDate = DateOnly.FromDateTime(_vnNow),
		//            Slots = new TimeSlotDetail
		//            {
		//                StartTime = "08:00",
		//                EndTime = "17:00"
		//            }
		//        }
		//    })
		//        });
		//    }

		//    // ==========================================================================
		//    // 6) SHIFT TEST
		//    // ==========================================================================
		//    shifts.Add(new Shifts
		//    {
		//        Id = 9000,
		//        CollectorId = collector_Dung_Id,
		//        Vehicle_Id = 91,
		//        WorkDate = DateOnly.FromDateTime(_vnNow),
		//        Shift_Start_Time = _vnNow.Date.AddHours(8),
		//        Shift_End_Time = _vnNow.Date.AddHours(17)
		//    });

        //    // ==========================================================================
        //    // 7) STAGING ASSIGN (để test auto-group)
        //    // ==========================================================================
        //    stagingAssignDays.Add(new StagingAssignDay
        //    {
        //        Date = DateOnly.FromDateTime(_vnNow),
        //        PointId = 9001,
        //        VehicleId = 91,
        //        PostIds = postIds
        //    });
        //}

       

        public static void AddPostsForDay27()
        {
            // ============================================================
            // 🔥 LẤY NGÀY 27 TRONG THÁNG HIỆN TẠI
            // ============================================================
            var day27DateTime = _vnNow.AddDays(27 - _vnNow.Day);
            var day27 = DateOnly.FromDateTime(day27DateTime);

			// ============================================================
			// A) USERS MỚI
			// ============================================================
			var extraUsers = new List<User>
{
	new User {
		UserId = Guid.NewGuid(), Name = "User Test 01", Email = "u01@test.com", Phone = "0901111111",
		Avatar = "https://picsum.photos/id/31/200/200",
		Role = "User"
	},
	new User {
		UserId = Guid.NewGuid(), Name = "User Test 02", Email = "u02@test.com", Phone = "0902222222",
		Avatar = "https://picsum.photos/id/32/200/200",
		Role = "User"
	},
	new User {
		UserId = Guid.NewGuid(), Name = "User Test 03", Email = "u03@test.com", Phone = "0903333333",
		Avatar = "https://picsum.photos/id/33/200/200",
		Role = "User"
	},
	new User {
		UserId = Guid.NewGuid(), Name = "User Test 04", Email = "u04@test.com", Phone = "0904444444",
		Avatar = "https://picsum.photos/id/34/200/200",
		Role = "User"
	}
};


			users.AddRange(extraUsers);

			var u1 = extraUsers[0].UserId;
			var u2 = extraUsers[1].UserId;
			var u3 = extraUsers[2].UserId;
			var u4 = extraUsers[3].UserId;
			var extraUserAddress = new List<UserAddress>
			{
				new UserAddress
				{
					UserAddressId = Guid.NewGuid(),
					UserId = u1,
					Address = "Park 1 – Vinhomes GP",
					Iat = 10.842500,
					Ing = 106.831500,
					isDefault = true
				},
				new UserAddress
				{
					UserAddressId = Guid.NewGuid(),
					UserId = u2,
					Address = "Park 2 – Vinhomes GP",
					Iat = 10.843200,
					Ing = 106.832200,
					isDefault = true
				},
				new UserAddress
				{
					UserAddressId = Guid.NewGuid(),
					UserId = u3,
					Address = "Park 3 – Vinhomes GP",
					Iat = 10.842900,
					Ing = 106.833000,
					isDefault = true
				},
				new UserAddress
				{
					UserAddressId = Guid.NewGuid(),
					UserId = u4,
					Address = "Park 5 – Vinhomes GP",
					Iat = 10.843600,
					Ing = 106.833400,
					isDefault = true
				}
			};
			userAddress.AddRange(extraUserAddress);

			// ============================================================
			// B) ATTRIBUTE MASTER (KHÔNG TRÙNG ID)
			// ============================================================
			var att_length = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000001");
            var att_width = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002");
            var att_height = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003");
            var att_weight = Guid.Parse("a1a1a1a1-0009-0009-0009-000000000001");
            var att_volume = Guid.Parse("a1a1a1a1-0004-0004-0004-000000000001");

            void EnsureAttribute(Guid id, string name)
            {
                if (!attributes.Any(a => a.Id == id))
                    attributes.Add(new Attributes { Id = id, Name = name });
            }

            EnsureAttribute(att_length, "length");
            EnsureAttribute(att_width, "width");
            EnsureAttribute(att_height, "height");
            EnsureAttribute(att_weight, "weight");
            EnsureAttribute(att_volume, "volume");

            // ============================================================
            // C) PRODUCTS
            // ============================================================
            var prodA = Guid.NewGuid();
            var prodB = Guid.NewGuid();
            var prodC = Guid.NewGuid();
            var prodD = Guid.NewGuid();

            products.AddRange(new List<Products>
    {
        new Products { Id = prodA, CategoryId = cat_LoViSong, BrandId = brand_Sharp_LoViSong,
            Status = "Chờ gom nhóm", Description = "Lò vi sóng hỏng" },

        new Products { Id = prodB, CategoryId = cat_DienThoai, BrandId = brand_Apple_DienThoai,
            Status = "Chờ gom nhóm", Description = "Điện thoại vỡ" },

        new Products { Id = prodC, CategoryId = cat_QuatDien, BrandId = brand_Asia_Quat,
            Status = "Chờ gom nhóm", Description = "Quạt không quay" },

        new Products { Id = prodD, CategoryId = cat_MayHutBui, BrandId = brand_Dyson,
            Status = "Chờ gom nhóm", Description = "Máy hút bụi yếu" }
    });

            // ============================================================
            // D) PRODUCT VALUES
            // ============================================================
            productValues.AddRange(new List<ProductValues>
    {
        // PROD A
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodA, AttributeId = att_length, Value = 50 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodA, AttributeId = att_width,  Value = 30 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodA, AttributeId = att_height, Value = 25 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodA, AttributeId = att_weight, Value = 12 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodA, AttributeId = att_volume, Value = 0.0375 },

        // PROD B
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodB, AttributeId = att_length, Value = 15 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodB, AttributeId = att_width,  Value = 7 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodB, AttributeId = att_height, Value = 1 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodB, AttributeId = att_weight, Value = 0.3 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodB, AttributeId = att_volume, Value = 0.000105 },

        // PROD C
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodC, AttributeId = att_length, Value = 40 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodC, AttributeId = att_width,  Value = 40 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodC, AttributeId = att_height, Value = 120 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodC, AttributeId = att_weight, Value = 5 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodC, AttributeId = att_volume, Value = 0.192 },

        // PROD D
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodD, AttributeId = att_length, Value = 30 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodD, AttributeId = att_width,  Value = 25 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodD, AttributeId = att_height, Value = 25 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodD, AttributeId = att_weight, Value = 6 },
        new ProductValues { ProductValuesId = Guid.NewGuid(), ProductId = prodD, AttributeId = att_volume, Value = 0.01875 }
    });

            // ============================================================
            // E) POSTS NGÀY 27
            // ============================================================
            var postA = Guid.NewGuid();
            var postB = Guid.NewGuid();
            var postC = Guid.NewGuid();
            var postD = Guid.NewGuid();

            posts.AddRange(new List<Post>
    {
        new Post {
            Id = postA, ProductId = prodA, SenderId = u1,
            Name = "Lò vi sóng – thu gom ngày 27",
            Date = day27DateTime, Status = "Đã duyệt",
            ScheduleJson = CreateScheduleJson(day27, "17:00", "18:00"),
            Address = extraUserAddress[0].Address, EstimatePoint = 100
        },

        new Post {
            Id = postB, ProductId = prodB, SenderId = u2,
            Name = "Điện thoại – thu gom ngày 27",
            Date = day27DateTime, Status = "Đã duyệt",
            ScheduleJson = CreateScheduleJson(day27, "18:00", "20:00"),
            Address = extraUserAddress[1].Address, EstimatePoint = 120
        },

        new Post {
            Id = postC, ProductId = prodC, SenderId = u3,
            Name = "Quạt điện – thu gom ngày 27",
            Date = day27DateTime, Status = "Đã duyệt",
            ScheduleJson = CreateScheduleJson(day27, "18:00", "21:00"),
            Address = extraUserAddress[2].Address, EstimatePoint = 90
        },

        new Post {
            Id = postD, ProductId = prodD, SenderId = u4,
            Name = "Máy hút bụi – thu gom ngày 27",
            Date = day27DateTime, Status = "Đã duyệt",
            ScheduleJson = CreateScheduleJson(day27, "19:00", "20:00"),
            Address = extraUserAddress[3].Address, EstimatePoint = 140
        }
    });


			// ============================================================
			// F) IMAGE
			// ============================================================
			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodA, ImageUrl = "https://picsum.photos/id/41/200/200", AiDetectedLabelsJson = "[]" });
			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodB, ImageUrl = "https://picsum.photos/id/42/200/200", AiDetectedLabelsJson = "[]" });
			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodC, ImageUrl = "https://picsum.photos/id/43/200/200", AiDetectedLabelsJson = "[]" });
			productImages.Add(new ProductImages { ProductImagesId = Guid.NewGuid(), ProductId = prodD, ImageUrl = "https://picsum.photos/id/44/200/200", AiDetectedLabelsJson = "[]" });

			// ============================================================
			// G) SHIFT NGÀY 27
			// ============================================================
			var shiftId1 = shifts.Count + 1;
            var shiftId2 = shifts.Count + 2;

            shifts.Add(new Shifts
            {
                Id = shiftId1,
                CollectorId = collector_Dung_Id,
                Vehicle_Id = 1,
                WorkDate = day27,
                Shift_Start_Time = day27DateTime.Date.AddHours(16),
                Shift_End_Time = day27DateTime.Date.AddHours(22)
            });

            shifts.Add(new Shifts
            {
                Id = shiftId2,
                CollectorId = collector_Tuan_Id,
                Vehicle_Id = 2,
                WorkDate = day27,
                Shift_Start_Time = day27DateTime.Date.AddHours(16),
                Shift_End_Time = day27DateTime.Date.AddHours(22)
            });

            // ============================================================
            // H) GROUP NGÀY 27
            // ============================================================
            collectionGroups.Add(new CollectionGroups
            {
                Id = collectionGroups.Count + 1,
                Shift_Id = shiftId1,
                Group_Code = $"DAY27-S1-DUNG",
                Name = "Tuyến Ngày 27 – Dũng",
                Created_At = _vnNow
            });

            collectionGroups.Add(new CollectionGroups
            {
                Id = collectionGroups.Count + 2,
                Shift_Id = shiftId2,
                Group_Code = $"DAY27-S2-TUAN",
                Name = "Tuyến Ngày 27 – Tuấn",
                Created_At = _vnNow
            });
        }


    }
}
