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

        protected int Value { get; set; }

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

        #endregion

        #region Operators

        public static ClampedIndex operator +(ClampedIndex clamp, int value)
        {
            var sum = clamp.Value + value;
            clamp.Value = (sum > clamp.Max) ? clamp.Max : sum;
            return clamp;
        }

        public static ClampedIndex operator -(ClampedIndex clamp, int value)
        {
            var sum = clamp.Value - value;
            clamp.Value = (sum < clamp.Min) ? clamp.Min : sum;
            return clamp;
        }

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