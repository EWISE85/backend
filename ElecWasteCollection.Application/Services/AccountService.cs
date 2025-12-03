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
	public class AccountService : IAccountService
	{
		private readonly List<Account> _accounts = FakeDataSeeder.accounts;
		public bool AddNewAccount(Account account)
		{
			_accounts.Add(account);
			return true;
		}
	}
}
