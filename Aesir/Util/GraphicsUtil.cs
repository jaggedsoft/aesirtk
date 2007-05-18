using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace Aesir.Util {
	static class GraphicsUtil {
		public static void DrawInvertedImage(Graphics graphics, Image image, Point point) {
			ImageAttributes imageAttributes = new ImageAttributes();
			ColorMatrix colorMatrix = new ColorMatrix();
			colorMatrix.Matrix00 = colorMatrix.Matrix11 = colorMatrix.Matrix22 = -1;
			imageAttributes.SetColorMatrix(colorMatrix);
			graphics.DrawImage(image, new Rectangle(point, image.Size), 0, 0, image.Width, image.Height,
				GraphicsUnit.Pixel, imageAttributes);
		}
	}
}