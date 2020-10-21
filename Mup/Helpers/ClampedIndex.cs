using System;
using Mup.Extensions;

namespace Mup.Helpers
{
    public class ClampedIndex
    {
        #region Constructors

        public ClampedIndex() : this(max: 0)
        {
        }

        public ClampedIndex(int max) : this(min: 0, max)
        {
        }

        public ClampedIndex(int min, int max)
        {
            this.Min = min;
            this.Max = max;
        }

        #endregion

        #region Properties

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                value = (value >= this.Max) ? this.Max : value;
                value = (value < this.Min) ? this.Min : value;
                _value = value;
                this.ValueChanged?.Invoke();
            }
        }

        private int _max;
        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                if (this.Value > _max)
                    this.Value = _max;
            }
        }

        private int _min;
        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                if (this.Value < _min)
                    this.Value = _min;
            }
        }

        public event Action ValueChanged;

        #endregion

        #region Operators

        public static ClampedIndex operator +(ClampedIndex clamp, int value) =>
            clamp.With(x => x.Value = x.Value + value);

        public static ClampedIndex operator -(ClampedIndex clamp, int value) =>
            clamp.With(x => x.Value = x.Value - value);

        public static ClampedIndex operator ++(ClampedIndex clamp) =>
            clamp += 1;

        public static ClampedIndex operator --(ClampedIndex clamp) =>
            clamp -= 1;

        public static implicit operator int(ClampedIndex clamp) =>
            clamp.Value;

        #endregion

        #region Methods

        public void Reset()
        {
            var min = this.Min;
            this.Value = min;
            this.Max = min;
        }

        #endregion
    }
}