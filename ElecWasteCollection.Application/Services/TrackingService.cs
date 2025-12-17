using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class TrackingService : ITrackingService
	{
		private readonly ITrackingRepository _trackingRepository;

		public TrackingService(ITrackingRepository trackingRepository)
		{
			_trackingRepository = trackingRepository;
		}

		public async Task<List<CollectionTimelineModel>> GetFullTimelineByProductIdAsync(Guid productId)
		{
			var timeline = await _trackingRepository.GetsAsync(h => h.ProductId == productId);
			if (timeline == null || !timeline.Any())
			{
				return new List<CollectionTimelineModel>();
			}
			var response = timeline.Select(h => new CollectionTimelineModel
			{
				Status = h.Status,
				Description = h.StatusDescription,
				Date = h.ChangedAt.ToString("dd/MM/yyyy"), // Format
				Time = h.ChangedAt.ToString("HH:mm")
			}).ToList();

			return response;
		}

		
	}
}