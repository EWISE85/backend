using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class DashboardService : IDashboardService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductRepository _productRepository;
		public DashboardService(IUnitOfWork unitOfWork, IProductRepository productRepository)
		{
			_unitOfWork = unitOfWork;
			_productRepository = productRepository;
		}
		public async Task<DashboardSummaryModel> GetDashboardSummary(DateOnly from, DateOnly to)
		{
			DateTime fromDate = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

			DateTime toDate = DateTime.SpecifyKind(to.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);

			var totalUsers = await _unitOfWork.Users.CountAsync(u => u.CreateAt >= fromDate && u.CreateAt <= toDate);

			var totalCompanies = await _unitOfWork.CollectionCompanies.CountAsync(c => c.Created_At >= fromDate && c.Created_At <= toDate);

			var totalProducts = await _unitOfWork.Products.CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);

			var statsDict = await _productRepository.GetProductCountsByCategoryAsync(fromDate, toDate);

			var productByCategory = statsDict.Select(kvp => new CategoryStatisticModel
			{
				CategoryName = kvp.Key,
				Count = kvp.Value
			}).ToList();

			return new DashboardSummaryModel
			{
				FromDate = from, 
				ToDate = to,
				TotalUsers = totalUsers,
				TotalCompanies = totalCompanies,
				TotalProducts = totalProducts,
				ProductCategories = productByCategory
			};
		}
	}
}
