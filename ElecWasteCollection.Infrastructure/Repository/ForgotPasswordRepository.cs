using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
	public class ForgotPasswordRepository : GenericRepository<ForgotPassword>, IForgotPasswordRepository
	{
		public ForgotPasswordRepository(DbContext context) : base(context)
		{
		}
	}
}
