using AssetRipper.Library.Configuration;
using AssetRipper.TextureDecoder.Rgb.Formats;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AssetRipper.Library.Utils
{
	public sealed partial class DirectBitmap : IDisposable
	{
		public int Height { get; }
		public int Width { get; }
		public int Stride => Width * 4;
		public byte[] Bits { get; }
		public IntPtr BitsPtr => m_bitsHandle.AddrOfPinnedObject();

		private readonly GCHandle m_bitsHandle;
		private bool m_disposed;
		private static int PixelSize => Unsafe.SizeOf<ColorBGRA32>();

		/// <summary>
		/// Make a new bitmap
		/// </summary>
		/// <param name="width">The width of the image</param>
		/// <param name="height">The height of the image</param>
		public DirectBitmap(int width, int height) : this(width, height, new byte[width * height * PixelSize]) { }

		/// <summary>
		/// Make a bitmap from existing BGRA32 data
		/// </summary>
		/// <param name="width">The width of the image</param>
		/// <param name="height">The height of the image</param>
		/// <param name="bgra32Data">The image data, 4 bytes per pixel. Will get pinned</param>
		public DirectBitmap(int width, int height, byte[] bgra32Data)
		{
			if (bgra32Data is null)
			{
				throw new ArgumentNullException(nameof(bgra32Data));
			}

			if (bgra32Data.Length != width * height * PixelSize)
			{
				throw new ArgumentException($"Invalid length: expected {width * height * PixelSize} but was actually {bgra32Data.Length}", nameof(bgra32Data));
			}

			Width = width;
			Height = height;
			Bits = bgra32Data;
			m_bitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
		}

		public void FlipY(int depth = 1)
		{
			int actualHeight = Height / depth;
			Debug.Assert(Height % depth == 0);

			Span<ColorBGRA32> pixels = MemoryMarshal.Cast<byte, ColorBGRA32>(Bits);
			for (int startingRow = 0; startingRow < Height; startingRow += actualHeight)
			{
				int endingRow = startingRow + actualHeight - 1;
				for (int row = startingRow, irow = endingRow; row < irow; row++, irow--)
				{
					Span<ColorBGRA32> rowTop = pixels.Slice(row * Width, Width);
					Span<ColorBGRA32> rowBottom = pixels.Slice(irow * Width, Width);
					for (int i = 0; i < Width; i++)
					{
						(rowTop[i], rowBottom[i]) = (rowBottom[i], rowTop[i]);
					}
				}
			}
		}

		public bool Save(Stream stream, ImageExportFormat format)
		{
			return format switch
			{
				ImageExportFormat.Bmp => SaveAsBmp(stream),
				ImageExportFormat.Gif => SaveAsGif(stream),
				ImageExportFormat.Jpeg => SaveAsJpeg(stream),
				ImageExportFormat.Pbm => SaveAsPbm(stream),
				ImageExportFormat.Png => SaveAsPng(stream),
				ImageExportFormat.Tga => SaveAsTga(stream),
				ImageExportFormat.Tiff => SaveAsTiff(stream),
				ImageExportFormat.Webp => SaveAsWebp(stream),
				_ => throw new ArgumentOutOfRangeException(nameof(format)),
			};
		}

		public bool Save(string path, ImageExportFormat format)
		{
			using FileStream stream = File.Create(path);
			return Save(stream, format);
		}

		public async Task SaveAsync(Stream stream, ImageExportFormat format)
		{
			switch (format)
			{
				case ImageExportFormat.Bmp:
					await SaveAsBmpAsync(stream);
					break;
				case ImageExportFormat.Gif:
					await SaveAsGifAsync(stream);
					break;
				case ImageExportFormat.Jpeg:
					await SaveAsJpegAsync(stream);
					break;
				case ImageExportFormat.Pbm:
					await SaveAsPbmAsync(stream);
					break;
				case ImageExportFormat.Png:
					await SaveAsPngAsync(stream);
					break;
				case ImageExportFormat.Tga:
					await SaveAsTgaAsync(stream);
					break;
				case ImageExportFormat.Tiff:
					await SaveAsTiffAsync(stream);
					break;
				case ImageExportFormat.Webp:
					await SaveAsWebpAsync(stream);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(format));
			}
		}

		public async Task SaveAsync(string path, ImageExportFormat format)
		{
			using FileStream stream = File.Create(path);
			await SaveAsync(stream, format);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		private void Dispose(bool _)
		{
			if (!m_disposed)
			{
				m_bitsHandle.Free();
				m_disposed = true;
			}
		}

		~DirectBitmap()
		{
			Dispose(false);
		}
	}
}
