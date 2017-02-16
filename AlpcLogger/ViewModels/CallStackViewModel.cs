using AlpcLogger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace AlpcLogger.ViewModels {
	sealed class CallStackViewModel : DialogViewModelBase {
		public CallStack Stack { get; }

		public CallStackViewModel(Window dialog, CallStack stack) : base(dialog) {
			Stack = stack;
		}

		public string Title => $"Call Stack TID={Stack.Event.ThreadId} PID={Stack.Event.ProcessId} ({Stack.Event.ProcessName})";

		public StackFrame[] Frames => Stack.Frames;
	}
}
