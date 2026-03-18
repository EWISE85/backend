using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class VoucherService : IVoucherService
	{
		private readonly IVoucherRepository _voucherRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly ICloudinaryService _cloudinaryService;


		public VoucherService(IVoucherRepository voucherRepository, IUnitOfWork unitOfWork, IUserRepository userRepository, ICloudinaryService cloudinaryService)
		{
			_voucherRepository = voucherRepository;
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_cloudinaryService = cloudinaryService;
		}

		public async Task<ImportResult> CheckAndUpdateVoucherAsync(CreateVoucherModel model)
		{
			var result = new ImportResult();
			if (model == null || string.IsNullOrWhiteSpace(model.Code))
			{
				result.Success = false;
				result.Messages.Add("Dữ liệu voucher trống hoặc thiếu mã Code.");
				return result;
			}

			try
			{
				var existingVoucher = await _unitOfWork.Vouchers.GetAsync(v => v.Code == model.Code);

				if (existingVoucher != null)
				{
					existingVoucher.Name = model.Name;
					existingVoucher.ImageUrl = model.ImageUrl;
					existingVoucher.Description = model.Description;
					existingVoucher.StartAt = model.StartAt;
					existingVoucher.EndAt = model.EndAt;
					existingVoucher.Value = model.Value;
					existingVoucher.PointsToRedeem = model.PointsToRedeem;

					if (!string.IsNullOrEmpty(model.Status))
					{
						var statusEnum = StatusEnumHelper.GetValueFromDescription<VoucherStatus>(model.Status);
						existingVoucher.Status = statusEnum.ToString();
					}

					_unitOfWork.Vouchers.Update(existingVoucher);
					result.Messages.Add($"Đã cập nhật thông tin voucher '{model.Name}' (Mã: {model.Code}).");
					result.IsNew = false;
				}
				else
				{
					// Tạo mới voucher
					var newVoucher = new Voucher
					{
						VoucherId = Guid.NewGuid(),
						Code = model.Code,
						Name = model.Name,
						ImageUrl = model.ImageUrl,
						Description = model.Description,
						StartAt = model.StartAt,
						EndAt = model.EndAt,
						Value = model.Value,
						PointsToRedeem = model.PointsToRedeem,
						Status = string.IsNullOrEmpty(model.Status) ? VoucherStatus.HOAT_DONG.ToString() : model.Status
					};

					await _unitOfWork.Vouchers.AddAsync(newVoucher);
					result.Messages.Add($"Thêm mới voucher '{model.Name}' (Mã: {model.Code}) thành công.");
					result.IsNew = true;
				}

				await _unitOfWork.SaveAsync();

				result.Success = true;
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] CheckAndUpdateVoucherAsync: {ex}");

				result.Success = false;
				result.Messages.Add($"Lỗi xử lý voucher {model?.Code}: {ex.Message}");
			}

			return result;
		}

		public async Task<bool> CreateVoucher(CreateVoucherModel model)
		{
			var isExistingCode = await _voucherRepository.GetAsync(v => v.Code == model.Code);
			if (isExistingCode != null)
			{
				throw new AppException("Code voucher đã tồn tại",400);
			}
			var voucher = new Voucher
			{
				Code = model.Code,
				Name = model.Name,
				Description = model.Description,
				ImageUrl = model.ImageUrl,
				StartAt = model.StartAt,
				EndAt = model.EndAt,
				Value = model.Value,
				PointsToRedeem = model.PointsToRedeem,
				Status = VoucherStatus.HOAT_DONG.ToString()
			};
			_unitOfWork.Vouchers.Add(voucher);
			var result = await _unitOfWork.SaveAsync();
			return result > 0;
		}

		public async Task<PagedResultModel<VoucherModel>> GetPagedVouchers(VoucherQueryModel model)
		{
			string? statusEnum = null;
			if (!string.IsNullOrEmpty(model.Status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<VoucherStatus>(model.Status).ToString();
			}
			var (vouchers, totalCount) = await _voucherRepository.GetPagedVoucher(model.Name, statusEnum, model.PageNumber, model.Limit);
			var voucherModels = vouchers.Select(v => new VoucherModel
			{
				VoucherId = v.VoucherId,
				Code = v.Code,
				Name = v.Name,
				Description = v.Description,
				ImageUrl = v.ImageUrl,
				StartAt = v.StartAt,
				EndAt = v.EndAt,
				Value = v.Value,
				PointsToRedeem = v.PointsToRedeem,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VoucherStatus>(v.Status)
			}).ToList();
			return new PagedResultModel<VoucherModel>(
				voucherModels,
				model.PageNumber,
				model.Limit,
				totalCount
			);
		}

		public async Task<PagedResultModel<VoucherModel>> GetPagedVouchersByUser(UserVoucherQueryModel model)
		{
			string? statusEnum = null;
			if (!string.IsNullOrEmpty(model.Status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<VoucherStatus>(model.Status).ToString();
			}
			var (vouchers, totalCount) = await _voucherRepository.GetPagedVoucherByUser(model.UserId,model.Name, statusEnum, model.PageNumber, model.Limit);
			var voucherModels = vouchers.Select(v => new VoucherModel
			{
				VoucherId = v.VoucherId,
				Code = v.Code,
				Name = v.Name,
				Description = v.Description,
				ImageUrl = v.ImageUrl,
				StartAt = v.StartAt,
				EndAt = v.EndAt,
				Value = v.Value,
				PointsToRedeem = v.PointsToRedeem,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VoucherStatus>(v.Status)
			}).ToList();
			return new PagedResultModel<VoucherModel>(
				voucherModels,
				model.PageNumber,
				model.Limit,
				totalCount
			);
		}

		public async Task<VoucherModel> GetVoucherById(Guid id)
		{
			var voucher = await _voucherRepository.GetAsync(v => v.VoucherId == id);
			if (voucher == null)
			{
				throw new AppException("Không tìm thấy voucher", 404);
			}
			return new VoucherModel
			{
				VoucherId = voucher.VoucherId,
				Code = voucher.Code,
				Name = voucher.Name,
				Description = voucher.Description,
				ImageUrl = voucher.ImageUrl,
				StartAt = voucher.StartAt,
				EndAt = voucher.EndAt,
				Value = voucher.Value,
				PointsToRedeem = voucher.PointsToRedeem,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VoucherStatus>(voucher.Status)
			};

		}

		public async Task UpdateFormatExcel(Guid systemConfigId, IFormFile formFile)
		{
			var config = await _unitOfWork.SystemConfig.GetAsync(c => c.SystemConfigId == systemConfigId);
			if (config == null)
			{
				throw new Exception($"System config with ID {systemConfigId} not found.");
			}

			// Kiểm tra file có hợp lệ không
			if (formFile == null || formFile.Length == 0)
			{
				throw new Exception("File is empty or null.");
			}
			string publicId = Path.GetFileName(formFile.FileName);

			// 3. Upload file dùng CloudinaryService
			string fileUrl = await _cloudinaryService.UploadRawFileAsync(formFile, publicId);
			// 4. Update link vào config.Value
			config.Value = fileUrl;
			_unitOfWork.SystemConfig.Update(config);
			await _unitOfWork.SaveAsync();
		}

		public async Task<bool> UserReceiveVoucher(Guid userId, Guid voucherId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null)
			{
				throw new AppException("Người dùng không tồn tại", 404);
			}
			var isVoucherExist = await _voucherRepository.GetAsync(v => v.VoucherId == voucherId);
			if (isVoucherExist == null)
			{
				throw new AppException("Voucher không tồn tại", 404);
			}
			var pointsTransaction = new PointTransactions
			{
				UserId = userId,
				VoucherId = voucherId,
				CreatedAt = DateTime.UtcNow,
				Desciption = $"Nhận voucher {isVoucherExist.Name}",
				TransactionType = PointTransactionType.DOI_DIEM.ToString(),
				Point = -isVoucherExist.PointsToRedeem
			};
			user.Points -= isVoucherExist.PointsToRedeem;
			var userVoucher = new UserVoucher
			{
				UserId = userId,
				VoucherId = voucherId,
				ReceivedAt = DateTime.UtcNow,
				IsUsed = false,
				UsedAt = null,
			};
			_unitOfWork.PointTransactions.Add(pointsTransaction);
			_unitOfWork.Users.Update(user);
			_unitOfWork.UserVouchers.Add(userVoucher);
			var result = await _unitOfWork.SaveAsync();
			return result > 0;

		}
	}
}
