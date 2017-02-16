using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.Models {
	class StackFrame {
		public string SymbolName { get; set; }
		public ulong Offset { get; set; }
		public ulong Address { get; set; }

		public override string ToString() {
			var value = $"0x{Address:X}";
			if(SymbolName == null)
				return value;
			value += $" {SymbolName}";
			if(Offset != 0)
				value += $"+0x{Offset:X}";
			return value;
		}
	}

	class CallStack {
		public AlpcEvent Event { get; }
		public StackFrame[] Frames { get; }

		public CallStack(AlpcEvent @event, StackFrame[] frames) {
			Event = @event;
			Frames = frames;
		}
	}
}
