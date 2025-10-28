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
	public static  class FakeDataSeeder
	{
		public static List<User> users = new()
	{
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
		private static string CreateSchedule(int daysFromNow, string start, string end)
		{
			var schedule = new List<DailyTimeSlots>
			{
				new DailyTimeSlots
				{
					DayName = $"Thứ {((int)DateTime.Now.AddDays(daysFromNow).DayOfWeek == 0 ? 8 : (int)DateTime.Now.AddDays(daysFromNow).DayOfWeek + 1)}",
					PickUpDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysFromNow)),
					Slots = new List<TimeSlotDetail> { new TimeSlotDetail { StartTime = start, EndTime = end } }
				}
			};
			return JsonSerializer.Serialize(schedule);
		}
		public static List<Post> posts = new()
		{
			new Post
			{
				Id = Guid.Parse("a2d7b801-b0fb-4f7d-9b83-b741d23666a1"),
				SenderId = users[0].UserId,
				Name = "Thu gom tivi cũ",
				Category = "Điện tử Tiêu dùng",
				Description = "Tivi Samsung 42 inch hỏng màn hình, cần thu gom.",
				Date = DateTime.Now,
				Address = users[0].Address,
				ScheduleJson = CreateSchedule(2, "08:00", "09:00"),
				Images = new List<string>{"https://tse4.mm.bing.net/th/id/OIP.LuRXEsdA9472ZA06zqLEswHaHa?pid=Api&P=0&h=180"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("b34c1223-7545-41d2-9e42-67d75e3c2a31"),
				SenderId = users[1].UserId,
				Name = "Máy giặt hỏng cần thu gom",
				Category = "Điện tử Gia dụng",
				Description = "Máy giặt Toshiba không hoạt động nữa, mong được hỗ trợ thu gom.",
				Date = DateTime.Now.AddDays(-5),
				Address = users[1].Address,
				ScheduleJson = CreateSchedule(2, "09:00", "10:00"),
				Images = new List<string>{"https://tse1.mm.bing.net/th/id/OIP.nqDpXYFDMJ4J3SHRuHJfCAHaF7?pid=Api&P=0&h=180"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("c1b63fa1-ec52-44a0-8a9c-8b83f8d1b8c3"),
				SenderId = users[2].UserId,
				Name = "Máy tính cũ không dùng nữa",
				Category = "Điện tử Tiêu dùng",
				Description = "CPU Intel i3 đời cũ, màn hình Dell 19 inch.",
				Date = DateTime.Now.AddDays(-2),
				Address = users[2].Address,
				ScheduleJson = CreateSchedule(2, "10:00", "11:00"),
				Images = new List<string>{"https://mccvietnam.vn/media/lib/26-09-2022/b-pc-mcc-1920x1080.png"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("d9a86de5-7d27-43d0-9f55-49094b30947d"),
				SenderId = users[3].UserId,
				Name = "Tủ lạnh hỏng cần xử lý",
				Category = "Điện tử Gia dụng",
				Description = "Tủ lạnh Panasonic không còn làm lạnh, muốn xử lý.",
				Date = DateTime.Now.AddDays(-8),
				Address = users[3].Address,
				ScheduleJson = CreateSchedule(2, "11:00", "12:00"),
				Images = new List<string>{"https://picsum.photos/id/203/400/300"},
				Status = "Rejected",
				RejectMessage = "Hình ảnh không rõ ràng."
			},
			new Post
			{
				Id = Guid.Parse("e0f92a77-188b-402b-a0ea-3b1c68891ac0"),
				SenderId = users[4].UserId,
				Name = "Laptop bị vỡ màn hình",
				Category = "Điện tử Tiêu dùng",
				Description = "Laptop Acer bị vỡ màn hình, cần thu gom cái cũ.",
				Date = DateTime.Now.AddDays(-1),
				Address = users[4].Address,
				ScheduleJson = CreateSchedule(2, "13:00", "14:00"),
				Images = new List<string>{"https://vinhphatstore.vn/wp-content/uploads/2022/09/cach-sua-man-hinh-laptop-bi-vo-hieu-qua-triet-de-3-1.jpg"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("f2c3cc25-f7d7-4b0a-bd1c-69a2dfb6b211"),
				SenderId = users[0].UserId,
				Name = "Điện thoại cũ bị chai pin",
				Category = "Điện tử Tiêu dùng",
				Description = "iPhone 7 bị chai pin, muốn gửi thu gom.",
				Date = DateTime.Now.AddDays(-4),
				Address = users[0].Address,
				ScheduleJson = CreateSchedule(2, "14:00", "15:00"),
				Images = new List<string>{"https://png.pngtree.com/background/20211217/original/pngtree-mobile-phone-photography-picture-image_1580997.jpg"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("a82d6f7b-f1e7-45dc-83ec-7b3e2db21a4f"),
				SenderId = users[1].UserId,
				Name = "Loa Bluetooth bị hỏng",
				Category = "Điện tử Tiêu dùng",
				Description = "Loa JBL mini không sạc được.",
				Date = DateTime.Now.AddDays(-6),
				Address = users[1].Address,
				ScheduleJson = CreateSchedule(2, "15:00", "16:00"),
				Images = new List<string>{"https://tse1.mm.bing.net/th/id/OIP.h0WESAKXTusQdzs5QSsLVAHaHa?pid=Api&P=0&h=180"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("b0b8c58b-4921-4e7d-9b09-0840f994e98e"),
				SenderId = users[2].UserId,
				Name = "Bình nước nóng hỏng",
				Category = "Điện tử Gia dụng",
				Description = "Bình Ariston bị rò điện, cần xử lý an toàn.",
				Date = DateTime.Now.AddDays(-9),
				Address = users[2].Address,
				ScheduleJson = CreateSchedule(2, "16:00", "17:00"),
				Images = new List<string>{"https://media.eproshop.vn/file/Ggw3EQpfr"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("c9955eab-20a8-463f-b6db-4d20382195c3"),
				SenderId = users[3].UserId,
				Name = "Máy in văn phòng cũ",
				Category = "Điện tử Tiêu dùng",
				Description = "Máy in HP cũ, không còn dùng.",
				Date = DateTime.Now.AddDays(-7),
				Address = users[3].Address,
				ScheduleJson = CreateSchedule(2, "17:00", "18:00"),
				Images = new List<string>{"https://cdn.tgdd.vn/Files/2019/01/24/1146335/may-in-da-nang-la-gi.jpg"},
				Status = "Đã Duyệt"
			},
			new Post
			{
				Id = Guid.Parse("e62aefc7-0e61-4b35-9d59-6b8e10d2b01e"),
				SenderId = users[4].UserId,
				Name = "Quạt điện hỏng cánh",
				Category = "Điện tử Gia dụng",
				Description = "Quạt Asia cũ, gãy cánh, muốn thu gom.",
				Date = DateTime.Now.AddDays(-10),
				Address = users[4].Address,
				ScheduleJson = CreateSchedule(2, "18:00", "19:00"),
				Images = new List<string>{"https://meta.vn/Data/image/2020/07/01/quat-dung-dien-co-91-qd-cn450p5.jpg"},
				Status = "Đã Duyệt"
			}
		};
		public static Collector collector = new()
		{
			CollectorId = Guid.Parse("6df4af85-6a59-4a0a-8513-1d7859fbd789"),
			Name = "Ngô Văn Dũng",
			Email = "ngo.van.dung@ewc.vn",
			Phone = "0905999888",
			Avatar = "https://picsum.photos/id/1062/200/200"
		};
		public static List<CollectionRoutes> routes = new()
{
	// === 5 ngày hôm qua ===
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
		PostId = posts[3].Id,
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

	// === 5 ngày hôm nay ===
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("e1f2cde2-0e2a-4a8e-b5a0-60d34e8d3b95"),
		PostId = posts[5].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now),
		EstimatedTime = new TimeOnly(8, 0),
		Actual_Time = new TimeOnly(8, 10),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/306/400/300" },
		LicensePlate = "51A-12345",
		Status = "Đang tiến hành"
	},
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("e2f2cde2-0e2a-4a8e-b5a0-60d34e8d3b96"),
		PostId = posts[6].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now),
		EstimatedTime = new TimeOnly(9, 0),
		Actual_Time = new TimeOnly(9, 0),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/307/400/300" },
		LicensePlate = "51A-12345",
		Status = "Đang tiến hành"
	},
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("e3f2cde2-0e2a-4a8e-b5a0-60d34e8d3b97"),
		PostId = posts[7].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now),
		EstimatedTime = new TimeOnly(10, 0),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/308/400/300" },
		LicensePlate = "51A-12345",
		Status = "Đang tiến hành"
	},
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("e4f2cde2-0e2a-4a8e-b5a0-60d34e8d3b98"),
		PostId = posts[8].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now),
		EstimatedTime = new TimeOnly(11, 0),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/309/400/300" },
		LicensePlate = "51A-12345",
		Status = "Đang tiến hành"
	},
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("e5f2cde2-0e2a-4a8e-b5a0-60d34e8d3b99"),
		PostId = posts[9].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now),
		EstimatedTime = new TimeOnly(13, 0),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/310/400/300" },
		LicensePlate = "51A-12345",
		Status = "Đang tiến hành"
	},

	// === 5 ngày mai ===
	new CollectionRoutes
	{
		CollectionRouteId = Guid.Parse("f1f2cde2-0e2a-4a8e-b5a0-60d34e8d3ba0"),
		PostId = posts[0].Id,
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
		PostId = posts[1].Id,
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
		PostId = posts[2].Id,
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
		PostId = posts[3].Id,
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
		PostId = posts[4].Id,
		CollectorId = collector.CollectorId,
		CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
		EstimatedTime = new TimeOnly(13, 0),
		ConfirmImages = new List<string>{ "https://picsum.photos/id/315/400/300" },
		LicensePlate = "51A-12345",
		Status = "Chưa bắt đầu"
	}
};


	}
}
