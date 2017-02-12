using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.Models {
	class AlpcCapture : IDisposable {
		List<AlpcMessage> _messages = new List<AlpcMessage>(1 << 16);
		TraceEventSession _session;
		List<ALPCSendMessageTraceData> _sendMessages = new List<ALPCSendMessageTraceData>(32);

		public bool IsRunning { get; set; }

		public IReadOnlyList<AlpcMessage> GetMessages() => _messages.ToList();

		public IReadOnlyList<AlpcMessage> GetMessagesAndClear() {
			lock (_messages) {
				var result = _messages.ToList();
				_messages.Clear();
				return result;
			}
		}

		public void Dispose() {
			IsRunning = false;
			_session.Dispose();
		}

		public void Start() {
			_session = new TraceEventSession("ALPCCapture");
			_session.StopOnDispose = true;
			_session.BufferSizeMB = 128;

			_session.EnableKernelProvider(KernelTraceEventParser.Keywords.AdvancedLocalProcedureCalls,
				KernelTraceEventParser.Keywords.AdvancedLocalProcedureCalls);

			var parser = new KernelTraceEventParser(_session.Source);
			parser.ALPCReceiveMessage += Parser_ALPCReceiveMessage;
			parser.ALPCSendMessage += Parser_ALPCSendMessage;
			//parser.ALPCWaitForReply += Parser_ALPCWaitForReply;
			//parser.ALPCUnwait += Parser_ALPCUnwait;
			_session.Source.Process();
		}

		public void Pause() {
			IsRunning = false;
		}

		public void Run() {
			IsRunning = true;
		}

		private void Parser_ALPCUnwait(ALPCUnwaitTraceData obj) {
			//Console.WriteLine($"Unwait {obj.ProcessName} TID={obj.ThreadID} status: {obj.Status}");
		}

		private void Parser_ALPCWaitForReply(ALPCWaitForReplyTraceData obj) {
			//Console.WriteLine($"Wait for reply {obj.ProcessName} TID={obj.ThreadID} msg: {obj.MessageID}...");
		}

		private void Parser_ALPCSendMessage(ALPCSendMessageTraceData obj) {
			if (!IsRunning)
				return;

			lock (_sendMessages) {
				_sendMessages.Add((ALPCSendMessageTraceData)obj.Clone());
			}
		}

		private void Parser_ALPCReceiveMessage(ALPCReceiveMessageTraceData obj) {
			if (!IsRunning)
				return;

			ALPCSendMessageTraceData source;
			lock (_sendMessages) {
				source = _sendMessages.FirstOrDefault(msg => msg.MessageID == obj.MessageID);
			}
			if (source == null) {
				//Console.WriteLine($"Receive without Send {obj.ProcessName} ({obj.ProcessID}) msg: {obj.MessageID}");
				return;
			}

			var message = new AlpcMessage {
				SourceProcess = source.ProcessID,
				SourceProcessName = source.ProcessName,
				TargetProcess = obj.ProcessID,
				TargetProcessName = obj.ProcessName,
				MessageId = obj.MessageID,
				SourceThread = source.ThreadID,
				TargetThread = obj.ThreadID,
				SendTime = source.TimeStamp,
				ReceiveTime = obj.TimeStamp,
			};
			lock (_messages) {
				_messages.Add(message);
			}
			_sendMessages.Remove(source);
			//Dump(message);
		}

		private void Dump(AlpcMessage message) {
			Console.WriteLine($"{message.SourceProcessName} ({message.SourceProcess} TID={message.SourceThread}) -> {message.MessageId}" +
				$" -> {message.TargetProcessName} ({message.TargetProcess})");
		}
	}
}
