using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class UserService : IUserService
	{
		private List<User> users = FakeDataSeeder.users;
		public UserService()
		{
		}

		public List<User> GetAll()
		{

			return users;
		}

		public void AddRange(IEnumerable<User> newUsers)
		{
			throw new NotImplementedException();
		}

		public void AddUser(User user)
		{
			throw new NotImplementedException();
		}

		public  User GetById(Guid id)
		{
			return  users.FirstOrDefault(u => u.UserId == id);
		}

		public void UpdateUser(int iat, int ing, Guid id)
		{
			var user = users.FirstOrDefault(u => u.UserId == id);
			if (user != null)
			{
				user.Iat = iat;
				user.Ing = ing;
			}
		}
	}
}
