using System.Collections.Generic;
using System.Drawing;

namespace SimpleMemoryBitmapGenerator.Models
{
	internal class BinaryBitmap
	{
		public string Name { get; internal set; }
		public List<byte> Pixels { get; internal set; }
		public Size Size { get; internal set; }
	}
}