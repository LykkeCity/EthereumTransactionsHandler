using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services.Models.Internal;

namespace Services.Models
{
    public class IncomingRequest
    {
		public RequestType Action { get; set; }

		public string JsonData { get; set; }
    }
}
