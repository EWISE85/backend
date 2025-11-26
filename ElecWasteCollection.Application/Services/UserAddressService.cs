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
	public class UserAddressService : IUserAddressService
	{
		private readonly List<UserAddress> _addresses = FakeDataSeeder.userAddress;
		private readonly List<User> _user = FakeDataSeeder.users;
		public bool AddUserAddress(CreateUpdateUserAddress create)
		{
			var userExists = _user.Any(u => u.UserId == create.UserId);
			if (!userExists)
			{
				return false; // User does not exist
			}
			var address = new UserAddress
			{
				UserAddressId = Guid.NewGuid(),
				UserId = create.UserId,
				Address = create.Address,
				Iat = create.Iat,
				Ing = create.Ing,
				isDefault = create.isDefault
			};
			var existingAddress = _addresses.Where(a => a.UserId == create.UserId);
			if (create.isDefault)
			{
				foreach (var addr in existingAddress)
				{
					addr.isDefault = false;
				}
			}
			_addresses.Add(address);
			return true;
		}

		public bool DeleteUserAddress(Guid userAddressId)
		{
			var address = _addresses.FirstOrDefault(a => a.UserAddressId == userAddressId);
			if (address == null)
			{
				return false; // Address not found
			}
			_addresses.Remove(address);
			return true;
		}

		public List<UserAddressResponse>? GetByUserId(Guid userId)
		{
			var address = _addresses.Where(a => a.UserId == userId);
			if (address == null)
			{
				return null; // Address not found
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

		public bool UpdateUserAddress(Guid userAddressId, CreateUpdateUserAddress update)
		{
			var userExists = _user.Any(u => u.UserId == update.UserId);
			if (!userExists)
			{
				return false; // User does not exist
			}
			var address = _addresses.FirstOrDefault(a => a.UserAddressId == userAddressId);
			if (address == null)
			{
				return false; // Address not found
			}
			address.Address = update.Address;
			address.Iat = update.Iat;
			address.Ing = update.Ing;
			address.isDefault = update.isDefault;
			return true;
		}
	}
}
