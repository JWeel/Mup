using Mup.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Mup.Helpers
{
    /// <summary> Represents a timeline of objects which keeps track of one featured element. </summary>
    public class Timeline<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        #region Constants

        private int INDEX_NOT_FOUND = -1;

        #endregion

        #region Constructors

        /// <summary> Initializes a new instance without an initial element. </summary>
        public Timeline()
        {
            this.Values = new List<T>();
        }

        /// <summary> Initializes a new instance with an initial element that will be featured. </summary>
        public Timeline(T value)
        {
            this.Values = value.IntoList();
        }

        /// <summary> Initializes a new instance with a range of elements, the first of which will be featured. </summary>
        public Timeline(IEnumerable<T> values)
        {
            this.Values = new List<T>(values);
        }

        #endregion

        #region Properties

        /// <summary> Contains the elements that make up the timeline. </summary>
        protected List<T> Values { get; }

        /// <summary> Backing field for <see cref="Index"/>. </summary>
        private int _index;

        /// <summary> Represents the position of the <see cref="Current"/> element in the timeline. </summary>
        public int Index
        {
            get => _index;
            set
            {
                value.UnlessThen((value >= this.Count), this.Count - 1);
                value.UnlessThen((value < 0), 0);
                _index = value;
                this.OnChangedCurrent?.Invoke(this.Current);
            }
        }

        /// <summary> Retrieves an element in the timeline by index. If the index is out of range, a default element is returned. </summary>
        public T this[int index] =>
            this.TryGet(index, out var value) ? value : default;

        /// <summary> Tries to retrieves an element in the timeline by index and return it in an <see langword="out"/> parameter. A boolean return values indicates whether the index was in range. </summary>
        public bool TryGet(int index, out T value)
        {
            if ((index >= 0) && (index < this.Count))
            {
                value = this.Values[index];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary> The element in the timeline that is featured. </summary>
        public T Current => this[this.Index];

        /// <summary> Gets the number of elements contained in the <see cref="Timeline{T}"/>. </summary>
        public int Count => this.Values.Count;

        /// <summary> Determines whether the currently featured element is the first element in the timeline. </summary>
        public bool IsStartOfTimeline => (this.Index == 0);

        /// <summary> Determines whether the currently featured element is the last element in the timeline. </summary>
        public bool IsEndOfTimeline => (this.Index == this.Count - 1);

        /// <summary> Determines whether the timeline contains no elements. </summary>
        public bool IsEmpty => (this.Count == 0);

        /// <summary> Raised when the timeline begins featuring a different element. </summary>
        public event Action<T> OnChangedCurrent;

        /// <summary> Raised when the collection changes. </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Methods

        /// <summary> Adds an element to the timeline. A second parameter determines whether <see cref="Current"/> should change to the new element. </summary>
        /// <param name="feature"> Determines whether <see cref="Current"/> should change to the added element. </param>
        public void Add(T value, bool feature)
        {
            this.Values.Add(value);
            if (feature)
                this.Index = this.Count - 1;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
        }

        /// <summary> Removes a specified element from the timeline if it exists. </summary>
        /// <returns> <see langword="true"/> if the element existed in the timeline and was removed, otherwise <see langword="false"/>. </returns>
        public bool Remove(T value)
        {
            // fuck this shitty event so much
            var index = this.Values.IndexOf(value);
            if (index == INDEX_NOT_FOUND) return false;

            this.Values.Remove(value);
            if (index == this.Index)
                this.Index = index - 1;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value, index));
            return true;
        }

        /// <summary> Removes all elements in the timeline after the featured element.
        /// <para/> Raises event <see cref="OnRemove"/> only if elements are removed. </summary>
        public void RemoveTrailing() =>
            this.RemoveAfter(this.Index);

        /// <summary> Removes all elements in the timeline after a specified element.
        /// <br/> If the element does not exist, or there are no elements after it, nothing happens.
        /// <br/> If the <see cref="Current"/> element is removed, the last element in the timeline becomes current.
        /// <para/> Raises event <see cref="OnRemove"/> only if elements are removed. </summary>
        public void RemoveAfter(T value) =>
            this.RemoveAfter(this.Values.IndexOf(value));

        /// <summary> Removes all elements in the timeline after a specified index.
        /// <br/> If there are no elements after the provided index, nothing happens.
        /// <br/> If the <see cref="Current"/> element is removed, the last element in the timeline becomes current.
        /// <para/> Raises event <see cref="OnRemove"/> only if elements are removed. </summary>
        public void RemoveAfter(int index)
        {
            if ((index < 0) || (index >= this.Count))
                return;
            var amountToRemove = this.Count - index - 1;
            if (amountToRemove < 1)
                return;
            this.Values.RemoveRange(index + 1, amountToRemove);
            if (index < this.Index)
                this.Index = index;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this.Values));
        }

        /// <summary> Removes all elements after the initial element. </summary>
        public void Reset() =>
            this.RemoveAfter(0);

        /// <summary> Clears all elements and starts over with a specified value. </summary>
        public void Restart(T value)
        {
            this.Values.Clear();
            this.Values.Add(value);
            this.Index = 0;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary> Sets the given value as the featured element in the timeline. If the value does not exist in the timeline, nothing happens. </summary>
        /// <returns> <see langword="true"/> if the element exists in the timeline, otherwise <see langword="false"/>. </returns>
        public bool Feature(T value)
        {
            var index = this.Values.IndexOf(value);
            if (index == INDEX_NOT_FOUND)
                return false;
            this.Index = index;
            return true;
        }

        /// <summary> Modifies <see cref="Index"/>, then returns the instance. Used internally for operator methods. </summary>
        private Timeline<T> ModifyIndex(int offset)
        {
            this.Index += offset;
            return this;
        }

        #endregion

        #region Operators

        /// <summary> Advances the index by a specified amount, capped by the end of the timeline. </summary>
        public static Timeline<T> operator +(Timeline<T> timeline, int offset) =>
            timeline.ModifyIndex(offset);

        /// <summary> Regresses the index by a specified amount, capped by the start of the timeline. </summary>
        public static Timeline<T> operator -(Timeline<T> timeline, int offset) =>
            timeline.ModifyIndex(-offset);

        /// <summary> Advances the index, unless it is at the end of the timeline. </summary>
        public static Timeline<T> operator ++(Timeline<T> timeline) =>
            timeline += 1;

        /// <summary> Regresses the index, unless it is at the start of the timeline. </summary>
        public static Timeline<T> operator --(Timeline<T> timeline) =>
            timeline -= 1;

        #endregion

        #region IEnumerable Methods

        public IEnumerator<T> GetEnumerator() =>
            this.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        #endregion
    }
}