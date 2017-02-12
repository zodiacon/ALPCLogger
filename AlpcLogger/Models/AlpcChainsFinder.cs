using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.Models {
	class AlpcChainsFinder {
		IList<AlpcMessage> _messages;
		int _current;
		List<AlpcMessage> _currentChain;
		Dictionary<int, AlpcMessage> _threadToMessage = new Dictionary<int, AlpcMessage>(128);

		public AlpcChainsFinder(IList<AlpcMessage> messages) {
			_messages = messages;
		}

		public IList<AlpcMessage> FindNextChain() {
			if(_current >= _messages.Count)
				return null;

			_currentChain = null;
			while(_current < _messages.Count) {
				var msg = _messages[_current];
				_current++;


				AlpcMessage existingMsg;
				if(_threadToMessage.TryGetValue(msg.SourceThread, out existingMsg)) {
					// target thread exists
					if(_currentChain == null) {
						_currentChain = new List<AlpcMessage>(2);
						_currentChain.Add(existingMsg);
					}
					_currentChain.Add(msg);
					_threadToMessage.Remove(existingMsg.TargetThread);
					if(_currentChain.Count > 1) {
						// check if loop
						if(msg.TargetThread == _currentChain[0].SourceThread)
							break;
					}
					else {
						_threadToMessage.Add(msg.TargetThread, msg);
					}
				}
				else if(_currentChain != null) {
					break;
				}
				else {
					_threadToMessage[msg.TargetThread] = msg;
				}
			}
			return _currentChain;
		}

		public IEnumerable<IList<AlpcMessage>> FindAllChains() {
			IList<AlpcMessage> item;
			while((item = FindNextChain()) != null) {
				yield return item;
			}
		}
	}
}
