using Mup.Extensions;
using System;
using System.Collections.Generic;

namespace Mup.Helpers
{
    /// <summary> Represents a timeline of objects which always has at least one element, and keeps track of one featured element. </summary>
    public class Timeline<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a starting element. </summary>
        public Timeline(T value)
        {
            this.Values = value.IntoList();
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

        /// <summary> Retrieves an element in the timeline by index. </summary>
        public T this[int index] => this.Values[index];

        /// <summary> The element in the timeline that is featured. </summary>
        public T Current => this[this.Index];

        /// <summary> Gets the number of elements contained in the <see cref="Timeline{T}"/>. </summary>
        public int Count => this.Values.Count;

        /// <summary> Determines whether the currently featured element is the first element in the timeline. </summary>
        public bool IsStartOfTimeline => (this.Index == 0);

        /// <summary> Determines whether the currently featured element is the last element in the timeline. </summary>
        public bool IsEndOfTimeline => (this.Index == this.Count - 1);

        /// <summary> Raised when the timeline begins featuring a different element. </summary>
        public event Action<T> OnChangedCurrent;

        #endregion

        #region Methods

        /// <summary> Adds an element to the timeline. A second parameter determines whether <see cref="Current"/> should change to the new element. </summary>
        /// <param name="feature"> Determines whether <see cref="Current"/> should change to the added element. </param>
        public void Add(T value, bool feature)
        {
            this.Values.Add(value);
            if (feature)
                this.Index = this.Count - 1;
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

        /// <summary> Returns the currently featured element. </summary>
        public static implicit operator T (Timeline<T> timeline) =>
            timeline.Current;

        #endregion
    }
}