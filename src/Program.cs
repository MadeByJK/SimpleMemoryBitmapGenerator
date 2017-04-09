using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleMemoryBitmapGenerator.Models;

namespace SimpleMemoryBitmapGenerator
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Save(ParseBitmaps(ReadSettings()));
		}

		private static void Save(List<BinaryBitmap> list)
		{
			var dialog = new SaveFileDialog();
			dialog.AddExtension = true;
			dialog.Filter = "C header|*.h|All files|*.*";

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				using (var file = new StreamWriter(dialog.FileName))
				{
					var buffer = new StringBuilder();

					var filename = Path.GetFileNameWithoutExtension(dialog.FileName);
					buffer.AppendLine($"#ifndef {filename}_h");
					buffer.AppendLine($"#define {filename}_h");

					int i = 0;

					foreach (var bmp in list)
					{
						buffer.AppendLine($"#define {bmp.Name.ToUpper()} {i++}");
					}

					buffer.AppendLine($"const PROGMEM uint8_t images[][{list[0].Pixels.Count}] = {{");
					foreach (var bmp in list)
					{
						buffer.AppendLine("{");

						foreach (var item in bmp.Pixels)
						{
							buffer.Append($"0x{item.ToString("X2")}, ");
						}
						buffer.Length -= 2;

						buffer.AppendLine("},");
					}

					buffer.Length -= 1;
					buffer.AppendLine("};");
					buffer.AppendLine($"#endif");

					file.Write(buffer.ToString());
				}
			}

		}

		private static List<BinaryBitmap> ParseBitmaps(Settings settings)
		{
			var files = Directory.GetFiles(settings.Path, "*.bmp");
			var bitmaps = new List<BinaryBitmap>();

			foreach (var file in files)
			{
				using (var bmp = Bitmap.FromFile(file) as Bitmap)
				{
					var list = new List<byte>();
					var data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, bmp.PixelFormat);
					var bb = new BinaryBitmap();
					var pixels = new byte[data.Height * data.Stride];

					bitmaps.Add(bb);
					bb.Size = bmp.Size;
					bb.Name = Path.GetFileNameWithoutExtension(file);
					bb.Pixels = list;
					Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
					pixels = RemoveExtra(pixels,bb.Size.Width,data.Stride);
					bmp.UnlockBits(data);

					var max = pixels.Max();
					for (int i = 0; i < (pixels.Length / 8) + (pixels.Length % 8 > 0 ? 1 : 0); i++)
					{
						byte val = 0;

						for (int j = 0; j < 8; j++)
						{
							if (i * 8 + j > pixels.Length - 1)
							{
								break;
							}
							
							val |= (byte) ((pixels[i * 8 + j] > max / 2 ? 0 : 1) << (7-j));
						}

						list.Add(val);
					}
				}
			}

			return bitmaps;
		}

		private static byte[] RemoveExtra(byte[] pixels, int width, int stride)
		{
			var result = new byte[pixels.Length * width / stride];

			for (int i = 0; i<pixels.Length/stride;i++)
			{
				for (int j = 0; j < stride; j++)
				{
					if(j<width)
					{
						result[i * width + j] = pixels[i * stride + j];
					}
				}
			}

			return result;
		}

		private static Settings ReadSettings()
		{
			var settings = new Settings();
			var dialog = new FolderBrowserDialog();

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				settings.Path = dialog.SelectedPath;
			}
			else
			{
				Application.Exit();
			}

			return settings;
		}
	}
}
