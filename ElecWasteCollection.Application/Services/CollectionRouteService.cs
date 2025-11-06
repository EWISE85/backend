using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
    public class CollectionRouteService : ICollectionRouteService
    {
        private static List<CollectionRoutes> routes = FakeDataSeeder.collectionRoutes;
        private readonly ICollectorService _collectorService;
        private readonly IPostService _postService;
        private readonly IUserService _userService;
		private readonly IProductService _productService;

		public CollectionRouteService(ICollectorService collectorService, IPostService postService, IUserService userService, IProductService productService)
        {
			_collectorService = collectorService;
			_postService = postService;
			_userService = userService;
			_productService = productService;
		}
        public bool CancelCollection(Guid collectionRouteId, string rejectMessage)
        {
            var route = routes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
            if (route != null)
            {
                route.Status = "Hủy bỏ";
                route.RejectMessage = rejectMessage;
                return true;
            }
            return false;
        }

        public  bool ConfirmCollection(Guid collectionRouteId, List<string> confirmImages, string QRCode)
        {
            var route = routes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			if (route == null)
			{
				return false;
			}
			var post = _postService.GetById(route.PostId);
			if (post == null) return false;
			var product = _productService.GetById(post.Product.ProductId);
			if (product == null) return false;
			
            route.Status = "Hoàn thành";
            route.ConfirmImages = confirmImages;
            route.Actual_Time = TimeOnly.FromDateTime(DateTime.Now);
			product.QRCode = QRCode;
			product.Status = "Đã thu gom";
			return true;

        }

		public List<CollectionRouteModel> GetAllRoutes(DateOnly PickUpDate)
		{
			return routes
				.Where(r => r.CollectionDate == PickUpDate)

				// 1. Chỉ Select để lấy dữ liệu (ghép Route với Post)
				.Select(r => new {
					Route = r,
					Post = _postService.GetById(r.PostId) // Chỉ gọi GetById 1 LẦN
				})

				// 2. Lọc bỏ những route có post bị null (post không tìm thấy)
				.Where(x => x.Post != null)

				// 3. Bây giờ mới Select để tạo Model (post ở đây 100% không null)
				.Select(x =>
				{
					var r = x.Route;
					var post = x.Post; // Đã đảm bảo không null

					var collector = _collectorService.GetById(r.CollectorId);
					var sender = (post.Sender != null)
								 ? _userService.GetById(post.Sender.UserId)
								 : null;

					var pickUpImages = post.ImageUrls;

					return new CollectionRouteModel
					{
						CollectionRouteId = r.CollectionRouteId,
						PostId = r.PostId,
						Collector = collector,
						Sender = sender,
						ItemName = post.Name,
						CollectionDate = r.CollectionDate,
						EstimatedTime = r.EstimatedTime,
						Actual_Time = r.Actual_Time.HasValue ? r.Actual_Time.Value : null,
						ConfirmImages = r.ConfirmImages,
						LicensePlate = r.LicensePlate,
						Address = post.Address,
						PickUpItemImages = pickUpImages,
						Status = r.Status
					};
				})
				.OrderBy(r => r.EstimatedTime)
				.ToList();
		}

		public CollectionRouteModel GetRouteById(Guid collectionRoute)
        {
            var route = routes.FirstOrDefault(r => r.CollectionRouteId == collectionRoute);
            if (route != null)
            {
				var model = new CollectionRouteModel
				{
					CollectionRouteId = route.CollectionRouteId,
					PostId = route.PostId,
					Collector = _collectorService.GetById(route.CollectorId),
					Sender = _userService.GetById(_postService.GetById(route.PostId).Sender.UserId),
					ItemName = _postService.GetById(route.PostId).Name,
					CollectionDate = route.CollectionDate,
					EstimatedTime = route.EstimatedTime,
					Actual_Time = route.Actual_Time.HasValue ? route.Actual_Time.Value : null,
					ConfirmImages = route.ConfirmImages,
					LicensePlate = route.LicensePlate,
					Address = _postService.GetById(route.PostId).Address,
					PickUpItemImages = _postService.GetById(route.PostId).ImageUrls,
					Status = route.Status

				};
				return model;
			}
			return null;
		}

		public bool IsUserConfirm(Guid collectionRouteId, bool isConfirm)
		{
			var route = routes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			if (route != null)
			{
				if (isConfirm)
				{
					route.Status = "Người dùng đã xác nhận";
					return true;
				}
				else
				{
					route.Status = "Người dùng không xác nhận";
					return true;
				}
				

			}
			return false;
		}
	}
}
