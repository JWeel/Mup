using System.Runtime.Serialization;
using Mup.Extensions;
using Mup.External;
using Mup.Helpers;
using Mup.Models;
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
        #region Constants

        private const int IGNORED_CLUSTER_IDENTIFIER = -1;

        #endregion

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
            var newData = this.GetBytesFromColors(recoloredPixels);
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

            var newData = this.GetBytesFromColors(recoloredPixels);
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
                .ToDictionary(pair => pair.Value, pair => pair.Index switch
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
                    var color when color.IsEdgeColor() => color,
                    var color when true => shuffledColors[color]
                });
            var newData = this.GetBytesFromColors(recoloredPixels);
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
                // not supported yet
                return this.BuildImage(pixels, imageWidth, imageHeight);
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
                var newData = this.GetBytesFromColors(recoloredPixels);
                return this.BuildImage(newData, imageWidth, imageHeight);
            }
        }

        /// <summary> Separate large blobs into smaller blobs. </summary>
        public async Task<Bitmap> SplitAsync(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize) =>
            await Task.Run(() => Split(imageData, contiguous, minBlobSize, maxBlobSize));

        /// <summary> Separate large blobs into smaller blobs. </summary>
        public Bitmap Split(byte[] imageData, bool contiguous, int minBlobSize, int maxBlobSize)
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

            var newImageData = this.GetBytesFromColors(recoloredPixels);
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

            var newImageData = this.GetBytesFromColors(recoloredPixels);
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
                    _ => x
                });
            var newImageData = this.GetBytesFromColors(recoloredPixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        /// <summary> Color blobs depending on whether or not they touch an edge. </summary>
        public async Task<Bitmap> EdgeAsync(byte[] imageData, bool contiguous) =>
            await Task.Run(() => Edge(imageData, contiguous));

        /// <summary> Color blobs depending on whether or not they touch an edge. </summary>
        public Bitmap Edge(byte[] imageData, bool contiguous)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);

            if (contiguous)
                // not supported yet
                return this.BuildImage(pixels, imageWidth, imageHeight);
            else
            {
                var neighborsByColor = this.DefineNeighborsByColor(pixels, imageWidth, imageHeight);
                var recoloredPixels = pixels
                    .Select(x => x switch
                    {
                        var color when color.IsEdgeColor() => color,
                        var color when neighborsByColor[color].Any(x => x.IsEdgeColor()) => Color.Yellow,
                        _ => Color.Green
                    });
                var newImageData = this.GetBytesFromColors(recoloredPixels);
                return this.BuildImage(newImageData, imageWidth, imageHeight);
            }
        }

        /// <summary> Identify bounds restricting clustering of cells. </summary>
        public async Task<Bitmap> BindAsync(byte[] primaryImageData, byte[] backingImageData, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => Bind(primaryImageData, backingImageData, ignoredArgbSet));

        /// <summary> Identify bounds restricting clustering of cells. </summary>
        public Bitmap Bind(byte[] primaryImageData, byte[] backingImageData, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(backingImageData);
            var (backingPixels, _, _) = this.ReadImageData(primaryImageData);
            return this.BuildImage(primaryPixels, imageWidth, imageHeight);
        }

        /// <summary> Highlight random cells. </summary>
        public async Task<Bitmap> PopAsync(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int popArgb, int rootArgb, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => this.Pop(primaryImageData, backingImageData, bindingImageData, popArgb, rootArgb, ignoredArgbSet));

        /// <summary> Highlight random cells. </summary>
        public Bitmap Pop(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int popArgb, int rootArgb, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(primaryImageData);
            var popColor = Color.FromArgb(popArgb);
            var rootColor = Color.FromArgb(rootArgb);

            Color[] recoloredPixels;
            if (backingImageData.IsNullOrEmpty() || bindingImageData.IsNullOrEmpty())
            {
                var poppedColor = primaryPixels
                    .Where(x => !x.ToArgb().In(ignoredArgbSet))
                    .Distinct()
                    .ToList()
                    .PopRandom();
                recoloredPixels = primaryPixels.Select(x => x switch
                {
                    var color when (color == poppedColor) => popColor,
                    var color when (color == popColor) => rootColor,
                    var color when true => color
                }).ToArray();
            }
            else
            {
                // doesnt need backing pixels per se,
                // could use pixels instead of backingpixels if no backingpixels

                recoloredPixels = primaryPixels.ToArray();

                // TODO when bind and pop, additional pop should not add to existing but should replace
                // but that means we need to find what the original color was on the primarypixels
                // which we dont have a direct way to
                primaryPixels
                    .WithIndex()
                    .Where(x => (x.Value.ToArgb() == popArgb))
                    .Each(x => recoloredPixels[x.Index] = rootColor); // replace with original color

                var (backingPixels, _, _) = this.ReadImageData(backingImageData);
                var (bindingPixels, _, _) = this.ReadImageData(bindingImageData);
                var bindingAllocation = this.GetBindingAllocation(bindingPixels, ignoredArgbSet);
                backingPixels
                    .WithIndex()
                    .GroupBy(x => bindingAllocation[x.Index])
                    .Where(group => group.Key != IGNORED_CLUSTER_IDENTIFIER)
                    .Each(group => group
                        .GroupBy(x => x.Value)
                        .Select(subGroup => subGroup.Select(x => x.Index))
                        .ToList()
                        .PopRandom()
                        .Each(index => recoloredPixels[index] = popColor));
            }
            var newImageData = this.GetBytesFromColors(recoloredPixels);
            var bitmap = this.BuildImage(newImageData, imageWidth, imageHeight);
            return bitmap;
        }

        /// <summary> Group discontiguous blobs into clusters. </summary>
        public async Task<Bitmap> ClusterAsync(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int amountOfClusters, int rootArgb, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => Cluster(primaryImageData, backingImageData, bindingImageData, amountOfClusters, rootArgb, ignoredArgbSet));

        /// <summary> Group discontiguous blobs into clusters. </summary>
        public Bitmap Cluster(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int amountOfClusters, int rootArgb, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(primaryImageData);
            var (backingPixels, _, _) = this.ReadImageData(backingImageData);
            var rootColor = Color.FromArgb(rootArgb);

            Cluster[] clusters;
            if (bindingImageData.IsNullOrEmpty())
            {
                // should ignoredArgbSet be used here?
                var backingCells = this.FindCells(backingPixels, imageWidth, imageHeight);
                var clusterAllocation = new int[backingCells.Length];
                clusters = this.CreateClusters(clusterAllocation, backingCells);
            }
            else
            {
                var (bindingPixels, _, _) = this.ReadImageData(bindingImageData);
                var bindingAllocation = this.GetBindingAllocation(bindingPixels, ignoredArgbSet);
                clusters = backingPixels
                    .WithIndex()
                    .GroupBy(x => bindingAllocation[x.Index])
                    .Where(group => group.Key != IGNORED_CLUSTER_IDENTIFIER)
                    .Select(group => group
                        .GroupBy(x => x.Value)
                        .Select(subGroup => subGroup
                            .Select(x => x.Index.ToPoint(imageWidth).ToVector())
                            .Average()
                            .Into(center => new Cell(subGroup.Key, center)))
                        .ToArray())
                    .SelectMany(cellGroup =>
                    {
                        var cellCenters = cellGroup.Select(x => x.Center).ToArray();
                        var meanPerCluster = this.InitializeClusterMeans(cellCenters, amountOfClusters);
                        var clusterAllocation = this.InitializeClusterAllocation(cellCenters, meanPerCluster, cellCenters.Length / amountOfClusters);
                        return this.CreateClusters(clusterAllocation, cellGroup);
                    })
                    .ToArray();
            }

            var clusterColorMap = clusters
                .SelectMany(cluster => cluster.Cells
                    .Select(cell => (Color: cell.Color, ClusterColor: cluster.Color)))
                .ToDictionary(x => x.Color, x => x.ClusterColor);
            var recoloredPixels = backingPixels
                .Select(x => x switch
                {
                    var color when color.ToArgb().In(ignoredArgbSet) => color,
                    var color when clusterColorMap.TryGetValue(color, out var value) => value,
                    _ => rootColor
                })
                .ToArray();
            return this.BuildImage(recoloredPixels, imageWidth, imageHeight);
        }

        /// <summary> Reallocate clustered blobs to create better fits. </summary>
        public async Task<Bitmap> RefineAsync(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int amountOfClusters, int maxIterations, int rootArgb, int nodeArgb, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => Refine(primaryImageData, backingImageData, bindingImageData, amountOfClusters, maxIterations, rootArgb, nodeArgb, ignoredArgbSet));

        /// <summary> Reallocate clustered blobs to create better fits. </summary>
        public Bitmap Refine(byte[] primaryImageData, byte[] backingImageData, byte[] bindingImageData, int amountOfClusters, int maxIterations, int rootArgb, int nodeArgb, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(primaryImageData);
            var (backingPixels, _, _) = this.ReadImageData(backingImageData);
            var (bindingPixels, _, _) = this.ReadImageData(bindingImageData);
            var rootColor = Color.FromArgb(rootArgb);
            var nodeColor = Color.FromArgb(nodeArgb);

            var bindingAllocation = this.GetBindingAllocation(bindingPixels, ignoredArgbSet);
            var cellsInClustersInBindings = backingPixels
                .WithIndex()
                .GroupBy(x => bindingAllocation[x.Index])
                .Where(bindGroup => bindGroup.Key != IGNORED_CLUSTER_IDENTIFIER)
                .Select(bindGroup => bindGroup
                    .Select(x => (x.Index, BackingColor: backingPixels[x.Index], CurrentColor: primaryPixels[x.Index]))
                    .GroupBy(x => x.CurrentColor)
                    .Where(primaryGroup => !primaryGroup.Key.ToArgb().In(ignoredArgbSet))
                    .Select(primaryGroup => primaryGroup
                        .GroupBy(x => x.BackingColor)
                        .Where(backingGroup => !backingGroup.Key.ToArgb().In(ignoredArgbSet))
                        .Select(backingGroup => backingGroup
                            .Select(x => x.Index.ToPoint(imageWidth).ToVector())
                            .Average()
                            .Into(center => new Cell(backingGroup.Key, center)))
                        .ToArray()
                        .Into(x => new Cluster(primaryGroup.Key, x)))
                    .ToArray())
                .ToArray();

            var clusteredClusters = cellsInClustersInBindings
                .Select(bindGroup =>
                {
                    var cellList = bindGroup
                        .SelectMany(cluster => cluster.Cells)
                        .ToList();

                    var clusterAllocation = bindGroup
                        .WithIndex()
                        .SelectMany(x => x.Value.Cells.Select(c => (ClusterIndex: x.Index, CellIndex: cellList.IndexOf(c))))
                        .OrderBy(x => x.CellIndex) // why order?
                        .Select(x => x.ClusterIndex)
                        .ToArray();

                    var centers = cellList
                        .Select(x => x.Center)
                        .ToArray();

                    var meanPerCluster = new Vector[amountOfClusters];
                    return this.RefineClustering(centers, amountOfClusters, maxIterations, meanPerCluster, clusterAllocation)
                        .WithIndex()
                        .GroupBy(kvp => kvp.Value)
                        .Select(group => group.Select(kvp => cellList[kvp.Index]).ToList())
                        .ToArray()
                        .WithIndex()
                        .Select(x => new Cluster(bindGroup[x.Index].Color, x.Value.ToArray()))
                        .ToArray();
                })
                .ToArray();

            var parentColorMap = clusteredClusters
                .SelectMany(group => group
                    .SelectMany(cluster => cluster.Cells
                        .Select(cell => (Color: cell.Color, ParentColor: cluster.Color))))
                .ToDictionary(x => x.Color, x => x.ParentColor);
            var recoloredPixels = backingPixels
                .Select(x => x switch
                {
                    var color when color.ToArgb().In(ignoredArgbSet) => color,
                    var color when parentColorMap.TryGetValue(color, out var value) => value,
                    _ => rootColor
                })
                .ToArray();
            return this.BuildImage(recoloredPixels, imageWidth, imageHeight);
        }

        /// <summary> Divide all discontiguous blobs into sets of parents with three unique children. </summary>
        public async Task<Bitmap> AllocateAsync(byte[] imageData, int rootArgb, int amountOfClusters, int maxIterations) =>
            await Task.Run(() => Allocate(imageData, rootArgb, amountOfClusters, maxIterations));

        /// <summary> Divide all discontiguous blobs into sets of parents with three unique children. </summary>
        public Bitmap Allocate(byte[] imageData, int rootArgb, int amountOfClusters, int maxIterations)
        {
            var (pixels, imageWidth, imageHeight) = this.ReadImageData(imageData);
            var distinctColors = pixels
                .Where(x => !x.IsEdgeColor())
                .Distinct()
                .ToArray();
            if ((distinctColors.Length - 1) % amountOfClusters != 0)
                // not supported yet
                return this.BuildImage(this.GetBytesFromColors(pixels), imageWidth, imageHeight);

            var rootColor = Color.FromArgb(rootArgb);
            var cells = this.FindCells(pixels, imageWidth, imageHeight);
            var cellList = cells.ToList();
            var parentColorMap = new Dictionary<Color, Color>();
            var stack = (Bucket: (IList<Cell>) cellList, ParentColor: rootColor).IntoStack();
            while (stack.TryPop(out var tuple))
            {
                var (bucket, parentColor) = tuple;
                if (!bucket.TryPopRandom(out var child))
                    continue;
                var childColor = child.Color;
                parentColorMap[childColor] = parentColor;
                if (!bucket.Any())
                    continue;
                this.Cluster(bucket, amountOfClusters, maxIterations)
                    .Select(cluster => (cluster, childColor))
                    .Each(stack.Push);
            }
            var recoloredPixels = pixels.Select(x => x switch
            {
                var color when color.IsEdgeColor() => color,
                var color when true => parentColorMap[color]
            });
            var newImageData = this.GetBytesFromColors(recoloredPixels);
            return this.BuildImage(newImageData, imageWidth, imageHeight);
        }

        /// <summary> Count overlapping blobs in different images. </summary>
        public async Task<Bitmap> CompareAsync(byte[] primaryImageData, byte[] backingImageData, int minSize, int maxSize, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => Compare(primaryImageData, backingImageData, minSize, maxSize, ignoredArgbSet));

        /// <summary> Count overlapping blobs in different images. </summary>
        public Bitmap Compare(byte[] primaryImageData, byte[] backingImageData, int minSize, int maxSize, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(primaryImageData);
            var (backingPixels, _, _) = this.ReadImageData(backingImageData);

            var amountOfBackingColorsByPrimaryColor = backingPixels
                .WithIndex()
                .Where(x => !x.Value.ToArgb().In(ignoredArgbSet))
                .GroupBy(x => primaryPixels[x.Index])
                .Select(group => group
                    .Select(x => x.Value)
                    .Distinct()
                    .Count()
                    .Into(x => (Color: group.Key, Count: x)))
                .ToDictionary(x => x.Color, x => x.Count);

            var recoloredPixels = primaryPixels
                .Select(x => x switch
                {
                    var color when color.ToArgb().In(ignoredArgbSet) => color,
                    var color when true => amountOfBackingColorsByPrimaryColor[color] switch
                    {
                        var count when (count < minSize) => Color.Yellow,
                        var count when (count > maxSize) => Color.Red,
                        _ => Color.Green
                    }
                })
                .ToArray();
            return this.BuildImage(recoloredPixels, imageWidth, imageHeight);
        }

        /// <summary> Highlights a backing cell with a specified color within a contiguous blob while changing the color of all other backing cells in the blob to the specified value. </summary>
        public async Task<Bitmap> CrownAsync(byte[] primaryImageData, byte[] backingImageData, Point point, int popArgb, ISet<int> ignoredArgbSet) =>
            await Task.Run(() => Crown(primaryImageData, backingImageData, point, popArgb, ignoredArgbSet));

        /// <summary> Highlights a backing cell with a specified color within a contiguous blob while changing the color of all other backing cells in the blob to the specified value. </summary>
        public Bitmap Crown(byte[] primaryImageData, byte[] backingImageData, Point point, int popArgb, ISet<int> ignoredArgbSet)
        {
            var (primaryPixels, imageWidth, imageHeight) = this.ReadImageData(primaryImageData);
            var (backingPixels, _, _) = this.ReadImageData(backingImageData);
            var popColor = Color.FromArgb(popArgb);

            var selectedIndex = point.ToIndex(imageWidth);
            var selectedPrimaryColor = primaryPixels[selectedIndex];
            var selectedBackingColor = backingPixels[selectedIndex];

            var recoloredPixels = primaryPixels.ToArray();
            backingPixels
                .WithIndex()
                .Where(x => (primaryPixels[x.Index] == selectedPrimaryColor))
                .Each(x => recoloredPixels[x.Index] = (x.Value == selectedBackingColor) ? popColor : selectedBackingColor);

            return this.BuildImage(recoloredPixels, imageWidth, imageHeight);
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

        protected byte[] GetBytesFromColors(IEnumerable<Color> colors) =>
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

        // for discontiguous only
        protected Cell[] FindCells(Color[] pixels, int imageWidth, int imageHeight, ISet<int> ignoredArgbSet = default) =>
            this.FindNonEdgeBlobs(pixels, imageWidth, imageHeight)
                .GroupBy(x => x.Color)
                .Where(x => ((ignoredArgbSet == null) || !x.Key.ToArgb().In(ignoredArgbSet)))
                .Select(group => (Color: group.Key, Blobs: group.SelectMany(x => x.Blob).ToArray()))
                .Select(x => x.Blobs
                    .Select(index => index.ToPoint(imageWidth).ToVector())
                    .Average()
                    .Into(center => new Cell(x.Color, center)))
                .ToArray();

        protected IList<Cell>[] Cluster(IList<Cell> source, int amountOfClusters, int maxIterations)
        {
            var data = source
                .Select(x => x.Center)
                .ToArray();
            return this.Cluster(data, amountOfClusters, maxIterations)
                .WithIndex()
                .GroupBy(kvp => kvp.Value)
                .Select(group => group.Select(kvp => source[kvp.Index]).ToList())
                .ToArray();
        }

        /// <summary> Groups vectors into clusters of equal size based on distance to cluster mean. </summary>
        /// <returns> The cluster allocation for each vector. Each value corresponds to a cluster index. </returns>
        protected int[] Cluster(Vector[] data, int amountOfClusters, int maxIterations)
        {
            if (amountOfClusters < 1)
                throw new ArgumentException("The amount of clusters K must be at least 1.");
            if (data.Length % amountOfClusters != 0)
                throw new ArgumentException("Equal-sized clustering requires division of N points by K clusters to be a whole number.");

            var vectorsPerCluster = data.Length / amountOfClusters;
            var meanPerCluster = this.InitializeClusterMeans(data, amountOfClusters);
            var clusterAllocation = this.InitializeClusterAllocation(data, meanPerCluster, vectorsPerCluster);

            if (amountOfClusters == 1)
                return clusterAllocation;
            return this.RefineClustering(data, amountOfClusters, maxIterations, meanPerCluster, clusterAllocation);
        }

        /// <summary> Groups vectors into clusters of equal size based on distance to cluster mean. </summary>
        /// <returns> The cluster allocation for each vector. Each value corresponds to a cluster index. </returns>
        protected int[] RefineClustering(Vector[] data, int amountOfClusters, int maxIterations, Vector[] meanPerCluster, int[] clusterAllocation)
        {
            var iteration = 0;
            while (iteration++ < maxIterations)
            {
                var nextMeanPerCluster = this.CalculateMeanPerCluster(data, clusterAllocation);
                if (nextMeanPerCluster.SequenceEqual(meanPerCluster))
                    break;
                meanPerCluster = nextMeanPerCluster;

                var clusterNodes = data
                    .Select((vector, index) =>
                        new ClusterNode(index, vector, clusterAllocation[index], meanPerCluster
                            .SelectWithIndex(mean => vector.Distance(mean))
                            .ToDictionary(x => x.Index, x => x.Value)))
                    .ToArray();
                var clusterNodeOrderMap = Generate.Range(amountOfClusters)
                    .SelectWithIndex(clusterIndex => new SortedSet<ClusterNode>(clusterNodes, new ClusterNodeComparer(clusterIndex)))
                    .ToDictionary(x => x.Index, x => x.Value);

                var madeASwap = false;
                var swappedNodeSet = new HashSet<ClusterNode>();
                var nodesNotInBestClusterOrderedByGreatestImprovement = clusterNodes
                    .Where(node => !swappedNodeSet.Contains(node))
                    .Select(node => (Node: node, BestDistance: node.ClusterDistanceMap.MinBy(x => x.Value)))
                    .Where(tuple => (tuple.BestDistance.Key != tuple.Node.ClusterIndex))
                    .OrderBy(tuple => tuple.BestDistance.Value - tuple.Node.CurrentDistance);
                foreach (var tuple in nodesNotInBestClusterOrderedByGreatestImprovement)
                {
                    // calculate gain : overall improvement of swap
                    // node gain = bestDistance - distanceToOurCurrentCluster (should never be negative)
                    // other gain = otherDistanceToItsCurrentCluster - distanceToOurCurrentCluster (can be negative)
                    // gain = delta + otherDelta -> if it is > 0 then it is an overall improvement

                    var (node, bestDistance) = tuple;
                    var delta = node.ClusterDistanceMap[node.ClusterIndex] - bestDistance.Value;
                    if (!clusterNodeOrderMap[bestDistance.Key]
                        .Where(other => !swappedNodeSet.Contains(other))
                        .Where(other => (other.ClusterIndex == bestDistance.Key))
                        .Select(other => (Other: other, Delta: (other.CurrentDistance - other.ClusterDistanceMap[node.ClusterIndex])))
                        .TryGetFirst(other => (delta + other.Delta > 0), out var otherTuple))
                        continue;

                    var (other, otherDelta) = otherTuple;
                    swappedNodeSet.Add(node);
                    swappedNodeSet.Add(other);

                    other.ClusterIndex = node.ClusterIndex;
                    clusterAllocation[other.Index] = node.ClusterIndex;

                    node.ClusterIndex = bestDistance.Key;
                    clusterAllocation[node.Index] = bestDistance.Key;
                    madeASwap = true;
                }

                if (!madeASwap)
                    break;
            }
            return clusterAllocation;
        }

        protected int[] GetBindingAllocation(Color[] bindingPixels, ISet<int> ignoredArgbSet)
        {
            var increment = 0;
            var incrementByColor = new Dictionary<Color, int>();
            return bindingPixels
                .Select(x => x.ToArgb().In(ignoredArgbSet)
                    ? IGNORED_CLUSTER_IDENTIFIER
                    : incrementByColor.GetOrSetMissing(x, () => increment++))
                .ToArray();
        }

        protected Vector[] CalculateMeanPerCluster(Vector[] data, int[] clusterAllocation) =>
            data.WithIndex()
                .GroupBy(vector => clusterAllocation[vector.Index])
                .Select(group => group
                    .Select(vector => vector.Value)
                    .Average())
                .ToArray();

        protected Vector[] InitializeClusterMeans(Vector[] data, int amountOfClusters)
        {
            // select k data items as initial means using k-means++ mechanism:
            // pick one data item at random as first mean
            // loop k-1 times (remaining means)
            //	 compute dist^2 from each item to closest mean
            //	 pick a data item w/ large dist^2 as next mean
            // end loop

            var means = new Vector[amountOfClusters];
            var (index, randomVector) = data.RandomWithIndex();
            means[0] = randomVector;
            var usedIndexSet = index.IntoSet();

            var random = new Random();
            // iterate remaining means
            for (int k = 1; k < amountOfClusters; ++k)
            {
                // calculate squared distance to closest mean for each vector
                var distancesSquared = new double[data.Length];
                for (int i = 0; i < data.Length; ++i) // for each data item
                {
                    // if data item i is already a mean, skip the item
                    if (usedIndexSet.Contains(i))
                        continue;

                    // find closest mean, save the associated distance-squared
                    var vector = data[i];
                    distancesSquared[i] = means.Min(mean => vector.Distance(mean)).Pow(2);
                }
                // no idea what happens from this point forward but it's k-means++

                // pick one of the data items, using the squared distances (this is a form of roulette wheel selection)
                var p = random.NextDouble();
                var sumOfSquaredDistances = 0.0;
                for (int i = 0; i < distancesSquared.Length; ++i)
                    sumOfSquaredDistances += distancesSquared[i];

                var cumulativeProbability = 0.0;
                var ii = 0; // points into distancesSquared[]
                var sanity = 0; // sanity count
                var newMean = -1; // index of data item to be a new mean
                while (sanity < data.Length * 2) // 'stochastic acceptance'
                {
                    cumulativeProbability += distancesSquared[ii] / sumOfSquaredDistances;
                    if ((cumulativeProbability >= p) && !usedIndexSet.Contains(ii))
                    {
                        newMean = ii; // the chosen index
                        usedIndexSet.Add(newMean); // don't pick again
                        break;
                    }
                    ++ii; // next candidate
                    if (ii >= distancesSquared.Length)
                        ii = 0; // back to first item
                    ++sanity;
                }
                if (newMean == -1)
                    // throw new InvalidOperationException("Bad k-means++! Bad!");
                    break;

                means[k] = data[newMean];
            }
            return means;
        }

        protected int[] InitializeClusterAllocation(Vector[] data, Vector[] meanPerCluster, int clusterSize)
        {
            // 1. Order points by the distance to their nearest cluster minus distance to the farthest cluster
            // 2. Assign points to their preferred cluster until this cluster is full
            // 3. Resort remaining objects without taking the full cluster into account anymore
            var orderedDistancesToMeanPerVector = data
                .Select(vector => meanPerCluster
                    .Select((mean, index) => (MeanIndex: index, Distance: vector.Distance(mean)))
                    .OrderBy(tuple => tuple.Distance)
                    .ToArray())
                .ToArray();
            var vectorIndexList = orderedDistancesToMeanPerVector
                .Select((tuple, index) => (VectorIndex: index, DistanceByMean: tuple))
                .ToList();
            var clusterAllocation = new int[data.Length];
            var clusterCounts = new int[meanPerCluster.Length];
            while (clusterCounts.Any(count => (count < clusterSize)))
            {
                // ordered by "biggest benefit of best over worst distance"
                var orderedVectorIndices = vectorIndexList
                    .OrderBy(element => element.DistanceByMean.First().Distance - element.DistanceByMean.Last().Distance)
                    .ToArray();
                foreach (var t in orderedVectorIndices)
                {
                    vectorIndexList.Remove(t);
                    var meanIndex = t.DistanceByMean.First(x => (clusterCounts[x.MeanIndex] < clusterSize)).MeanIndex;
                    clusterAllocation[t.VectorIndex] = meanIndex;
                    if (++clusterCounts[meanIndex] < clusterSize)
                        continue;

                    // cluster filled -> resort remaining vectors without that cluster
                    for (int i = 0; i < vectorIndexList.Count; i++)
                    {
                        var current = vectorIndexList[i];
                        var distanceByMean = current.DistanceByMean
                            .Where(d => (d.MeanIndex != meanIndex))
                            .ToArray();
                        vectorIndexList[i] = (current.VectorIndex, distanceByMean);
                    }
                    break;
                }
            }
            return clusterAllocation;
        }

        protected Cluster[] CreateClusters(int[] clusterAllocation, Cell[] cells) =>
            clusterAllocation
                .WithIndex()
                .GroupBy(x => x.Value)
                .Select(group => new Cluster(Generate.MupColor(), group.Select(x => cells[x.Index]).ToArray()))
                .ToArray();

        protected Bitmap BuildImage(Color[] colors, int width, int height) =>
            this.GetBytesFromColors(colors)
                .Into(bytes => this.BuildImage(bytes, width, height));

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