using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using turbojpegCLI;

namespace UI2Service
{
	public class JpegDiffVideoDecoder : IDisposable
	{
		public static int[] Versions = new int[] { 1, 2, 3, 4 };
		TJDecompressor decomp;
		private byte[] previousFrameData = null;
		private byte[] currentFrameData = null;
		private int frameWidth = -1;
		private int frameHeight = -1;
		public int Width
		{
			get { return frameWidth; }
		}
		public int Height
		{
			get { return frameHeight; }
		}
		bool isDisposed = false;

		public JpegDiffVideoDecoder()
		{
			decomp = new TJDecompressor();
		}

		#region IDisposable Members

		/// <summary>
		/// Call this when finished with the instance. If using a C# using block, you won't need to call this.
		/// </summary>
		public void Dispose()
		{
			try
			{
				lock (this)
				{
					if (isDisposed)
						return;
					isDisposed = true;
					decomp.Dispose();
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Exception disposing TurboJpeg stuff in JpegDiffVideoDecoder");
			}
		}

		#endregion
		/// <summary>
		/// Accepts a JpegDiff frame and decodes it to raw RGB data.  This instance reserves ownership of the returned byte array, and may return the same byte array again later after changing its content.  You can use the byte array for any purpose until the next call to DecodeFrame on this instance.
		/// </summary>
		/// <param name="jpegDiffFrame">A buffer containing a jpegDiff frame.</param>
		/// <param name="inputSizeBytes">The length of the data in the input buffer, which may be shorter than the buffer itself.</param>
		/// <returns></returns>
		public byte[] DecodeFrame(byte[] jpegDiffFrame, int inputSizeBytes, int version)
		{
			if (!Versions.Contains(version))
				return new byte[0];
			lock (this)
			{
				if (isDisposed)
					return new byte[0];
				decomp.setSourceImage(jpegDiffFrame, inputSizeBytes);
				if (previousFrameData == null)
				{
					// Special case: First frame of video is a complete frame.  All following frames will be difference (diff) frames.
					frameWidth = decomp.getWidth();
					frameHeight = decomp.getHeight();
					previousFrameData = decomp.decompress();
					return previousFrameData;
				}
				if (decomp.getWidth() != frameWidth || decomp.getHeight() != frameHeight)
					throw new JpegDiffVideoException("Diff frame dimensions (" + decomp.getWidth() + "x" + decomp.getHeight() + ") do not match the first frame (" + frameWidth + "x" + frameHeight + ")");

				if (currentFrameData == null)
					currentFrameData = new byte[previousFrameData.Length];

				decomp.decompress(currentFrameData);

				// Combine this diff frame with the previous frame.
				int[] decoderArray;
				if (version == 1)
					decoderArray = decoderArrayV1;
				else if (version == 2)
					decoderArray = decoderArrayV2;
				else if (version == 3)
					decoderArray = decoderArrayV3;
				else if (version == 4)
					decoderArray = decoderArrayV4;
				else
					throw new Exception("Invalid version number specified");

				for (int i = 0; i < currentFrameData.Length; i++)
				{
					int newVal = previousFrameData[i] + decoderArray[currentFrameData[i]];
					if (newVal < 0)
						previousFrameData[i] = 0;
					else if (newVal > 255)
						previousFrameData[i] = 255;
					else
						previousFrameData[i] = (byte)newVal;
				}
				return previousFrameData;
			}
		}
		private static int Clamp(int i, int min, int max)
		{
			if (i < min)
				return min;
			if (i > max)
				return max;
			return i;
		}
		private static int[] decoderArrayV1 = new int[] { -128, -127, -126, -125, -124, -123, -122, -121, -120, -119, -118, -117, -116, -115, -114, -113, -112, -111, -110, -109, -108, -107, -106, -105, -104, -103, -102, -101, -100, -99, -98, -97, -96, -95, -94, -93, -92, -91, -90, -89, -88, -87, -86, -85, -84, -83, -82, -81, -80, -79, -78, -77, -76, -75, -74, -73, -72, -71, -70, -69, -68, -67, -66, -65, -64, -63, -62, -61, -60, -59, -58, -57, -56, -55, -54, -53, -52, -51, -50, -49, -48, -47, -46, -45, -44, -43, -42, -41, -40, -39, -38, -37, -36, -35, -34, -33, -32, -31, -30, -29, -28, -27, -26, -25, -24, -23, -22, -21, -20, -19, -18, -17, -16, -15, -14, -13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127 };
		private static int[] decoderArrayV2 = new int[] { -255, -253, -251, -249, -247, -245, -243, -241, -239, -237, -235, -233, -231, -229, -227, -225, -223, -221, -219, -217, -215, -213, -211, -209, -207, -205, -203, -201, -199, -197, -195, -193, -191, -189, -187, -185, -183, -181, -179, -177, -175, -173, -171, -169, -167, -165, -163, -161, -159, -157, -155, -153, -151, -149, -147, -145, -143, -141, -139, -137, -135, -133, -131, -129, -127, -125, -123, -121, -119, -117, -115, -113, -111, -109, -107, -105, -103, -101, -99, -97, -95, -93, -91, -89, -87, -85, -83, -81, -79, -77, -75, -73, -71, -69, -67, -65, -63, -61, -59, -57, -55, -53, -51, -49, -47, -45, -43, -41, -39, -37, -35, -33, -31, -29, -27, -25, -23, -21, -19, -17, -15, -13, -11, -9, -7, -5, -3, -1, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 53, 55, 57, 59, 61, 63, 65, 67, 69, 71, 73, 75, 77, 79, 81, 83, 85, 87, 89, 91, 93, 95, 97, 99, 101, 103, 105, 107, 109, 111, 113, 115, 117, 119, 121, 123, 125, 127, 129, 131, 133, 135, 137, 139, 141, 143, 145, 147, 149, 151, 153, 155, 157, 159, 161, 163, 165, 167, 169, 171, 173, 175, 177, 179, 181, 183, 185, 187, 189, 191, 193, 195, 197, 199, 201, 203, 205, 207, 209, 211, 213, 215, 217, 219, 221, 223, 225, 227, 229, 231, 233, 235, 237, 239, 241, 243, 245, 247, 249, 251, 253, 255 };
		private static int[] decoderArrayV3 = new int[] { -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -255, -251, -243, -235, -227, -219, -211, -203, -195, -187, -179, -171, -163, -155, -147, -139, -131, -125, -122, -117, -114, -109, -106, -101, -98, -93, -90, -85, -82, -77, -74, -69, -66, -62, -59, -56, -53, -50, -47, -44, -41, -38, -35, -32, -29, -26, -23, -20, -17, -15, -14, -13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57, 60, 63, 67, 70, 75, 78, 83, 86, 91, 94, 99, 102, 107, 110, 115, 118, 123, 126, 132, 140, 148, 156, 164, 172, 180, 188, 196, 204, 212, 220, 228, 236, 244, 252, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
		private static int[] decoderArrayV4 = new int[] { -255, -253, -250, -247, -244, -241, -238, -235, -232, -229, -226, -223, -220, -217, -214, -211, -208, -205, -202, -199, -196, -193, -190, -187, -184, -181, -178, -175, -172, -169, -166, -163, -160, -156, -153, -150, -147, -144, -141, -138, -135, -132, -129, -126, -123, -120, -117, -114, -111, -108, -105, -102, -99, -96, -93, -90, -87, -84, -81, -78, -75, -72, -69, -66, -64, -63, -62, -61, -60, -59, -58, -57, -56, -55, -54, -53, -52, -51, -50, -49, -48, -47, -46, -45, -44, -43, -42, -41, -40, -39, -38, -37, -36, -35, -34, -33, -32, -31, -30, -29, -28, -27, -26, -25, -24, -23, -22, -21, -20, -19, -18, -17, -16, -15, -14, -13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 67, 70, 73, 76, 79, 82, 85, 88, 92, 95, 98, 101, 104, 107, 110, 113, 116, 119, 122, 125, 128, 131, 134, 138, 141, 144, 147, 150, 153, 156, 159, 162, 165, 168, 171, 174, 177, 180, 183, 187, 190, 193, 196, 199, 202, 205, 208, 211, 214, 217, 220, 223, 226, 229, 233, 236, 239, 242, 245, 248, 251, 254 };
	}
}
