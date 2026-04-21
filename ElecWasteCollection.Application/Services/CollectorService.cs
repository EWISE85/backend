using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.CollectorStatistic;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
	public class CollectorService : ICollectorService
	{
		private readonly ICollectorRepository _collectorRepository;
		private readonly IAccountRepsitory _accountRepsitory;
		private readonly IUnitOfWork _unitOfWork;
		public CollectorService(ICollectorRepository collectorRepository, IAccountRepsitory accountRepsitory, IUnitOfWork unitOfWork)
		{
			_collectorRepository = collectorRepository;
			_accountRepsitory = accountRepsitory;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> AddNewCollector(User collector)
		{
			await _unitOfWork.Users.AddAsync(collector);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateCollectorAsync(User collector, string collectorUsername, string password)
		{
			var result = new ImportResult();
			var existingCollector = await _collectorRepository.GetAsync(c => c.UserId == collector.UserId);
			if (existingCollector != null)
			{
				await UpdateCollector(collector);
				result.Messages.Add($"Đã cập nhật thông tin thu gom viên '{collector.Name}'.");
				result.IsNew = false;
			}
			else
			{
				collector.UserId = Guid.NewGuid();
				await AddNewCollector(collector);
				var account = new Account
				{
					Username = collectorUsername,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
					UserId = collector.UserId,
					IsFirstLogin = true
				};
				await _unitOfWork.Accounts.AddAsync(account);
				result.Messages.Add($"Thêm thu gom viên '{collector.Name}' thành công.");
				result.IsNew = true;
				await _unitOfWork.SaveAsync();
			}
			return result;
		}

		public async Task<bool> DeleteCollector(Guid collectorId)
		{
			var collector = await _collectorRepository.GetAsync(c => c.UserId == collectorId);
			if (collector == null) throw new AppException("Không tìm thấy người thu gom", 404);
			collector.Status = UserStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.Users.Update(collector);
			await _unitOfWork.SaveAsync();
			return true;

		}

		public async Task<List<CollectorResponse>> GetAll()
		{
			var collectors = await _collectorRepository.GetAllAsync(c => c.Role == UserRole.Collector.ToString());
			var response = collectors.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.CollectionUnitId
			}).ToList();
			return response;

		}

		public async Task<CollectorResponse> GetById(Guid id)
		{
			var collector = await _collectorRepository.GetAsync(c => c.UserId == id, "CollectionUnits");
			if (collector == null) throw new AppException("Không tìm thấy người thu gom", 404);

			var response = new CollectorResponse
			{
				CollectorId = collector.UserId,
				Name = collector.Name,
				Email = collector.Email,
				Phone = collector.Phone,
				Avatar = collector.Avatar,
				SmallCollectionPointId = collector.CollectionUnitId,
				SmallCollectionPointName = collector.CollectionUnits != null ? collector.CollectionUnits.Name : null
			};

			return response;
		}

		public async Task<PagedResult<CollectorResponse>> GetCollectorsByCompanyIdPagedAsync(
		 string companyId,
		 int page,
		 int limit)
		{
			var (collectors, totalCount) =
				await _collectorRepository.GetPagedCollectorsAsync(
					status: null,
					companyId: companyId,
					smallCollectionPointId: null,
					page: page,
					limit: limit);

			var resultItems = collectors.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.CollectionUnitId,
				SmallCollectionPointName = c.CollectionUnitId != null ? c.CollectionUnits.Name : null
			}).ToList();

			return new PagedResult<CollectorResponse>
			{
				Data = resultItems,
				TotalItems = totalCount,
				Page = page,
				Limit = limit
			};
		}

		public async Task<PagedResultModel<CollectorResponse>> GetCollectorByWareHouseId(string wareHouseId, int page, int limit, string? status)
		{
			string statusEnumValue = null;
            if (!string.IsNullOrEmpty(status))
			{
				statusEnumValue = StatusEnumHelper.GetValueFromDescription<UserStatus>(status).ToString();
			}
            var (collectores, totalItems) = await _collectorRepository.GetPagedCollectorsAsync(
				status: statusEnumValue,
				companyId: null,
				smallCollectionPointId: wareHouseId,
				page: page,
				limit: limit
			);
			var response = collectores.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.CollectionUnitId,
				SmallCollectionPointName = c.CollectionUnits != null ? c.CollectionUnits.Name : null
			}).ToList();
			return new PagedResultModel<CollectorResponse>(
				response,
				page,
				limit,
				totalItems
			);
		}

		public async Task<bool> UpdateCollector(User collector)
		{
			var collectorToUpdate = await _collectorRepository.GetAsync(c => c.CollectorCode == collector.CollectorCode);
			if (collectorToUpdate == null) throw new AppException("Không tìm thấy người thu gom", 404);
			var status = StatusEnumHelper.GetValueFromDescription<UserStatus>(collector.Status);
			collectorToUpdate.Name = collector.Name;
			collectorToUpdate.Email = collector.Email;
			collectorToUpdate.Phone = collector.Phone;
			collectorToUpdate.Avatar = collector.Avatar;
			collectorToUpdate.CompanyId = collector.CompanyId;
			collectorToUpdate.CollectionUnitId = collector.CollectionUnitId;
			collectorToUpdate.Status = status.ToString();
			_unitOfWork.Users.Update(collectorToUpdate);
			await _unitOfWork.SaveAsync();
			return true;

		}

		public async Task<PagedResultModel<CollectorResponse>> GetPagedCollectorsAsync(CollectorSearchModel model)
		{
			var (users, totalItems) = await _collectorRepository.GetPagedCollectorsAsync(
				status: model.Status,
				companyId: model.CompanyId,
				smallCollectionPointId: model.SmallCollectionId,
				page: model.Page,
				limit: model.Limit
			);

			var resultList = users.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.CollectionUnitId,
				SmallCollectionPointName = c.CollectionUnits != null ? c.CollectionUnits.Name : null
			}).ToList();

			return new PagedResultModel<CollectorResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}

		public async Task<CollectorStatisticsResponseModel> GetStatisticsAsync(CollectorStatisticModel request)
		{
			GetDateRange(request.TargetDate, request.Period, out DateOnly startDate, out DateOnly endDate);

			// 2. Lấy dữ liệu từ DB (Chỉ lấy các route trong khoảng thời gian đã tính)
			var routes = await _unitOfWork.CollecctionRoutes.GetsAsync(
	r => r.CollectionGroup.Shifts.CollectorId == request.CollectorId
		 && r.CollectionDate >= startDate
		 && r.CollectionDate <= endDate
);

			var response = new CollectorStatisticsResponseModel
			{
				TotalOrders = routes.Count,
				CompletedOrders = routes.Count(r => r.Status == CollectionRouteStatus.HOAN_THANH.ToString()),
				FailedOrders = routes.Count(r => r.Status == CollectionRouteStatus.THAT_BAI.ToString())
			};

			InitializeChartData(response, request.TargetDate, request.Period);

			foreach (var route in routes)
			{
				string label = GetLabelForDate(route.CollectionDate, request.Period);

				if (route.Status == CollectionRouteStatus.HOAN_THANH.ToString())
				{
					var item = response.CompletedChart.FirstOrDefault(x => x.Label == label);
					if (item != null) item.Value++;
				}
				else if (route.Status == CollectionRouteStatus.THAT_BAI.ToString())
				{
					var item = response.FailedChart.FirstOrDefault(x => x.Label == label);
					if (item != null) item.Value++;
				}
			}

			return response;
		}
		private void GetDateRange(DateTime targetDate, StatisticPeriod period, out DateOnly startDate, out DateOnly endDate)
		{
			if (period == StatisticPeriod.Week)
			{
				int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Monday)) % 7;
				var start = targetDate.AddDays(-1 * diff).Date;
				var end = start.AddDays(6).Date;

				startDate = DateOnly.FromDateTime(start);
				endDate = DateOnly.FromDateTime(end);
			}
			else 
			{
				
				var firstDayOfTargetMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
				var start = firstDayOfTargetMonth.AddMonths(-5); 
				var end = firstDayOfTargetMonth.AddMonths(1).AddDays(-1);	

				startDate = DateOnly.FromDateTime(start);
				endDate = DateOnly.FromDateTime(end);
			}
		}

		private void InitializeChartData(CollectorStatisticsResponseModel response, DateTime targetDate, StatisticPeriod period)
		{
			if (period == StatisticPeriod.Week)
			{
				// Các nhãn cho tuần cố định như UI: "T2", "T3",...
				var weekLabels = new List<string> { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
				foreach (var label in weekLabels)
				{
					response.CompletedChart.Add(new ChartDataItem { Label = label, Value = 0 });
					response.FailedChart.Add(new ChartDataItem { Label = label, Value = 0 });
				}
			}
			else // StatisticPeriod.Month
			{
				// Sinh nhãn cho 6 tháng gần nhất theo định dạng "M/yy" (VD: "11/25", "4/26")
				var firstDayOfTargetMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
				for (int i = 5; i >= 0; i--)
				{
					var monthDate = firstDayOfTargetMonth.AddMonths(-i);
					string label = monthDate.ToString("M/yy"); // Format "M/yy" sẽ cho ra số như 4/26 hoặc 11/25

					response.CompletedChart.Add(new ChartDataItem { Label = label, Value = 0 });
					response.FailedChart.Add(new ChartDataItem { Label = label, Value = 0 });
				}
			}
		}

		private string GetLabelForDate(DateOnly date, StatisticPeriod period)
		{
			if (period == StatisticPeriod.Week)
			{
				return date.DayOfWeek switch
				{
					DayOfWeek.Monday => "T2",
					DayOfWeek.Tuesday => "T3",
					DayOfWeek.Wednesday => "T4",
					DayOfWeek.Thursday => "T5",
					DayOfWeek.Friday => "T6",
					DayOfWeek.Saturday => "T7",
					DayOfWeek.Sunday => "CN",
					_ => ""
				};
			}
			else // StatisticPeriod.Month
			{
				// Convert DateOnly về chuỗi "M/yy" để match với Label đã tạo ở hàm InitializeChartData
				var dateTime = new DateTime(date.Year, date.Month, date.Day);
				return dateTime.ToString("M/yy");
			}
		}
	}
}
