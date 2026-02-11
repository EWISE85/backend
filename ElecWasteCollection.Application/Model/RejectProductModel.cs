using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class RejectProductRequest
    {
        public string SmallCollectionPointId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<Guid> ProductIds { get; set; } = new();
    }

    public class RejectProductResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RejectProductData Data { get; set; } = new();
    }

    public class RejectProductData
    {
        public int TotalProcessed { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
        public List<RejectDetail> Details { get; set; } = new();
    }

    public class RejectDetail
    {
        public Guid ProductId { get; set; }
        public string Status { get; set; } = string.Empty; // "success" hoặc "failed"
        public string NewProductStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
