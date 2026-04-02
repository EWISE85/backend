using ElecWasteCollection.Application.Exceptions;
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
	public class PublicHolidayService : IPublicHolidayService
	{
		private readonly IUnitOfWork _unitOfWork;

		public PublicHolidayService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<ImportResult> CheckAndUpdatePublicHolidayAsync(PublicHoliday publicHoliday)
		{
			var result = new ImportResult();

			if (publicHoliday == null || string.IsNullOrWhiteSpace(publicHoliday.Name))
			{
				throw new AppException("Tên ngày nghỉ bị trống",400);
			}

			// Kiểm tra tính hợp lệ của ngày tháng (tránh trường hợp parse lỗi ra MinValue ở hàm gọi)
			if (publicHoliday.StartDate == DateOnly.MinValue || publicHoliday.EndDate == DateOnly.MinValue)
			{
				throw new AppException($"Ngày bắt đầu hoặc ngày kết thúc của ngày nghỉ '{publicHoliday.Name}' không hợp lệ.", 400);
			}

			if (publicHoliday.StartDate > publicHoliday.EndDate)
			{
				throw new AppException($"Ngày bắt đầu của ngày nghỉ '{publicHoliday.Name}' không được sau ngày kết thúc.", 400);
			}

			try
			{
				var existingHoliday = await _unitOfWork.PublicHolidays.GetAsync(h => h.Name == publicHoliday.Name);

				if (existingHoliday != null)
				{
					existingHoliday.StartDate = publicHoliday.StartDate;
					existingHoliday.EndDate = publicHoliday.EndDate;
					existingHoliday.Description = publicHoliday.Description;
					existingHoliday.IsActive = publicHoliday.IsActive;

					_unitOfWork.PublicHolidays.Update(existingHoliday);
					result.Messages.Add($"Đã cập nhật thông tin ngày nghỉ '{publicHoliday.Name}'.");
					result.IsNew = false;
				}
				else
				{
					// Tạo mới ngày nghỉ
					var newHoliday = new PublicHoliday
					{
						PublicHolidayId = Guid.NewGuid(),
						Name = publicHoliday.Name,
						StartDate = publicHoliday.StartDate,
						EndDate = publicHoliday.EndDate,
						Description = publicHoliday.Description,
						IsActive = true,
						CreatedAt = DateTime.UtcNow
					};

					await _unitOfWork.PublicHolidays.AddAsync(newHoliday);
					result.Messages.Add($"Thêm mới ngày nghỉ '{publicHoliday.Name}' thành công.");
					result.IsNew = true;
				}

				// Lưu thay đổi vào database
				await _unitOfWork.SaveAsync();

				result.Success = true;
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] CheckAndUpdatePublicHolidayAsync: {ex}");

				result.Success = false;
				result.Messages.Add($"Lỗi xử lý ngày nghỉ '{publicHoliday?.Name}': {ex.Message}");
			}

			return result;
		}
		public async Task DeactivateMissingHolidaysAsync(List<string> importedNames, ImportResult result)
		{
			try
			{
				// Tùy theo Repository pattern của bạn, có thể là FindAsync, GetAllAsync hoặc GetListAsync
				// Lấy các ngày nghỉ đang Active nhưng tên KHÔNG nằm trong danh sách Excel
				var missingHolidays = await _unitOfWork.PublicHolidays.GetsAsync(h => h.IsActive && !importedNames.Contains(h.Name));

				if (missingHolidays != null && missingHolidays.Any())
				{
					foreach (var holiday in missingHolidays)
					{
						holiday.IsActive = false; // Cập nhật trạng thái
						_unitOfWork.PublicHolidays.Update(holiday);

						result.Messages.Add($"Đã vô hiệu hóa ngày nghỉ '{holiday.Name}' do không có trong file Excel cập nhật.");
					}

					await _unitOfWork.SaveAsync();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] DeactivateMissingHolidaysAsync: {ex}");
				result.Messages.Add($"Lỗi khi vô hiệu hóa các ngày nghỉ cũ: {ex.Message}");
			}
		}

		public async Task<List<PublicHolidayModel>> GetAllPublicHolidayActive()
		{
			var holidays = await _unitOfWork.PublicHolidays.GetsAsync(h => h.IsActive);

			var result = holidays.Select(h => new PublicHolidayModel
			{
				PublicHolidayId = h.PublicHolidayId,
				Name = h.Name,
				Description = h.Description,
				StartDate = h.StartDate,
				EndDate = h.EndDate,
			}).ToList();

			return result;
		}
	}
}
