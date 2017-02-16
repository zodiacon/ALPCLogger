using AlpcLogger.Models;
using DebugHelp;
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
		CallStack _stack;
		public CallStack Stack => _stack ?? (_stack = BuildStack(Event.Stack));

		private CallStack BuildStack(ulong[] stack) {
			if(stack == null)
				return null;

			StackFrame[] frames;
			using(var handler = SymbolHandler.TryCreateFromProcess(ProcessId, SymbolOptions.Include32BitModules | SymbolOptions.UndecorateNames)) {
				if(handler == null)
					frames = stack.Select(p => new StackFrame { Address = p }).ToArray();

				else {
					frames = new StackFrame[stack.Length];
					var symbol = new SymbolInfo();
					ulong disp;
					for(int i = 0; i < stack.Length; i++)
						if(handler.TryGetSymbolFromAddress(stack[i], ref symbol, out disp))
							frames[i] = new StackFrame { Address = stack[i], Offset = disp, SymbolName = symbol.Name };
						else
							frames[i] = new StackFrame { Address = stack[i] };
				}
				return new CallStack(Event, frames);
			}
		}

		private string Dispacement(ulong disp) => disp == 0 ? string.Empty : $"+0x{disp:X}";
	}
}
