using Mup.Extensions;
using System;
using System.Collections.Generic;

namespace Mup.Helpers
{
    public class Timeline<T>
    {
        #region Constructors

        public Timeline(T value)
        {
            this.Values = value.IntoList();
        }

        #endregion

        #region Properties

        protected List<T> Values { get; }

        public T this[int index] => this.Values[index];

        public event Action<T> Added;

        public event Action<int> Removed;

        #endregion

        #region Methods

        public void Add(T value)
        {
            this.Values.Add(value);
            this.Added?.Invoke(value);
        }

        public void RemoveAfter(int index)
        {
            var start = index + 1;
            var count = this.Values.Count - start;
            if (count < 1)
                return;
            this.Values.RemoveRange(start, count);
            this.Removed?.Invoke(count);
        }

        public void Reset() =>
            this.RemoveAfter(0);

        #endregion
    }
}