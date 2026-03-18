using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
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
	public class VoucherService : IVoucherService
	{
		private readonly IVoucherRepository _voucherRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
	

		public VoucherService(IVoucherRepository voucherRepository, IUnitOfWork unitOfWork, IUserRepository userRepository)
		{
			_voucherRepository = voucherRepository;
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
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
