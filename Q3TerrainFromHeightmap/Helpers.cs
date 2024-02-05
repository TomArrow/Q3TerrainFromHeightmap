using ImageMagick;
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
        struct AverageHelper
        {
            public double total;
            public double divider;
        }

        public static Image scaleImage(Image orig, int maxWidth, int maxHeight, bool hqGauss=true)
        {

            int width = orig.Width;
            int height = orig.Height;
            if (width < maxWidth)
            {
                height = height * maxWidth / width;
                width = maxWidth;
            }
            if (height < maxHeight)
            {
                width = width * maxHeight / height;
                height = maxHeight;
            }
            if (width > maxWidth)
            {
                height = height * maxWidth / width;
                width = maxWidth;
            }
            if (height > maxHeight)
            {
                width = width * maxHeight / height;
                height = maxHeight;
            }


            if (height == orig.Height && width == orig.Width)
            {
                return orig;
            }

            if (!hqGauss)
            {
                Bitmap clone = new Bitmap(width, height,
                   System.Drawing.Imaging.PixelFormat.Format48bppRgb);
                using (Graphics gr = Graphics.FromImage(clone))
                {
                    gr.Clear(Color.White);
                    gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gr.SmoothingMode = SmoothingMode.HighQuality;
                    gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gr.CompositingQuality = CompositingQuality.HighQuality;
                    gr.DrawImage(orig, new Rectangle(0, 0, width, height));
                    orig.Dispose();
                }

                return clone;
            }

            ShortImage srcImage = Helpers.BitmapToShortArray((Bitmap)orig);

            Bitmap clone2 = new Bitmap(width, height,
                System.Drawing.Imaging.PixelFormat.Format48bppRgb);
            ShortImage destImage = Helpers.BitmapToShortArray(clone2);
            clone2.Dispose();

            int pixMult = 3;

            switch (orig.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    pixMult = 1;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
                    pixMult = 4;
                    break;
            }

            AverageHelper[,] outputData = new AverageHelper[width, height];

            float scaleRatio = (float)width / (float)orig.Width;
            float radiusToSet = scaleRatio; //1.0f / scaleRatio;

            float oneDiagonal = (float)Math.Sqrt(2.0f);

            //Parallel.For(0, width, (x) =>
            //{

            for (int x = 0; x < orig.Width; x++)
            {
                float targetX = x * scaleRatio;

                int targetXMin = Math.Max(0,(int)(Math.Floor(targetX-radiusToSet)+0.5f));
                int targetXMax = Math.Min(width-1,(int)(Math.Ceiling(targetX+radiusToSet)+0.5f));

                for (int y = 0; y < orig.Height; y++)
                {
                    float targetY = y * scaleRatio;

                    int targetYMin = Math.Max(0, (int)(Math.Floor(targetY - radiusToSet) + 0.5f));
                    int targetYMax = Math.Min(height - 1, (int)(Math.Ceiling(targetY + radiusToSet) + 0.5f));

                    for(int xTarget = targetXMin; xTarget <= targetXMax; xTarget++)
                    {
                        for (int yTarget = targetYMin; yTarget <= targetYMax; yTarget++)
                        {
                            float xDist = Math.Abs((float)xTarget/ scaleRatio- (float)x);
                            float yDist = Math.Abs((float)yTarget / scaleRatio- (float)y);
                            float distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist)*Math.Min(1.0f,scaleRatio);
                            float weight = (float)Math.Exp(-Math.Pow((distance/oneDiagonal * 2.0f), 2.0f));
                            //float xDist = Math.Abs(xTarget - targetX);
                            //float yDist = Math.Abs(yTarget - targetY);
                            //float distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);
                            //float weight = (float)Math.Exp(-Math.Pow((distance/oneDiagonal * 2.0f), 2.0f));
                            outputData[xTarget, yTarget].total += (float)srcImage.imageData[y * (srcImage.stride / 2) + x * pixMult] * weight;
                            outputData[xTarget, yTarget].divider += weight;
                        }
                    }

                }
            }
            //});
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    destImage.imageData[y * (destImage.stride / 2) + x * 3] = (ushort)(float)(outputData[x, y].total / outputData[x, y].divider);
                    destImage.imageData[y * (destImage.stride / 2) + x * 3+1] = (ushort)(float)(outputData[x, y].total / outputData[x, y].divider);
                    destImage.imageData[y * (destImage.stride / 2) + x * 3+2] = (ushort)(float)(outputData[x, y].total / outputData[x, y].divider);
                }
            }

            orig.Dispose();
            return Helpers.ShortArrayToBitmap(destImage);


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
        
        public unsafe static Bitmap ShortArrayToBitmap(ShortImage byteImage)
        {
            Bitmap myBitmap = new Bitmap(byteImage.width, byteImage.height, byteImage.pixelFormat);
            Rectangle rect = new Rectangle(0, 0, myBitmap.Width, myBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                myBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                myBitmap.PixelFormat);

            bmpData.Stride = byteImage.stride;

            IntPtr ptr = bmpData.Scan0;
            int bytes = byteImage.stride * byteImage.height;

            // Copy the RGB values into the array.
            fixed (ushort* vals = byteImage.imageData)
            {
                Buffer.MemoryCopy((void*)vals, (void*)ptr, bytes, bytes);
            }

            myBitmap.UnlockBits(bmpData);
            return myBitmap;

        }
        public unsafe static Bitmap TiffAsImage(string filename)
        {
            //TiffFileReader reader = TiffFileReader.Open(filename);
            //var decoder = reader.CreateImageDecoder();
            //decoder.
            // Load pixel values from a 16-bit TIF using ImageMagick (Q16)
            MagickImage image = new MagickImage(filename);
            ushort[] pixelValues = image.GetPixels().GetValues();

            Bitmap myBitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format48bppRgb);
            Rectangle rect = new Rectangle(0, 0, myBitmap.Width, myBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                myBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                myBitmap.PixelFormat);

            bmpData.Stride = 2*3*image.Width;

            IntPtr ptr = bmpData.Scan0;
            int bytes = bmpData.Stride * image.Height;

            // Copy the RGB values into the array.
            fixed (ushort* vals = pixelValues)
            {
                Buffer.MemoryCopy((void*)vals, (void*)ptr, bytes, bytes);
            }

            myBitmap.UnlockBits(bmpData);
            return myBitmap;

        }
    }
}
