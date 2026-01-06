using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Thry.ThryEditor.Helpers
{
	public class GifDecoder
	{
		public struct GifFrame
		{
			public Color32[] Pixels;
			public int Width;
			public int Height;
			public int Delay;
		}

		byte[] _data;
		int _pos;
		int _width;
		int _height;
		Color32[] _globalColorTable;
		Color32[] _canvas;
		Color32[] _previousCanvas;
		int _bgColorIndex;
		int _lastDisposalMethod;

		public int Width => _width;
		public int Height => _height;

		public List<GifFrame> Decode(string path)
		{
			_data = File.ReadAllBytes(path);
			_pos = 0;
			return DecodeInternal();
		}

		public List<GifFrame> Decode(byte[] data)
		{
			_data = data;
			_pos = 0;
			return DecodeInternal();
		}

		List<GifFrame> DecodeInternal()
		{
			var frames = new List<GifFrame>();

			if (!ReadHeader())
				return frames;

			if (!ReadLogicalScreenDescriptor())
				return frames;

			_canvas = new Color32[_width * _height];
			_previousCanvas = new Color32[_width * _height];
			for (int i = 0; i < _canvas.Length; i++)
				_canvas[i] = new Color32(0, 0, 0, 0);

			int transparentIndex = -1;
			int disposalMethod = 0;
			int delay = 0;

			while (_pos < _data.Length)
			{
				byte blockType = ReadByte();

				switch (blockType)
				{
					case 0x21: // Extension
						byte extType = ReadByte();
						switch (extType)
						{
							case 0xF9: // Graphic Control Extension
								ReadGraphicControlExtension(out transparentIndex, out disposalMethod, out delay);
								break;
							case 0xFF: // Application Extension
							case 0xFE: // Comment Extension
							case 0x01: // Plain Text Extension
								SkipSubBlocks();
								break;
							default:
								SkipSubBlocks();
								break;
						}
						break;

					case 0x2C: // Image Descriptor
						var frame = ReadImageDescriptor(transparentIndex, disposalMethod, delay);
						if (frame.Pixels != null)
							frames.Add(frame);
						_lastDisposalMethod = disposalMethod;
						transparentIndex = -1;
						disposalMethod = 0;
						delay = 0;
						break;

					case 0x3B: // Trailer
						return frames;

					default:
						return frames;
				}
			}

			return frames;
		}

		bool ReadHeader()
		{
			if (_data.Length < 6)
				return false;

			string header = "" + (char)_data[0] + (char)_data[1] + (char)_data[2];
			string version = "" + (char)_data[3] + (char)_data[4] + (char)_data[5];
			_pos = 6;

			return header == "GIF" && (version == "87a" || version == "89a");
		}

		bool ReadLogicalScreenDescriptor()
		{
			if (_pos + 7 > _data.Length)
				return false;

			_width = ReadUInt16();
			_height = ReadUInt16();

			byte packed = ReadByte();
			bool hasGlobalColorTable = (packed & 0x80) != 0;
			int colorResolution = ((packed >> 4) & 0x07) + 1;
			bool sortFlag = (packed & 0x08) != 0;
			int globalColorTableSize = 1 << ((packed & 0x07) + 1);

			_bgColorIndex = ReadByte();
			byte pixelAspectRatio = ReadByte();

			if (hasGlobalColorTable)
				_globalColorTable = ReadColorTable(globalColorTableSize);

			return true;
		}

		Color32[] ReadColorTable(int count)
		{
			var table = new Color32[count];
			for (int i = 0; i < count; i++)
			{
				byte r = ReadByte();
				byte g = ReadByte();
				byte b = ReadByte();
				table[i] = new Color32(r, g, b, 255);
			}
			return table;
		}

		void ReadGraphicControlExtension(out int transparentIndex, out int disposalMethod, out int delay)
		{
			byte blockSize = ReadByte();
			byte packed = ReadByte();
			disposalMethod = (packed >> 2) & 0x07;
			bool userInput = (packed & 0x02) != 0;
			bool hasTransparency = (packed & 0x01) != 0;

			delay = ReadUInt16();
			transparentIndex = hasTransparency ? ReadByte() : -1;
			if (!hasTransparency)
				_pos++;

			ReadByte(); // Block terminator
		}

		GifFrame ReadImageDescriptor(int transparentIndex, int disposalMethod, int delay)
		{
			int left = ReadUInt16();
			int top = ReadUInt16();
			int width = ReadUInt16();
			int height = ReadUInt16();

			byte packed = ReadByte();
			bool hasLocalColorTable = (packed & 0x80) != 0;
			bool interlaced = (packed & 0x40) != 0;
			bool sortFlag = (packed & 0x20) != 0;
			int localColorTableSize = 1 << ((packed & 0x07) + 1);

			Color32[] colorTable = _globalColorTable;
			if (hasLocalColorTable)
				colorTable = ReadColorTable(localColorTableSize);

			if (colorTable == null)
				return default;

			// Handle disposal from previous frame
			HandleDisposal(_lastDisposalMethod);

			// Decode LZW image data
			byte[] indices = DecodeLZW(width, height);
			if (indices == null)
				return default;

			// Render frame to canvas
			RenderFrame(indices, left, top, width, height, colorTable, transparentIndex, interlaced);

			// Copy canvas to frame
			var frame = new GifFrame
			{
				Pixels = new Color32[_width * _height],
				Width = _width,
				Height = _height,
				Delay = delay
			};
			Array.Copy(_canvas, frame.Pixels, _canvas.Length);

			return frame;
		}

		void HandleDisposal(int disposalMethod)
		{
			switch (disposalMethod)
			{
				case 0: // No disposal
				case 1: // Do not dispose
					Array.Copy(_canvas, _previousCanvas, _canvas.Length);
					break;
				case 2: // Restore to background
					for (int i = 0; i < _canvas.Length; i++)
						_canvas[i] = new Color32(0, 0, 0, 0);
					break;
				case 3: // Restore to previous
					Array.Copy(_previousCanvas, _canvas, _canvas.Length);
					break;
			}
		}

		void RenderFrame(byte[] indices, int left, int top, int width, int height, Color32[] colorTable, int transparentIndex, bool interlaced)
		{
			int[] interlaceStarts = { 0, 4, 2, 1 };
			int[] interlaceSteps = { 8, 8, 4, 2 };

			int srcIdx = 0;
			if (interlaced)
			{
				for (int pass = 0; pass < 4; pass++)
				{
					for (int y = interlaceStarts[pass]; y < height; y += interlaceSteps[pass])
					{
						for (int x = 0; x < width; x++)
						{
							int idx = indices[srcIdx++];
							if (idx != transparentIndex)
							{
								int canvasX = left + x;
								int canvasY = top + y;
								if (canvasX < _width && canvasY < _height)
									_canvas[canvasY * _width + canvasX] = colorTable[idx];
							}
						}
					}
				}
			}
			else
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						int idx = indices[srcIdx++];
						if (idx != transparentIndex)
						{
							int canvasX = left + x;
							int canvasY = top + y;
							if (canvasX < _width && canvasY < _height)
								_canvas[canvasY * _width + canvasX] = colorTable[idx];
						}
					}
				}
			}
		}

		byte[] DecodeLZW(int width, int height)
		{
			int minCodeSize = ReadByte();
			if (minCodeSize < 2 || minCodeSize > 12)
				return null;

			// Read all sub-blocks into a single buffer
			var compressedData = new List<byte>();
			while (true)
			{
				int blockSize = ReadByte();
				if (blockSize == 0)
					break;
				for (int i = 0; i < blockSize && _pos < _data.Length; i++)
					compressedData.Add(ReadByte());
			}

			return LzwDecode(compressedData.ToArray(), minCodeSize, width * height);
		}

		byte[] LzwDecode(byte[] compressed, int minCodeSize, int pixelCount)
		{
			int clearCode = 1 << minCodeSize;
			int endCode = clearCode + 1;
			int codeSize = minCodeSize + 1;
			int nextCode = endCode + 1;
			int codeMask = (1 << codeSize) - 1;

			// Initialize dictionary
			var dictionary = new List<byte[]>();
			for (int i = 0; i < clearCode; i++)
				dictionary.Add(new byte[] { (byte)i });
			dictionary.Add(null); // Clear code
			dictionary.Add(null); // End code

			var output = new List<byte>();
			int bitBuffer = 0;
			int bitsInBuffer = 0;
			int dataIndex = 0;

			int prevCode = -1;

			while (output.Count < pixelCount && dataIndex < compressed.Length)
			{
				// Read next code
				while (bitsInBuffer < codeSize && dataIndex < compressed.Length)
				{
					bitBuffer |= compressed[dataIndex++] << bitsInBuffer;
					bitsInBuffer += 8;
				}

				int code = bitBuffer & codeMask;
				bitBuffer >>= codeSize;
				bitsInBuffer -= codeSize;

				if (code == endCode)
					break;

				if (code == clearCode)
				{
					codeSize = minCodeSize + 1;
					codeMask = (1 << codeSize) - 1;
					nextCode = endCode + 1;
					dictionary.RemoveRange(nextCode, dictionary.Count - nextCode);
					prevCode = -1;
					continue;
				}

				byte[] entry;
				if (code < dictionary.Count && dictionary[code] != null)
				{
					entry = dictionary[code];
				}
				else if (code == nextCode && prevCode >= 0)
				{
					var prev = dictionary[prevCode];
					entry = new byte[prev.Length + 1];
					Array.Copy(prev, entry, prev.Length);
					entry[prev.Length] = prev[0];
				}
				else
				{
					break; // Invalid code
				}

				for (int i = 0; i < entry.Length && output.Count < pixelCount; i++)
					output.Add(entry[i]);

				if (prevCode >= 0 && nextCode < 4096)
				{
					var prev = dictionary[prevCode];
					var newEntry = new byte[prev.Length + 1];
					Array.Copy(prev, newEntry, prev.Length);
					newEntry[prev.Length] = entry[0];
					dictionary.Add(newEntry);
					nextCode++;

					if (nextCode > codeMask && codeSize < 12)
					{
						codeSize++;
						codeMask = (1 << codeSize) - 1;
					}
				}

				prevCode = code;
			}

			return output.ToArray();
		}

		void SkipSubBlocks()
		{
			while (_pos < _data.Length)
			{
				int size = ReadByte();
				if (size == 0)
					break;
				_pos += size;
			}
		}

		byte ReadByte()
		{
			return _pos < _data.Length ? _data[_pos++] : (byte)0;
		}

		int ReadUInt16()
		{
			int low = ReadByte();
			int high = ReadByte();
			return low | (high << 8);
		}
	}
}

