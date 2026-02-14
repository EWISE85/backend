using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class StagingDataModel
    {
        public Guid StagingId { get; set; } = Guid.NewGuid();
        public DateOnly Date { get; set; }
        public string PointId { get; set; }
        public string VehicleId { get; set; }
        public List<ProductStagingDetail> ProductDetails { get; set; }
    }

    public class ProductStagingDetail
    {
        public Guid ProductId { get; set; }
        public string EstimatedArrival { get; set; }
        public double DistanceKm { get; set; }
    }
}
