﻿using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;

namespace Q3TerrainFromHeightmap
{
    class Program
    {
        static void Main(string[] args)
        {
            Bitmap img = getImage(args[0]);
            doit(img);
        }

        static Bitmap getImage(string filename)
        {
            Bitmap bmp = new Bitmap(filename);
            return bmp;
        }

        static void doit(Bitmap img, int maxBrushes = 30000)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{\n\"classname\" \"worldspawn\"");

            int imgXRes = img.Width;
            int imgYRes = img.Height;
            float imgRatio = imgXRes / imgYRes;

            // (xRes/imgRatio)*xRes = 30000
            // xRes^2 / imgRatio = 30000
            // xRes^2 = 30000*imgRatio
            // xRes = sqrt(3000*imgRatio)

            int xRes = (int)((float)Math.Floor(Math.Sqrt((float)maxBrushes * imgRatio)) + 0.5f);
            int yRes = (int)(Math.Floor((float)xRes / imgRatio) + 0.5f);

            //int xRes = 170;
            //int yRes = 170;
            float totalSizeX = 130000;
            float totalSizeY = totalSizeX/imgRatio;
            if(totalSizeY > 130000.0f)
            {
                totalSizeY = 130000.0f;
                totalSizeX = totalSizeY * imgRatio;
            }

            Bitmap imgScaled = Helpers.scaleImage(img, xRes, yRes);
            imgScaled.Save("testscaled.png");
            img.Dispose();

            ShortImage shortImage = Helpers.BitmapToShortArray(imgScaled);

            float heightScale = 1000;

            float tileWidth = totalSizeX / (float)xRes;
            float tileHeight = totalSizeY / (float)yRes;

            float[,] heights = new float[xRes, yRes];

            Random rnd = new Random();

            for(int x = 0; x < xRes; x++)
            {
                for (int y = 0; y < yRes; y++)
                {
                    //heights[x,y] = (float)rnd.NextDouble()* heightScale;
                    //heights[x,y] = (float)shortImage.imageData[y*(shortImage.stride/2)+x*3];
                    heights[x,(yRes-1-y)] = (float)shortImage.imageData[y*(shortImage.stride/2)+x*3];
                }
            }

            imgScaled.Dispose();

            float startX = -totalSizeX / 2.0f;
            float startY = -totalSizeY / 2.0f;

            Vector3[] points = new Vector3[8];

            for (int x = 0; x < xRes-1; x++)
            {
                for (int y = 0; y < yRes-1; y++)
                {
                    sb.Append("\n{\nbrushDef\n{");

                    // The comments are from bird's eye perspective
                    points[0] = new Vector3() { X= startX + x*tileWidth, Y= startY+y*tileHeight, Z = heights[x,y] }; // Left upper
                    points[1] = new Vector3() { X= startX + (x+1)*tileWidth, Y= startY+y*tileHeight, Z = heights[x+1,y] }; // Right upper
                    points[2] = new Vector3() { X= startX + (x+1)*tileWidth, Y= startY+(y+1)*tileHeight, Z = heights[x+1,y+1] }; // right lower
                    points[3] = new Vector3() { X= startX + x*tileWidth, Y= startY+(y+1)*tileHeight, Z = heights[x,y+1] }; // left lower

                    for (int i = 0; i < 4; i++)
                    {
                        points[i + 4] = points[i];
                        points[i+4].Z = -100;
                    }

                    // Z Top planes
                    Vector3 mid1 = (points[0] + points[2]) / 2.0f;
                    Vector3 mid2 = (points[1] + points[3]) / 2.0f;
                    if(mid1.Z >= mid2.Z)
                    {
                        sb.Append(ToBrushText(points[0], points[1], points[2]));
                        sb.Append(ToBrushText(points[0], points[2], points[3]));
                    } else
                    {
                        sb.Append(ToBrushText(points[1], points[2], points[3]));
                        sb.Append(ToBrushText(points[1], points[3], points[0]));
                    }

                    // Z Bottom plane
                    sb.Append(ToBrushText(points[4+2], points[4+1], points[4+0]));

                    // Side planes (bird's eye perspective)
                    sb.Append(ToBrushText(points[0], points[0+4], points[1+4])); // Top plane
                    sb.Append(ToBrushText(points[1], points[1+4], points[2+4])); // Right plane
                    sb.Append(ToBrushText(points[2], points[2+4], points[3+4])); // Bottom plane
                    sb.Append(ToBrushText(points[3], points[3+4], points[0+4])); // Left plane


                    sb.Append("\n}\n}");
                }
            }

            sb.Append("\n}");

            File.WriteAllText("test.map",sb.ToString());
        } 
        
        
        static void doitRandom()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{\n\"classname\" \"worldspawn\"");


            int xRes = 170;
            int yRes = 170;
            float totalSizeX = 130000;
            float totalSizeY =  130000;
            float heightScale = 1000;

            float tileWidth = totalSizeX / (float)xRes;
            float tileHeight = totalSizeY / (float)yRes;

            float[,] heights = new float[xRes, yRes];

            Random rnd = new Random();

            for(int x = 0; x < xRes; x++)
            {
                for (int y = 0; y < yRes; y++)
                {
                    heights[x,y] = (float)rnd.NextDouble()* heightScale;
                }
            }

            float startX = -totalSizeX / 2.0f;
            float startY = -totalSizeY / 2.0f;

            Vector3[] points = new Vector3[8];

            for (int x = 0; x < xRes-1; x++)
            {
                for (int y = 0; y < yRes-1; y++)
                {
                    sb.Append("\n{\nbrushDef\n{");

                    // The comments are from bird's eye perspective
                    points[0] = new Vector3() { X= startX + x*tileWidth, Y= startY+y*tileHeight, Z = heights[x,y] }; // Left upper
                    points[1] = new Vector3() { X= startX + (x+1)*tileWidth, Y= startY+y*tileHeight, Z = heights[x+1,y] }; // Right upper
                    points[2] = new Vector3() { X= startX + (x+1)*tileWidth, Y= startY+(y+1)*tileHeight, Z = heights[x+1,y+1] }; // right lower
                    points[3] = new Vector3() { X= startX + x*tileWidth, Y= startY+(y+1)*tileHeight, Z = heights[x,y+1] }; // left lower

                    for (int i = 0; i < 4; i++)
                    {
                        points[i + 4] = points[i];
                        points[i+4].Z = -100;
                    }

                    // Z Top planes
                    Vector3 mid1 = (points[0] + points[2]) / 2.0f;
                    Vector3 mid2 = (points[1] + points[3]) / 2.0f;
                    if(mid1.Z >= mid2.Z)
                    {
                        sb.Append(ToBrushText(points[0], points[1], points[2]));
                        sb.Append(ToBrushText(points[0], points[2], points[3]));
                    } else
                    {
                        sb.Append(ToBrushText(points[1], points[2], points[3]));
                        sb.Append(ToBrushText(points[1], points[3], points[0]));
                    }

                    // Z Bottom plane
                    sb.Append(ToBrushText(points[4+2], points[4+1], points[4+0]));

                    // Side planes (bird's eye perspective)
                    sb.Append(ToBrushText(points[0], points[0+4], points[1+4])); // Top plane
                    sb.Append(ToBrushText(points[1], points[1+4], points[2+4])); // Right plane
                    sb.Append(ToBrushText(points[2], points[2+4], points[3+4])); // Bottom plane
                    sb.Append(ToBrushText(points[3], points[3+4], points[0+4])); // Left plane


                    sb.Append("\n}\n}");
                }
            }

            sb.Append("\n}");

            File.WriteAllText("test.map",sb.ToString());
        }

        static string ToBrushText(Vector3 a, Vector3 b, Vector3 c)
        {
            StringBuilder sb = new StringBuilder();

            (a, c) = (c, a);

            sb.Append($"\n( ");
            sb.Append(a.X.ToString("0.###"));
            sb.Append(" ");
            sb.Append(a.Y.ToString("0.###"));
            sb.Append(" ");
            sb.Append(a.Z.ToString("0.###"));
            sb.Append($" ) ( ");
            sb.Append(b.X.ToString("0.###"));
            sb.Append(" ");
            sb.Append(b.Y.ToString("0.###"));
            sb.Append(" ");
            sb.Append(b.Z.ToString("0.###"));
            sb.Append($" ) ( ");
            sb.Append(c.X.ToString("0.###"));
            sb.Append(" ");
            sb.Append(c.Y.ToString("0.###"));
            sb.Append(" ");
            sb.Append(c.Z.ToString("0.###"));
            sb.Append($" ) ( ( 0 0 0 ) ( 0 0 0 ) ) system/caulk 0 0 0");

            return sb.ToString();
        }
    }
}
