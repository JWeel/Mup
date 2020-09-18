using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mup.Extensions;

namespace Mup.Helpers
{
    public class Mapper
    {
        #region Constructors

        public Mapper(string filePath)
        {
            this.Imagey = new Bitmap(filePath);
        }

        #endregion

        #region Properties

        protected Bitmap Imagey { get; set; }

        #endregion

        #region Public Methods

        public void A()
        {
            var image = this.Imagey;
            var data = this.GetBytes(image);

            var pixels = Generate.Range(0, data.Length, step: 4)
                .Select(i => (Index: i / 4, Color: Color.FromArgb(data[i + 3], data[i + 2], data[i + 1], data[i])))
                .ToArray();

            var pixelPositionsByColor = pixels
                .Where(x => !x.Color.IsBlackOrWhite())
                .GroupBy(x => x.Color)
                .Select(group => (Color: group.Key, Positions: group.ToArray(x => x.Index.Into(i => (X: i % image.Width, Y: i / image.Width)))))
                .ToDictionary(x => x.Color, x => x.Positions);

            this.WriteToTextFile(pixelPositionsByColor);

            var validColors = pixels
                .Select(x => x.Color)
                .Where(x => !x.IsBlackOrWhite())
                .GroupBy(x => x)
                .SelectMany(x => x)
                .ToArray();

            var tiers = Generate.Range(0, 8)
                .Select(x => (int) Math.Pow(3, x))
                .Reverse()
                .ToArray();

            var shuffledColorSet = validColors.Shuffled()
                .Take(tiers[0] + tiers[1])
                .ToHashSet();

            var newData = pixels
                .Select(x => x.Color switch
                {
                    var color when color.IsBlackOrWhite() => Color.White,
                    var color when !shuffledColorSet.Contains(color) => Color.Black,
                    var color when true => color
                })
                .SelectMany(ColorExtensions.ToBytes)
                .ToArray();

            var image2 = this.BuildImage(newData, image.Width, image.Height);
            image2.Save(@"D:\Documents\Code\repos\zo\Zo\out.png", ImageFormat.Png);
        }

        #endregion

        #region Helper Methods

        protected void WriteToTextFile(Dictionary<Color, (int X, int Y)[]> pixelPositionsByColor)
        {
            var outFile = new FileInfo(@"D:\Documents\Code\repos\zo\Zo\out.txt");
            var outFileWriteStream = outFile.CreateText();
            pixelPositionsByColor
                .OrderBy(x => x.Value.Length)
                .Each(x => outFileWriteStream.WriteLine($"Color: {x.Key.R},{x.Key.G},{x.Key.B}, Count: {x.Value.Length}, Positions: {x.Value.Select(x => x.ToString()).Join(",")}"));
            outFileWriteStream.Flush();
        }
        
        protected byte[] GetBytes(Bitmap image)
        {
            var rectangle = new Rectangle(0, 0, image.Width, image.Height);
            var bitmapData = image.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var data = new byte[bitmapData.Stride * image.Height];
            Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
            image.UnlockBits(bitmapData);
            return data;
        }

        protected Bitmap BuildImage(byte[] sourceData, int width, int height)
        {
            var newImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var targetData = newImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newImage.PixelFormat);
            var newDataWidth = ((Image.GetPixelFormatSize(PixelFormat.Format32bppArgb) * width) + 7) / 8;
            var stride = targetData.Stride;
            var scan0 = targetData.Scan0.ToInt64();

            // note: this uses the stride (scan width) from targetData for source startIndex calculation
            // this means source (originates from method GetBytes) and target must use the same pixelformat!
            for (var y = 0; y < height; y++)
                Marshal.Copy(sourceData, y * stride, new IntPtr(scan0 + y * stride), newDataWidth);
            newImage.UnlockBits(targetData);
            return newImage;
        }

        #endregion
    }
}