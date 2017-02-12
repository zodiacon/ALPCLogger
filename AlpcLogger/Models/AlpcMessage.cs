using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.Models {
	class AlpcMessage {
		public int SourceProcess { get; set; }
		public string SourceProcessName { get; set; }
		public int TargetProcess { get; set; }
		public string TargetProcessName { get; set; }
		public int MessageId { get; set; }
		public DateTime SendTime { get; set; }
		public DateTime ReceiveTime { get; set; }
		public int SourceThread { get; set; }
		public int TargetThread { get; set; }
	}
}
