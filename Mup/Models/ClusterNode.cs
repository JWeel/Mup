using Mup.External;
using System.Collections.Generic;

namespace Mup.Models
{
    public class ClusterNode
    {
        #region Constructors

        public ClusterNode(int index, Vector center, int clusterIndex, IDictionary<int, double> clusterDistanceMap)
        {
            this.Index = index;
            this.Center = center;
            this.ClusterIndex = clusterIndex;
            this.ClusterDistanceMap = clusterDistanceMap;
        }

        #endregion

        #region Properties

        public int Index { get; }

        public Vector Center { get; }

        public int ClusterIndex { get; set; }

        /// <summary> Mapping of distance to cluster mean by cluster index. </summary>
        public IDictionary<int, double> ClusterDistanceMap { get; set; }

        public double CurrentDistance => this.ClusterDistanceMap[this.ClusterIndex];

        #endregion
    }

    public class ClusterNodeComparer : IComparer<ClusterNode>
    {
        #region Constructors

        public ClusterNodeComparer(int clusterIndex)
        {
            this.ClusterIndex = clusterIndex;
        }

        #endregion

        #region Properties

        protected int ClusterIndex { get; }

        #endregion

        #region IComparer Implementation

        public int Compare(ClusterNode x, ClusterNode y)
        {
            // sort in reverse order
            var distanceOfX = x.ClusterDistanceMap[this.ClusterIndex];
            var distanceOfY = y.ClusterDistanceMap[this.ClusterIndex];
            return distanceOfY.CompareTo(distanceOfX);
        }

        #endregion
    }
}