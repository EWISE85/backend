using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IUserAddressService
	{
		Task<bool> AddUserAddress(CreateUpdateUserAddress create);
		Task<bool> UpdateUserAddress(Guid userId, CreateUpdateUserAddress update);
		Task<bool> DeleteUserAddress(Guid userId);
		Task<List<UserAddressResponse>?> GetByUserId(Guid userId);

	}
}
