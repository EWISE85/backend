using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IExportService
    {
        Task<byte[]> ExportFullSystemDashboardAsync(DateOnly from, DateOnly to);
    }
}
