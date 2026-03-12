using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RenderHeads.Media.AVProMovieCapture
{
	public class CaptureAudioFromMultipleSources : UnityAudioCapture
	{
		[SerializeField] bool _debugLogging = false;
		[SerializeField] bool _muteAudio = false;
		public List<OnAudioFilterReadForwarder> _OnAudioFilterReadForwarders;

		private const int BufferSize = 16;
		private float[] _buffer = new float[0];
		private float[] _readBuffer = new float[0];
		private int _bufferIndex;
		private GCHandle _bufferHandle;
		private int _numChannels;

		private int _overflowCount;
		private object _lockObject = new object();

		public float[] Buffer { get { return _readBuffer; } }
		public int BufferLength { get { return _bufferIndex; } }
		public System.IntPtr BufferPtr { get { return _bufferHandle.AddrOfPinnedObject(); } }

		public override int OverflowCount { get { return _overflowCount; } }
		public override int ChannelCount { get { return _numChannels; } }


		public override int SampleRate { get { return AudioSettings.outputSampleRate; } }

		public bool Capturing = false;

		/// <summary>
		/// ÃÌº”“Ù∆µ‘¥
		/// </summary>
		/// <param name="filterReadForwarder"></param>
		public void AddAudioFilterReadForwarder(OnAudioFilterReadForwarder filterReadForwarder)
		{
			if (_OnAudioFilterReadForwarders == null)
				_OnAudioFilterReadForwarders = new List<OnAudioFilterReadForwarder>();

			if (_OnAudioFilterReadForwarders.Count == 0)
            {
				filterReadForwarder.Callback += OnAudioFilterReadSingle;
				_OnAudioFilterReadForwarders.Add(filterReadForwarder);
			}
            else
            {
				if (_OnAudioFilterReadForwarders.Count == 1)
				{
					foreach (var onAudioFilterReadForwarder in _OnAudioFilterReadForwarders)
					{
						onAudioFilterReadForwarder.Callback -= OnAudioFilterReadSingle;
						onAudioFilterReadForwarder.Callback += OnAudioFilterReadCombiner;
					}
				}

				filterReadForwarder.Callback += OnAudioFilterReadCombiner;
				_OnAudioFilterReadForwarders.Add(filterReadForwarder);
			}
		}

		public void RemoveAllAudioFilterReadForwarder()
		{
			if (_OnAudioFilterReadForwarders == null)
				return;

			for(int i = 0; i < _OnAudioFilterReadForwarders.Count; i++)
            {
				_OnAudioFilterReadForwarders[i].Callback = null;
				Destroy(_OnAudioFilterReadForwarders[i]);
            }
			_OnAudioFilterReadForwarders.Clear();
		}

		public override void PrepareCapture()
		{
			int bufferLength = 0;
			int numBuffers = 0;
			AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
			_numChannels = GetUnityAudioChannelCount();
			if (_debugLogging)
			{
				Debug.Log(string.Format("[UnityAudioCapture] SampleRate: {0}hz SpeakerMode: {1} BestDriverMode: {2} (DSP using {3} buffers of {4} bytes using {5} channels)", AudioSettings.outputSampleRate, AudioSettings.speakerMode.ToString(), AudioSettings.driverCapabilities.ToString(), numBuffers, bufferLength, _numChannels));
			}

			_buffer = new float[bufferLength * _numChannels * numBuffers * BufferSize];
			_readBuffer = new float[bufferLength * _numChannels * numBuffers * BufferSize];
			_bufferIndex = 0;
			_bufferHandle = GCHandle.Alloc(_readBuffer, GCHandleType.Pinned);
			_overflowCount = 0;
		}

		public override void StartCapture()
		{
			FlushBuffer();
			Capturing = true;
		}

		public override void StopCapture()
		{
			lock (_lockObject)
			{
				_bufferIndex = 0;
				if (_bufferHandle.IsAllocated)
					_bufferHandle.Free();
				_readBuffer = _buffer = null;
			}
			_numChannels = 0;
			Capturing = false;
		}

		public override void PauseCapture()
		{
			Capturing = false;
		}

		public override void ResumeCapture()
		{
			Capturing = true;
		}

		public override System.IntPtr ReadData(out int length)
		{
			lock (_lockObject)
			{
				System.Array.Copy(_buffer, 0, _readBuffer, 0, _bufferIndex);
				length = _bufferIndex;
				_bufferIndex = 0;
			}
			return _bufferHandle.AddrOfPinnedObject();
		}

		public override void FlushBuffer()
		{
			lock (_lockObject)
			{
				_bufferIndex = 0;
				_overflowCount = 0;
			}
		}

		private void OnAudioFilterReadSingle(float[] data, int channels)
		{
			if (!Capturing)
				return;

			if (_buffer != null && _buffer.Length > 0)
			{
				// TODO: use double buffering
				lock (_lockObject)
				{
					int length = Mathf.Min(data.Length, _buffer.Length - _bufferIndex);

					//System.Array.Copy(data, 0, _buffer, _bufferIndex, length);

					if (!_muteAudio)
					{
						for (int i = 0; i < length; i++)
						{
							_buffer[i + _bufferIndex] = data[i];
						}
					}
					else
					{
						for (int i = 0; i < length; i++)
						{
							_buffer[i + _bufferIndex] = data[i];
							data[i] = 0f;
						}
					}
					_bufferIndex += length;

					if (length < data.Length)
					{
						_overflowCount++;
						// Check how many times we've overflowed and disable the warning if more than 10
						if (_overflowCount < 10)
						{
							Debug.LogWarning("[AVProMovieCapture] Audio buffer has overflowed which may cause sync issues. " +
											 "Disable this component if not recording Unity audio. " +
											 ((_overflowCount == 9) ? "Silencing this warning now." : ("(" + _overflowCount + ").")));
						}
					}
				}
			}
		}

		int _count = 0;
		int _lastLength;
		float scaleFactor = 0.5f;

		private void OnAudioFilterReadCombiner(float[] data, int channels)
		{
			if (!Capturing)
				return;

			if (_buffer != null && _buffer.Length > 0)
			{
				_count++;
				// TODO: use double buffering
				lock (_lockObject)
				{
					int length = Mathf.Min(data.Length, _buffer.Length - _bufferIndex);

					if (!_muteAudio)
					{
						if (_count == 1)
						{
							for (int i = 0; i < length; i++)
							{
								_buffer[i + _bufferIndex] = data[i];
							}
						}
						else
						{
							for (int i = 0; i < length; i++)
							{
								//_buffer[i + _bufferIndex] += data[i];
								// ±‹√‚float“Á≥ˆ
								_buffer[i + _bufferIndex] = (_buffer[i + _bufferIndex] + data[i]) * scaleFactor;
							}
							_count = 0;
						}
					}
					else
					{
						for (int i = 0; i < length; i++)
						{
							_buffer[i + _bufferIndex] = data[i];
							data[i] = 0f;
						}
					}
					if (_count == 0)
					{
						_bufferIndex += length;
						_lastLength = length;
					}

					if (length < data.Length)
					{
						_overflowCount++;
						Debug.LogWarning("[AVProMovieCapture] Audio buffer overflow, may cause sync issues.  Disable this component if not recording Unity audio.");
					}
				}

			}
		}
	}
}