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
		private static List<User> users = new List<User>();
		
		private void AddData()
		{
			var user1 = new User
			{
				UserId = Guid.NewGuid(),
				Name = "Trần Văn An",
				Email = "tran.van.an@example.com",
				Phone = "0901234567",
				Address = "123 Đường Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
				Avatar = "https://picsum.photos/id/1011/200/200",
				Iat = 1050000,
				Ing = 10660000
			};
			var user2 = new User
			{
				UserId = Guid.NewGuid(),
				Name = "Lê Thị Mai",
				Email = "le.thi.mai@example.com",
				Phone = "0987654321",
				Address = "45 Hàng Ngang, Quận Hoàn Kiếm, Hà Nội",
				Avatar = "https://picsum.photos/id/1025/200/200",
				Iat = 2100000,
				Ing = 10580000
			};
			users.AddRange(new List<User> { user1, user2 });
		}
		public UserService()
		{
		}

		public List<User> GetAll()
		{
			if (users.Count == 0)
			{
				AddData();
			}
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
