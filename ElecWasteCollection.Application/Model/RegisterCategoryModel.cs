using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class RegisterCategoryRequest
    {
        public string CompanyId { get; set; } = string.Empty;
        public List<Guid> CategoryIds { get; set; } = new();
    }

    public class RegisterCategoryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRegistered { get; set; }
    }

    public class CompanyRegisteredCategoryResponse
    {
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalCategories { get; set; }
        public List<CategoryDetailResponse> CategoryDetails { get; set; } = new();
    }

    public class CategoryDetailResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CompanyListResponse
    {
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalRegisteredCategories { get; set; }
    }
}
