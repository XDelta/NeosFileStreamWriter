using System;
using System.IO;
using System.Threading.Tasks.Dataflow;

using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;


namespace NeosFileStreamWriter {
	[Category("LogiX/Experimental")]
	public class FileStreamWriter : LogixNode {
		public readonly Input<string> FilePath;
		public readonly Input<string> TextString;
		public readonly Input<bool> Append;
		public readonly Input<bool> NewLine;
		public readonly Impulse OnOpenFile;
		public readonly Impulse OnWriteStarted;
		public readonly Impulse OnWriteFinished;
		public readonly Impulse OnCloseFile;
		public readonly Impulse OnFailed;
		private FileStream _fileStream;
		private StreamWriter _streamWriter;

		[ImpulseTarget]
		public void Open() {
#if !UNSAFE
			if (!base.World.UnsafeMode) {
				OnFailed.Trigger();
				return;
			}
#endif
			if (_streamWriter != null) {
				//File already open
				return;
			}
			string _filePath = FilePath.Evaluate(null);
			bool _append = Append.Evaluate(false);
			if (string.IsNullOrWhiteSpace(_filePath)) {
				OnFailed.Trigger();
				return;
			}

			FileMode fileMode = _append ? FileMode.Append : FileMode.Create;
			_fileStream = new FileStream(_filePath, fileMode, FileAccess.Write, FileShare.ReadWrite);
			_streamWriter = new StreamWriter(_fileStream);
			OnOpenFile.Trigger();
		}

		[ImpulseTarget]
		public void Write() {
#if !UNSAFE
			if (!base.World.UnsafeMode) {
				OnFailed.Trigger();
				return;
			}
#endif
			if (_streamWriter == null) {
				OnFailed.Trigger();
				return;
			}
			string _string = TextString.Evaluate(null);
			if (_string == null) {
				_string = "";
			}
			if (NewLine.Evaluate(false)) {
				_string += Environment.NewLine;
			}

			OnWriteStarted.Trigger();
			Action onWriteFinished = null;
			_writeQueue.Post(delegate {
				_streamWriter.Write(_string);
				if (onWriteFinished == null) {
					onWriteFinished = () => {
						OnWriteFinished.Trigger();
					};
				}
				RunSynchronously(onWriteFinished, false);
			});
		}

		[ImpulseTarget]
		public void Close() {
#if !UNSAFE
			if (!base.World.UnsafeMode) {
				OnFailed.Trigger();
				return;
			}
#endif
			if (_streamWriter == null) {
				OnFailed.Trigger();
				return;
			}

			_streamWriter.Flush();
			_streamWriter.Close();
			_streamWriter = null;
			_fileStream = null;
			OnCloseFile.Trigger();
		}

		protected override void OnDispose() {
            if (_streamWriter != null) {
                _streamWriter.Flush();
                _streamWriter.Close();
                _streamWriter = null;
                _fileStream = null;
            }
            base.OnDispose();
        }

        private static ActionBlock<Action> _writeQueue = new ActionBlock<Action>(
			delegate (Action action) {
					try {
					action();
				} catch (Exception ex) {
					UniLog.Log("Exception writing data to file:\n" + ((ex != null) ? ex.ToString() : null), false);
				}
			},
			new ExecutionDataflowBlockOptions {
				EnsureOrdered = true,
				MaxDegreeOfParallelism = 1
			}
		);
	}
}
