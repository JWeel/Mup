using Mup.Extensions;
using Mup.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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

        public async Task<ImageInfo> InfoAsync(byte[] imageData) =>
            await Task.Run(() => Info(imageData));

        public ImageInfo Info(byte[] imageData)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var nonEdgeColorSet = pixels
                .Where(color => !color.IsEdgeColor())
                .ToHashSet();
            var sizeByColor = pixels
                .GroupBy(x => x)
                .ToDictionary(group => group.Key, group => group.Count());
            return new ImageInfo(pixels, nonEdgeColorSet, sizeByColor, imageWidth, imageHeight);
        }

        public async Task LogAsync(byte[] imageData, string logPath) =>
            await Task.Run(() => Log(imageData, logPath));

        public void Log(byte[] imageData, string logPath)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var pixelPointsByColor = pixels
                .WithIndex()
                .Where(x => !x.Value.IsEdgeColor())
                .MapPointsByColor(imageWidth);
            this.WriteToTextFile(pixelPointsByColor, logPath);
        }

        /// <summary> Random color for every blob. </summary>
        public async Task<Bitmap> RepaintAsync(byte[] imageData, bool contiguous) =>
            await Task.Run(() => Repaint(imageData, contiguous));

        /// <summary> Random color for every blob. </summary>
        public Bitmap Repaint(byte[] imageData, bool contiguous)
        {
            var colorSet = new HashSet<Color>();
            Color UniqueMupColor()
            {
                Color color;
                do color = Generate.MupColor();
                while (!colorSet.Add(color));
                return color;
            }

            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var blobs = this.FindNonEdgeBlobs(pixels, imageWidth, imageHeight);
            var recoloredPixels = new Color[pixels.Length];
            if (contiguous)
                blobs
                    .Each(x => UniqueMupColor()
                        .Into(newColor => x.Blob
                            .Each(index => recoloredPixels[index] = newColor)));
            else
                blobs
                    .GroupBy(x => x.Color)
                    .Each(group => UniqueMupColor()
                        .Into(newColor => group
                            .Each(x => x.Blob
                                .Each(index => recoloredPixels[index] = newColor))));
            var newData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newData, imageWidth, imageHeight);
        }

        /// <summary> Create border around contiguous blobs </summary>
        public async Task<Bitmap> BorderAsync(byte[] imageData, int borderArgb) =>
            await Task.Run(() => Border(imageData, borderArgb));

        /// <summary> Create border around contiguous blobs </summary>
        public Bitmap Border(byte[] imageData, int borderArgb)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var borderColor = Color.FromArgb(borderArgb);
            var recoloredPixels = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (color.IsEdgeColor())
                {
                    recoloredPixels[i] = color;
                    continue;
                }

                var opacity = 0.3f;
                var overlaidR = Math.Ceiling(borderColor.R * opacity) + Math.Ceiling((1 - opacity) * color.R);
                var overlaidG = Math.Ceiling(borderColor.G * opacity) + Math.Ceiling((1 - opacity) * color.G);
                var overlaidB = Math.Ceiling(borderColor.B * opacity) + Math.Ceiling((1 - opacity) * color.B);
                var overlaidColor = Color.FromArgb((int) overlaidR, (int) overlaidG, (int) overlaidB);

                var (x, y) = i.ToPoint(imageWidth);
                var left = i - 1;
                var right = i + 1;
                var up = i - imageWidth;
                var down = i + imageWidth;
                if ((x > 0) && (pixels[left] != color))
                {
                    recoloredPixels[i] = overlaidColor;
                    continue;
                }
                if ((x < imageWidth - 1) && (pixels[right] != color))
                {
                    recoloredPixels[i] = overlaidColor;
                    continue;
                }
                if ((y > 0) && (pixels[up] != color))
                {
                    recoloredPixels[i] = overlaidColor;
                    continue;
                }
                if ((y < imageHeight - 1) && (pixels[down] != color))
                {
                    recoloredPixels[i] = overlaidColor;
                    continue;
                }
                recoloredPixels[i] = color;
            }

            var newData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newData, imageWidth, imageHeight);
        }

        /// <summary> Keep a certain amount of contiguous blobs </summary>
        public async Task<Bitmap> ExtractAsync(byte[] imageData) =>
            await Task.Run(() => Extract(imageData));

        /// <summary> Keep a certain amount of contiguous blobs </summary>
        public Bitmap Extract(byte[] imageData)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var distinctColors = pixels
                .Where(x => !x.IsEdgeColor())
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
                    var index when (index >= addedTiers[6]) => color8,
                    var index when (index >= addedTiers[5]) => color7,
                    var index when (index >= addedTiers[4]) => color6,
                    var index when (index >= addedTiers[3]) => color5,
                    var index when (index >= addedTiers[2]) => color4,
                    var index when (index >= addedTiers[1]) => color3,
                    var index when (index >= addedTiers[0]) => color2,
                    _ => color1,
                });

            var recoloredPixels = pixels
                .Select(x => x switch
                {
                    var color when color.IsEdgeColor() => Color.White,
                    var color when true => shuffledColors[color]
                });
            var newData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newData, imageWidth, imageHeight);
        }

        /// <summary> Combine small blobs to create large blobs. </summary>
        public async Task<Bitmap> MergeAsync(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize, int isleBlobSize) =>
            await Task.Run(() => Merge(imageData, contiguous, minBlobSize, maxBlobSize, isleBlobSize));

        /// <summary> Combine small blobs to create large blobs. </summary>
        public Bitmap Merge(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize, int isleBlobSize)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);

            if (contiguous)
            {
                // not supported yet
                var newImageData = this.GetBytes(pixels);
                return this.BuildImage(newImageData, imageWidth, imageHeight);
            }
            else
            {
                // must be discontiguous or dictionary will have duplicate keys
                var blobs = this.FindNonEdgeBlobs(pixels, imageWidth, imageHeight)
                    .GroupBy(x => x.Color)
                    .ToDictionary(group => group.Key, group => group.SelectMany(x => x.Blob).ToArray());
                var neighborsByColor = this.DefineNeighborsByColor(pixels, imageWidth, imageHeight);
                var colorsUsed = new HashSet<Color>();
                var mappedColors = new Dictionary<Color, Color>();

                var colonyColors = new HashSet<Color>();
                if (isleBlobSize != minBlobSize)
                {
                    var sizeByColor = pixels
                        .GroupBy(x => x)
                        .ToDictionary(group => group.Key, group => group.Count());
                    colonyColors = neighborsByColor
                        .Where(x => !x.Key.IsEdgeColor())
                        .Where(x => (sizeByColor[x.Key] < isleBlobSize))
                        .Where(x => x.Value.All(BitmapExtensions.IsEdgeColor))
                        .Select(x => x.Key)
                        .ToHashSet();
                }

                blobs
                    .Where(x => (x.Value.Length < (colonyColors.Contains(x.Key) ? isleBlobSize : minBlobSize)))
                    .Each(x =>
                    {
                        var neighborBlobs = neighborsByColor[x.Key]
                            .Where(x => !x.IsEdgeColor())
                            .ToDictionary(x => x, x => blobs[x]);
                        var onlyBigNeighbors = neighborBlobs.All(x => (x.Value.Length >= maxBlobSize));
                        var smallestNeighborOrDefault = neighborBlobs
                            .Where(n => !colorsUsed.Contains(n.Key))
                            .Where(n => ((n.Value.Length < maxBlobSize) || onlyBigNeighbors))
                            .OrderBy(n => n.Value.Length)
                            .Select(GenericExtensions.ToNullable)
                            .FirstOrDefault();
                        if (!smallestNeighborOrDefault.HasValue)
                            return;
                        var smallestNeighbor = smallestNeighborOrDefault.Value;
                        // whoever is smaller wins
                        if (smallestNeighbor.Value.Length < x.Value.Length)
                            mappedColors[x.Key] = smallestNeighbor.Key;
                        else
                            mappedColors[smallestNeighbor.Key] = x.Key;
                        colorsUsed.Add(smallestNeighbor.Key);
                        colorsUsed.Add(x.Key);
                    });
                var recoloredPixels = pixels
                    .Select(x => x switch
                    {
                        var color when mappedColors.TryGetValue(color, out var mappedColor) => mappedColor,
                        var color when true => color
                    });
                var newData = this.GetBytes(recoloredPixels);
                return this.BuildImage(newData, imageWidth, imageHeight);
            }
        }

        /// <summary> Split blobs into smaller blobs. </summary>
        public async Task<Bitmap> SeparateAsync(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize) =>
            await Task.Run(() => Separate(imageData, contiguous, minBlobSize, maxBlobSize));

        /// <summary> Split blobs into smaller blobs. </summary>
        public Bitmap Separate(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var colorSet = pixels.Distinct().ToHashSet();
            Color UniqueMupColor()
            {
                Color color;
                do color = Generate.MupColor();
                while (!colorSet.Add(color));
                return color;
            }
            var recoloredPixels = pixels.ToArray();
            var blobs = this.FindNonEdgeBlobs(pixels, imageWidth, imageHeight);
            if (!contiguous)
                blobs = blobs
                    .GroupBy(x => x.Color)
                    .Select(group => (Color: group.Key, Blob: group.SelectMany(x => x.Blob).ToArray()))
                    .ToArray();
            blobs
                .Where(x => (x.Blob.Length > maxBlobSize))
                .Each(tuple =>
                {
                    var blobIndices = tuple.Blob.Shuffled().ToList();
                    var pixelsRecolored = 0;
                    while (blobIndices.Any())
                    {
                        var nextColor = UniqueMupColor();
                        var buffer = blobIndices.First().IntoList();
                        while (buffer.TryPopRandom(out var index))
                        {
                            if (!blobIndices.Contains(index))
                                continue;
                            blobIndices.Remove(index);
                            recoloredPixels[index] = nextColor;
                            if (++pixelsRecolored >= minBlobSize)
                            {
                                pixelsRecolored = 0;
                                nextColor = UniqueMupColor();
                                break;
                            }
                            var (x, y) = index.ToPoint(imageWidth);
                            if (x > 0) buffer.Add(index - 1);
                            if (x < imageWidth - 1) buffer.Add(index + 1);
                            if (y > 0) buffer.Add(index - imageWidth);
                            if (y < imageHeight - 1) buffer.Add(index + imageWidth);
                        }
                    }
                });

            var newImageData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        /// <summary> Join blobs separated by edges with their nearest neighbor across the edge. </summary>
        public async Task<Bitmap> ColonyAsync(byte[] imageData, int maxBlobSize, int isleBlobSize) =>
            await Task.Run(() => Colony(imageData, maxBlobSize, isleBlobSize));

        /// <summary> Join blobs separated by edges with their nearest neighbor across the edge. </summary>
        public Bitmap Colony(byte[] imageData, int maxBlobSize, int isleBlobSize)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var sizeByColor = pixels
                .GroupBy(x => x)
                .ToDictionary(group => group.Key, group => group.Count());
            var neighborsByColor = this.DefineNeighborsByColor(pixels, imageWidth, imageHeight);
            var colonyColors = neighborsByColor
                .Where(x => !x.Key.IsEdgeColor())
                .Where(x => (sizeByColor[x.Key] < isleBlobSize))
                .Where(x => x.Value.All(BitmapExtensions.IsEdgeColor))
                .Select(x => x.Key)
                .ToHashSet();

            var mainlandColorByColonyColor = new Dictionary<Color, Color>();
            var recoloredPixels = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (!colonyColors.Contains(color))
                {
                    recoloredPixels[i] = color;
                    continue;
                }
                if (mainlandColorByColonyColor.TryGetValue(color, out var mainlandColor))
                {
                    recoloredPixels[i] = mainlandColor;
                    continue;
                }

                var handledIndices = new HashSet<int>();
                var queue = i.IntoQueue();
                while (queue.TryDequeue(out var index))
                {
                    if (!handledIndices.Add(index))
                        continue;
                    var otherColor = pixels[index];
                    if (!otherColor.IsEdgeColor() && (color != otherColor) && !colonyColors.Contains(otherColor))
                    {
                        mainlandColorByColonyColor[color] = otherColor;
                        recoloredPixels[i] = otherColor;
                        break;
                    }

                    var (x, y) = index.ToPoint(imageWidth);
                    if (x > 0) queue.Enqueue(index - 1);
                    if (x < imageWidth - 1) queue.Enqueue(index + 1);
                    if (y > 0) queue.Enqueue(index - imageWidth);
                    if (y < imageHeight - 1) queue.Enqueue(index + imageWidth);
                }
            }

            var newImageData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        /// <summary> Identify blobs of incorrect size. </summary>
        public async Task<Bitmap> CheckAsync(byte[] imageData, int minBlobSize, int maxBlobSize, int isleBlobSize) =>
            await Task.Run(() => Check(imageData, minBlobSize, maxBlobSize, isleBlobSize));

        /// <summary> Identify blobs of incorrect size. </summary>
        public Bitmap Check(byte[] imageData, int minBlobSize, int maxBlobSize, int isleBlobSize)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var sizeByColor = pixels
                .GroupBy(x => x)
                .ToDictionary(group => group.Key, group => group.Count());
            var neighborsByColor = this.DefineNeighborsByColor(pixels, imageWidth, imageHeight);
            var colonyColors = neighborsByColor
                .Where(x => !x.Key.IsEdgeColor())
                .Where(x => x.Value.All(BitmapExtensions.IsEdgeColor))
                .Select(x => x.Key)
                .ToHashSet();
            var recoloredPixels = pixels
                .Select(x => x switch
                {
                    var color when color.IsEdgeColor() => color,
                    var color when sizeByColor[color] < (colonyColors.Contains(color) ? isleBlobSize : minBlobSize) => Color.Yellow,
                    var color when sizeByColor[color] > maxBlobSize => Color.Red,
                    var color when true => color
                });
            var newImageData = this.GetBytes(recoloredPixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        /// <summary> Color blobs depending on whether or not they touch an edge. </summary>
        public async Task<Bitmap> EdgeAsync(byte[] imageData, bool contiguous) =>
            await Task.Run(() => Edge(imageData, contiguous));

        /// <summary> Color blobs depending on whether or not they touch an edge. </summary>
        public Bitmap Edge(byte[] imageData, bool contiguous)
        {
            if (contiguous)
            {
                // not supported yet
                var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
                var newImageData = this.GetBytes(pixels);
                return this.BuildImage(newImageData, imageWidth, imageHeight);
            }
            else
            {
                var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
                var neighborsByColor = this.DefineNeighborsByColor(pixels, imageWidth, imageHeight);
                var recoloredPixels = pixels
                    .Select(x => x switch
                    {
                        var color when color.IsEdgeColor() => color,
                        var color when neighborsByColor[color].Any(x => x.IsEdgeColor()) => Color.Yellow,
                        _ => Color.Green
                    });
                var newImageData = this.GetBytes(recoloredPixels);
                return this.BuildImage(newImageData, imageWidth, imageHeight);
            }
        }

        /// <summary> Count overlapping blobs in different images. </summary>
        public async Task<Bitmap> CompareAsync(byte[] imageData1, byte[] imageData2) =>
            await Task.Run(() => Compare(imageData1, imageData2));

        /// <summary> Count overlapping blobs in different images. </summary>
        public Bitmap Compare(byte[] imageData1, byte[] imageData2)
        {
            // not supported yet
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData1);
            var newImageData = this.GetBytes(pixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        #endregion

        #region Helper Methods

        protected void WriteToTextFile(Dictionary<Color, Point[]> pixelPointsByColor, string filePath)
        {
            var outFile = new FileInfo(filePath);
            using var outFileWriteStream = outFile.CreateText();
            pixelPointsByColor
                .OrderBy(x => x.Value.Length)
                .Each(x => outFileWriteStream.WriteLine($"Color: {x.Key.R},{x.Key.G},{x.Key.B}, Count: {x.Value.Length}, Points: {x.Value.Select(x => $"({x.X},{x.Y})").Join(", ")}"));
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

        protected (Color[] Pixels, int ImageWidth, int ImageHeight) ReadImageData(byte[] imageData)
        {
            using var stream = new MemoryStream(imageData);
            using var image = new Bitmap(stream);
            var pixels = this.GetBytes(image).ToPixelColors();
            return (pixels, image.Width, image.Height);
        }

        protected (Color Color, int[] Blob)[] FindNonEdgeBlobs(Color[] pixels, int imageWidth, int imageHeight)
        {
            var flags = new bool[pixels.Length];
            var blobs = new List<(Color Color, int[] Blob)>();
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
                blobs.Add((color, blob.ToArray()));
            }
            return blobs.ToArray();
        }

        // for discontiguous only
        protected IDictionary<Color, Color[]> DefineNeighborsByColor(Color[] pixels, int imageWidth, int imageHeight)
        {
            var neighborsByColor = new Dictionary<Color, HashSet<Color>>();
            void CheckNeighbor(bool condition, int neighborIndex, Color color)
            {
                if (!condition) return;
                var neighbor = pixels[neighborIndex];
                if (neighbor == color) return;
                neighborsByColor.AddOrInit(color, neighbor);
            }
            for (int index = 0; index < pixels.Length; index++)
            {
                var color = pixels[index];
                var (x, y) = index.ToPoint(imageWidth);
                CheckNeighbor((x > 0), index - 1, color);
                CheckNeighbor((x < imageWidth - 1), index + 1, color);
                CheckNeighbor((y > 0), index - imageWidth, color);
                CheckNeighbor((y < imageHeight - 1), index + imageWidth, color);
            }
            return neighborsByColor.ToDictionary(x => x.Key, x => x.Value.ToArray());
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