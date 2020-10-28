using System.Collections.Specialized;
using Mup.Extensions;
using Mup.Helpers;
using System;

namespace Mup.Models
{
    public class ImageModel
    {
        #region Constructor

        public ImageModel(byte[] data)
        {
            this.DataTimeline = new Timeline<byte[]>(data);
            this.DataTimeline.OnChangedCurrent += this.HandleChangedCurrent;
            this.DataTimeline.CollectionChanged += this.HandleCollectionChanged;
            this.SavedIndex = 0;
        }

        #endregion

        #region Properties

        protected Timeline<byte[]> DataTimeline { get; set; }

        protected int SavedIndex { get; set; }

        public bool IsModified => (this.DataTimeline.Index != this.SavedIndex);

        public byte[] Data => this.DataTimeline.Current;

        public bool IsStartOfTimeline => this.DataTimeline.IsStartOfTimeline;

        public bool IsEndOfTimeline => this.DataTimeline.IsEndOfTimeline;

        public int Index => this.DataTimeline.Index;

        public int Count => this.DataTimeline.Count;

        public event Action OnChangedCurrent;

        public event Action OnChangedCollection;

        #endregion

        #region Methods

        public void Undo() =>
            this.DataTimeline--;

        public void Redo() =>
            this.DataTimeline++;

        public void Save(string filePath)
        {
            this.Data.SaveToImage(filePath);
            this.SavedIndex = this.DataTimeline.Index;
        }

        public void Advance(byte[] data)
        {
            if (!this.DataTimeline.IsEndOfTimeline)
                this.DataTimeline.RemoveTrailing();
            this.DataTimeline.Add(data, feature: true);
        }

        protected void HandleChangedCurrent(byte[] data) =>
            this.OnChangedCurrent?.Invoke();

        protected void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) =>
            this.OnChangedCollection?.Invoke();

        #endregion
    }
}