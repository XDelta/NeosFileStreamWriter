using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;

namespace NeosFileStreamWriter;

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
		if (!World.UnsafeMode) {
			OnFailed.Trigger();
			return;
		}
#endif
		if (_streamWriter != null) {
			return; //File already open
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
	public async void Write() {
#if !UNSAFE
		if (!World.UnsafeMode) {
			OnFailed.Trigger();
			return;
		}
#endif
		if (_streamWriter == null) {
			OnFailed.Trigger();
			return;
		}
		string _string = TextString.Evaluate("");

		if (NewLine.Evaluate(false)) {
			_string += Environment.NewLine;
		}

		OnWriteStarted.Trigger();
		bool result = await _writeQueue.SendAsync(() => {
			_streamWriter.Write(_string);
		});
		if (result) {
			OnWriteFinished.Trigger();
		} else {
			OnFailed.Trigger();
		}
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

	private readonly TransformBlock<Action, bool> _writeQueue = new(
		async action => {
			try {
				await Task.Run(() => action());
				return true;
			} catch (Exception ex) {
				UniLog.Log($"Exception writing data to file:\n{ex}", false);
				return false;
			}
		},
		new ExecutionDataflowBlockOptions {
			EnsureOrdered = true,
			MaxDegreeOfParallelism = Environment.ProcessorCount
		}
	);
}