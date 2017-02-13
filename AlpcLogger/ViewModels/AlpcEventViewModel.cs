using AlpcLogger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.ViewModels {
	class AlpcEventViewModel {
		internal AlpcEvent Event { get; }
		public int Index { get; }

		public AlpcEventViewModel(AlpcEvent evt, int index) {
			Event = evt;
			Index = index;
		}

		public string ProcessName => Event.ProcessName;
		public int ProcessId => Event.ProcessId;
		public int ThreadId => Event.ThreadId;
		public int MessageId => Event.MessageId;
		public DateTime Time => Event.Time;
		public AlpcEventType Type => Event.Type;
	}
}
