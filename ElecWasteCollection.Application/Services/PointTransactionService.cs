using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class PointTransactionService : IPointTransactionService
	{
		private readonly List<PointTransactions> pointTransactions = FakeDataSeeder.points;
		private readonly IUserPointService _userPointService;
		private readonly List<Post> _post = FakeDataSeeder.posts;
		//private readonly List<PostImages> postImages = FakeDataSeeder.postImages;
		private readonly List<ProductImages> productImages = FakeDataSeeder.productImages;	
		private readonly List<Products> products = FakeDataSeeder.products;

		public PointTransactionService(IUserPointService userPointService)
		{
			_userPointService = userPointService;
		}

		public List<PointTransactionModel> GetAllPointHistoryByUserId(Guid id)
		{
			var result = pointTransactions // Sử dụng biến _pointTransactions từ FakeData
				.Where(x => x.UserId == id)
				.Select(pt =>
				{
					// --- LOGIC LẤY ẢNH ---
					List<string> images = new List<string>();

					// 1. Ưu tiên lấy ảnh từ Post (ảnh hiện trường/thực tế)
					//if (pt.PostId.HasValue)
					//{
					//	images = postImages
					//		.Where(pi => pi.PostId == pt.PostId)
					//		.Select(pi => pi.ImageUrl)
					//		.ToList();
					//}

					// 2. Nếu chưa có ảnh từ Post, thử lấy ảnh từ Product (ảnh danh mục)

						images = productImages
							.Where(pi => pi.ProductId == pt.ProductId)
							.Select(pi => pi.ImageUrl)
							.ToList();
					

					// --- MAPPING MODEL ---
					return new PointTransactionModel
					{
						PointTransactionId = pt.PointTransactionId,
						ProductId = pt.ProductId,
						UserId = pt.UserId,
						Desciption = pt.Desciption,
						TransactionType = pt.TransactionType,
						Point = pt.Point,
						CreatedAt = pt.CreatedAt,

						// Gán danh sách ảnh tìm được
						Images = images ?? new List<string>()
					};
				})
				.OrderByDescending(pt => pt.CreatedAt) // Nên sắp xếp mới nhất lên đầu
				.ToList();

			return result;
		}

		public Guid ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel)
		{
			var points = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				ProductId = createPointTransactionModel.ProductId,
				UserId = createPointTransactionModel.UserId,
				Desciption = createPointTransactionModel.Desciption,
				Point = createPointTransactionModel.Point,
				CreatedAt = DateTime.UtcNow,
				TransactionType = PointTransactionType.Earned.ToString()
			};
			var userPoint = _userPointService.GetPointByUserId(createPointTransactionModel.UserId);
			pointTransactions.Add(points);
			var result =  _userPointService.UpdatePointForUser(createPointTransactionModel.UserId, points.Point);
			return points.PointTransactionId;
		}
	}
}
