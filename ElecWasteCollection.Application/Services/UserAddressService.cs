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
	public class UserAddressService : IUserAddressService
	{
		private readonly IUserAddressRepository _userAddressRepository;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;

		public UserAddressService(IUserAddressRepository userAddressRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
		{
			_userAddressRepository = userAddressRepository;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
		}
		public async Task<bool> AddUserAddress(CreateUpdateUserAddress create)
		{
			var userExists = await _userRepository.GetAsync(u => u.UserId == create.UserId);
			if (userExists == null) throw new AppException("User không tồn tại", 404);
			if (create.isDefault)
			{
				var existingAddresses = await _userAddressRepository.GetsAsync(a => a.UserId == create.UserId);

				if (existingAddresses != null && existingAddresses.Any())
				{
					foreach (var addr in existingAddresses)
					{
						if (addr.isDefault)
						{
							addr.isDefault = false;
							_unitOfWork.UserAddresses.Update(addr);
						}
					}
				}
			}
			var newAddress = new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = create.UserId,
				Address = create.Address,
				Iat = create.Iat,
				Ing = create.Ing,
				isDefault = create.isDefault
			};
			await _unitOfWork.UserAddresses.AddAsync(newAddress);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> DeleteUserAddress(Guid userAddressId)
		{
			var address = await _userAddressRepository.GetAsync(a => a.UserAddressId == userAddressId);
			if (address == null) throw new AppException("Địa chỉ không tồn tại", 404);
			_unitOfWork.UserAddresses.Delete(address);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<List<UserAddressResponse>?> GetByUserId(Guid userId)
		{
			var address = await _userAddressRepository.GetsAsync(a => a.UserId == userId);
			if (address == null)
			{
				return null;
			}
			var response = address.Select(a => new UserAddressResponse
			{
				UserAddressId = a.UserAddressId,
				UserId = a.UserId,
				Address = a.Address,
				Iat = a.Iat,
				Ing = a.Ing,
				isDefault = a.isDefault
			}).ToList();
			return response;
		}

		public async Task<bool> UpdateUserAddress(Guid userAddressId, CreateUpdateUserAddress update)
		{
			var userExists = await _userRepository.GetAsync(u => u.UserId == update.UserId);
			if (userExists == null) throw new AppException("User không tồn tại", 404);
			var addressToUpdate = await _userAddressRepository.GetAsync(a => a.UserAddressId == userAddressId);
			if (addressToUpdate == null) throw new AppException("Địa chỉ không tồn tại", 404);
			if (update.isDefault)
			{
				var otherAddresses = await _userAddressRepository.GetsAsync(a => a.UserId == update.UserId && a.UserAddressId != userAddressId);
				if (otherAddresses != null)
				{
					foreach (var addr in otherAddresses)
					{
						if (addr.isDefault)
						{
							addr.isDefault = false;
							_unitOfWork.UserAddresses.Update(addr);
						}
					}
				}
			}
			addressToUpdate.Address = update.Address;
			addressToUpdate.Iat = update.Iat;
			addressToUpdate.Ing = update.Ing;
			addressToUpdate.isDefault = update.isDefault;
			_unitOfWork.UserAddresses.Update(addressToUpdate);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
