using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class SendNotificationToUserModel
    {
        public List<Guid> UserIds { get; set; }

		public string Title { get; set; }

		public string Message { get; set; }


	}
}
