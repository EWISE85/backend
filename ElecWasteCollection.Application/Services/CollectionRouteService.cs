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
        private static List<CollectionRoutes> routes = FakeDataSeeder.routes;
        private readonly ICollectorService _collectorService;
        private readonly IPostService _postService;
        private readonly IUserService _userService;
		public CollectionRouteService(ICollectorService collectorService, IPostService postService, IUserService userService)
        {
			_collectorService = collectorService;
			_postService = postService;
			_userService = userService;
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

        public bool ConfirmCollection(Guid collectionRouteId, List<string> confirmImages)
        {
            var route = routes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
            if (route != null)
            {
                route.Status = "Hoàn thành";
                route.ConfirmImages = confirmImages;
                route.Actual_Time = TimeOnly.FromDateTime(DateTime.Now);
				return true;
            }
            return false;
        }

        public List<CollectionRouteModel> GetAllRoutes(DateOnly PickUpDate)
        {
            return routes
                .Where(r => r.CollectionDate == PickUpDate)
                .Select(r => new CollectionRouteModel
                {
                    CollectionRouteId = r.CollectionRouteId,
                    PostId = r.PostId,
                    Collector = _collectorService.GetById(r.CollectorId),
					Sender = _userService.GetById(_postService.GetById(r.PostId).Sender.UserId),
					ItemName = _postService.GetById(r.PostId).Name,
					CollectionDate = r.CollectionDate,
                    EstimatedTime = r.EstimatedTime,
                    Actual_Time = r.Actual_Time,
                    ConfirmImages = r.ConfirmImages,
                    LicensePlate = r.LicensePlate,
					Address = _postService.GetById(r.PostId).Address,
                    PickUpItemImages = _postService.GetById(r.PostId).Images,
					Status = r.Status
                }).OrderBy(r => r.EstimatedTime)
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
                    Actual_Time = route.Actual_Time,
                    ConfirmImages = route.ConfirmImages,
                    LicensePlate = route.LicensePlate,
                    Address = _postService.GetById(route.PostId).Address,
					PickUpItemImages = _postService.GetById(route.PostId).Images,
					Status = route.Status

                };
				return model;
			}
			return null;
		}
    }
}
