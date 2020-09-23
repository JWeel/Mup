using System.Threading.Tasks;
using Mup.Extensions;
using Mup.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mup
{
    public class Mupper
    {
        #region Constructors

        public Mupper()
        {
        }

        #endregion

        #region Public Methods

        public void Log(string sourcePath, string targetPath)
        {
            using var image = new Bitmap(sourcePath);
            var pixelPointsByColor = this.GetBytes(image)
                .ToPixelColors()
                .WithIndex()
                .Where(x => !x.Value.IsEdgeColor())
                .MapPointsByColor(image.Width);
            this.WriteToTextFile(pixelPointsByColor, targetPath);
        }

        /// <summary> Random color for every blob. </summary>
        public void Repaint(string sourcePath, string targetPath, bool contiguous)
        {
            using var image = new Bitmap(sourcePath);
            var pixels = this.GetBytes(image).ToPixelColors();
            var flags = new bool[pixels.Length];
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var blobs = new List<(Color Color, List<int> Blob)>();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (flags[i])
                    continue;
                var color = pixels[i];
                if (color.IsEdgeColor())
                    continue;
                var stack = i.IntoStack();
                var blob = new List<int>();
                while (stack.TryPop(out var index))
                {
                    if (flags[index])
                        continue;
                    blob.Add(index);
                    flags[index] = true;
                    var (x, y) = index.ToPoint(imageWidth);
                    var left = index - 1;
                    var right = index + 1;
                    var up = index - imageWidth;
                    var down = index + imageWidth;
                    if ((x > 0) && !flags[left] && (pixels[left] == color))
                        stack.Push(left);
                    if ((x < imageWidth - 1) && !flags[right] && (pixels[right] == color))
                        stack.Push(right);
                    if ((y > 0) && !flags[up] && (pixels[up] == color))
                        stack.Push(up);
                    if ((y < imageHeight - 1) && !flags[down] && (pixels[down] == color))
                        stack.Push(down);
                }
                blobs.Add((color, blob));
            }

            var recoloredPixels = new Color[pixels.Length];
            if (contiguous)
                blobs
                    .Each(x => Generate.MupColor()
                        .Into(newColor => x.Blob
                            .Each(index => recoloredPixels[index] = newColor)));
            else
                blobs
                    .GroupBy(x => x.Color)
                    .Each(group => Generate.MupColor()
                        .Into(newColor => group
                            .Each(x => x.Blob
                                .Each(index => recoloredPixels[index] = newColor))));

            var newData = this.GetBytes(recoloredPixels);
            using var image2 = this.BuildImage(newData, image.Width, image.Height);
            image2.Save(targetPath, ImageFormat.Png);
        }

        /// <summary> Create border around contiguous blobs </summary>
        public void Border(string sourcePath, string targetPath, int borderArgb)
        {
            var borderColor = Color.FromArgb(borderArgb);
            using var image = new Bitmap(sourcePath);
            var pixels = this.GetBytes(image).ToPixelColors();
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var recoloredPixels = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (color.IsEdgeColor())
                {
                    recoloredPixels[i] = color;
                    continue;
                }
                var (x, y) = i.ToPoint(imageWidth);
                var left = i - 1;
                var right = i + 1;
                var up = i - imageWidth;
                var down = i + imageWidth;
                if ((x > 0) && (pixels[left] != color))
                {
                    recoloredPixels[i] = borderColor;
                    continue;
                }
                if ((x < imageWidth - 1) && (pixels[right] != color))
                {
                    recoloredPixels[i] = borderColor;
                    continue;
                }
                if ((y > 0) && (pixels[up] != color))
                {
                    recoloredPixels[i] = borderColor;
                    continue;
                }
                if ((y < imageHeight - 1) && (pixels[down] != color))
                {
                    recoloredPixels[i] = borderColor;
                    continue;
                }
                recoloredPixels[i] = color;
            }

            var newData = this.GetBytes(recoloredPixels);
            using var image2 = this.BuildImage(newData, image.Width, image.Height);
            image2.Save(targetPath, ImageFormat.Png);
        }

        /// <summary> Keep a certain amount of contiguous blobs </summary>
        public async Task<Bitmap> ExtractAsync(byte[] imageData) =>
            await Task.Run(() => Extract(imageData));

        /// <summary> Keep a certain amount of contiguous blobs </summary>
        public Bitmap Extract(byte[] imageData)
        {
            using var stream = new MemoryStream(imageData);
            using var image = new Bitmap(stream);
            var pixels = this.GetBytes(image).ToPixelColors();
            var nonEdgeColorPixels = pixels.WithIndex().Where(x => !x.Value.IsEdgeColor());
            var pixelPointsByColor = nonEdgeColorPixels.MapPointsByColor(image.Width);
            var distinctColors = nonEdgeColorPixels
                .Select(x => x.Value)
                .Distinct()
                .ToArray();

            var tiers = Generate.Range(0, 8)
                .Select(x => (int) Math.Pow(3, x))
                .Reverse()
                .ToArray();
            var addedTiers = Generate.Range(1, 9)
                .Select(x => tiers.Take(x).Sum())
                .ToArray();

            // TODO: option to use predetermined tier set
            // var shuffledColorSet = distinctColors.Shuffled()
            //     .Take(tiers[0] + tiers[1])
            //     .ToHashSet();

            var color1 = Color.FromArgb(111, 111, 111);
            var color2 = Color.FromArgb(122, 122, 122);
            var color3 = Color.FromArgb(133, 133, 133);
            var color4 = Color.FromArgb(144, 144, 144);
            var color5 = Color.FromArgb(155, 155, 155);
            var color6 = Color.FromArgb(166, 166, 166);
            var color7 = Color.FromArgb(177, 177, 177);
            var color8 = Color.FromArgb(188, 188, 188);
            var shuffledColors = distinctColors.Shuffled()
                .WithIndex()
                .ToDictionary(pair => pair.Value, pair => pair.Key switch
                {
                    var index when index < addedTiers[0] => color1,
                    var index when index >= addedTiers[0] && index < addedTiers[1] => color2,
                    var index when index >= addedTiers[1] && index < addedTiers[2] => color3,
                    var index when index >= addedTiers[2] && index < addedTiers[3] => color4,
                    var index when index >= addedTiers[3] && index < addedTiers[4] => color5,
                    var index when index >= addedTiers[4] && index < addedTiers[5] => color6,
                    var index when index >= addedTiers[5] && index < addedTiers[6] => color7,
                    var index when index >= addedTiers[6] => color8,
                    _ => Color.Black
                });

            var recoloredPixels = pixels
                .Select(x => x switch
                {
                    var color when color.IsEdgeColor() => Color.White,
                    var color when true => shuffledColors[color]
                });

            var newData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newData, image.Width, image.Height); ;
            // using var recoloredImage = this.BuildImage(newData, image.Width, image.Height);
            // recoloredImage.Save(targetPath, ImageFormat.Png);

            // // to get byte[] usable in wpf it needs to be encoded for PNG
            // using var stream = new MemoryStream();
            // recoloredImage.Save(stream, ImageFormat.Png);
            // return stream.ToArray();
        }

        #endregion

        #region Helper Methods

        protected void WriteToTextFile(Dictionary<Color, Point[]> pixelPointsByColor, string filePath)
        {
            var outFile = new FileInfo(filePath);
            using var outFileWriteStream = outFile.CreateText();
            pixelPointsByColor
                .OrderBy(x => x.Value.Length)
                .Each(x => outFileWriteStream.WriteLine($"Color: {x.Key.R},{x.Key.G},{x.Key.B}, Count: {x.Value.Length}, Points: {x.Value.Select(x => x.ToString()).Join(",")}"));
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

        protected byte[] GetBytes(IEnumerable<Color> colors) =>
            colors.SelectMany(BitmapExtensions.ToBytes).ToArray();

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

        protected void Dump(string dump)
        {
            var outFile = new FileInfo(@"D:\Documents\Code\repos\mup\Mup\dump");
            using var outFileWriteStream = outFile.CreateText();
            outFileWriteStream.WriteLine(dump);
            outFileWriteStream.Flush();
        }

        #endregion
    }
}