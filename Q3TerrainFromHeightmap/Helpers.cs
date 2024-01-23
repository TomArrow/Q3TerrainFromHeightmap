using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Q3TerrainFromHeightmap
{
    static class Helpers
    {
        public static Bitmap scaleImage(Bitmap orig, int maxWidth, int maxHeight)
        {

            int width = orig.Width;
            int height = orig.Height;
            if(width > maxWidth)
            {
                height = height * maxWidth / width;
                width = maxWidth;
            }
            if(height > maxHeight)
            {
                width = width * maxHeight / height;
                height = maxHeight;
            }

            Bitmap clone = new Bitmap(width, height,
                orig.PixelFormat);

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.Clear(Color.White);
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.CompositingQuality = CompositingQuality.HighQuality;
                gr.DrawImage(orig, new Rectangle(0,0,width,height));

            }

            return clone;
        }

        public unsafe static ShortImage BitmapToShortArray(Bitmap bmp)
        {

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int stride = Math.Abs(bmpData.Stride);
            int bytes = stride * bmp.Height;
            ushort[] rgbValues = new ushort[(bytes/2)+1]; // +1 just to be safe in case bytes is not divisible by 2 lol

            // Copy the RGB values into the array.
            fixed (ushort* vals = rgbValues)
            {
                Buffer.MemoryCopy((void*)ptr, vals, bytes, bytes);
            }

            bmp.UnlockBits(bmpData);

            return new ShortImage(rgbValues, stride, bmp.Width, bmp.Height, bmp.PixelFormat);
        }
        /*
        public static Bitmap ByteArrayToBitmap(ByteImage byteImage)
        {
            Bitmap myBitmap = new Bitmap(byteImage.width, byteImage.height, byteImage.pixelFormat);
            Rectangle rect = new Rectangle(0, 0, myBitmap.Width, myBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                myBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                myBitmap.PixelFormat);

            bmpData.Stride = byteImage.stride;

            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(byteImage.imageData, 0, ptr, byteImage.imageData.Length);

            myBitmap.UnlockBits(bmpData);
            return myBitmap;

        }*/
    }
}
