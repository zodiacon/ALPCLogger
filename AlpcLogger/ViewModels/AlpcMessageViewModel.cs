using AlpcLogger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.ViewModels {
	class AlpcMessageViewModel {
        internal readonly AlpcMessage Message;

		public AlpcMessageViewModel(AlpcMessage message, int index) {
			Message = message;
			Index = index;
		}

        public int Index { get; }

        public DateTime SendTime => Message.SendTime;
        public string SourceProcessName => Message.SourceProcessName;
        public int SourceProcess => Message.SourceProcess;
        public int SourceThread => Message.SourceThread;
        public int MessageId => Message.MessageId;
        public DateTime ReceiveTime => Message.ReceiveTime;
        public string TargetProcessName => Message.TargetProcessName;
        public int TargetProcess => Message.TargetProcess;
        public int TargetThread => Message.TargetThread;
    }
}
