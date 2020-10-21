using System.ComponentModel;
using Mup.Extensions;
using Mup.Helpers;
using System.IO;
using System;

namespace Mup.Models
{
    public class ImageModel : INotifyPropertyChanged
    {
        #region Constructor

        public ImageModel()
        {
            this.DataIndex = new ClampedIndex();
        }

        #endregion

        #region Properties

        protected ClampedIndex DataIndex { get; set; }

        protected Timeline<byte[]> DataTimeline { get; set; }

        protected int SavedIndex { get; set; }

        public string FileDirectory { get; set; }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.FileName)));
            }
        }

        public string FilePath => Path.Combine(this.FileDirectory, this.FileName);

        public bool IsModified => (this.DataIndex != this.SavedIndex);

        public byte[] Data => this.DataTimeline[this.DataIndex];

        public bool AtTimelineStart => (this.DataIndex == this.DataIndex.Min);

        public bool AtTimelineEnd => (this.DataIndex == this.DataIndex.Max);

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action OnAdvance;

        #endregion

        #region Methods

        protected void HandleTimelineAddition(byte[] value) =>
            this.DataIndex.Max++;

        protected void HandleTimelineRemoval(int count) =>
            this.DataIndex.Max -= count;

        public byte[] Undo() =>
            this.DataTimeline[--this.DataIndex];

        public byte[] Redo() =>
            this.DataTimeline[++this.DataIndex];

        public void Save()
        {
            if (!this.IsModified)
                return;
            this.Data.SaveToImage(this.FilePath);
            this.SavedIndex = this.DataIndex;
        }

        public void Load(string filePath)
        {
            this.FileName = Path.GetFileName(filePath);
            this.FileDirectory = Path.GetDirectoryName(filePath);

            var bytes = File.ReadAllBytes(filePath);
            this.DataTimeline = new Timeline<byte[]>(bytes);
            this.DataTimeline.Added += this.HandleTimelineAddition;
            this.DataTimeline.Removed += this.HandleTimelineRemoval;
            this.DataIndex.Reset();
            this.SavedIndex = 0;
        }

        public void Advance(byte[] data)
        {
            if (!this.AtTimelineEnd)
                this.DataTimeline.RemoveAfter(this.DataIndex);
            this.DataTimeline.Add(data);
            this.DataIndex++;
            this.OnAdvance?.Invoke();
        }

        #endregion
    }
}