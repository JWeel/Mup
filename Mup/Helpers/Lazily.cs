using System;

namespace Mup.Helpers
{
    /// <summary> Provides a mechanism for lazily loading values. </summary>
    /// <typeparam name="T"> The type of the lazily loaded value. </typeparam>
    /// <remarks> This way of lazy loading is conceptually different from doing it in a property getter.
    /// In the imagined use case of this class, a member would be in charge of loading (and should be assigned when declared),
    /// instead of handling the loading inside the property getter. So logic is moved to a different area. </remarks>
    public class Lazily<T>
    {
        #region Constructors

        /// <summary> Creates an instance with an unloaded value and an accessor which will be used to load it when requested. </summary>
        /// <param name="load"> An operation that returns the lazy value. It will only be called the first time the value is requested. </param>
        public Lazily(Func<T> load)
        {
            this.Load = load;
            this.HasValue = false;
            this.Value = default;
        }

        #endregion

        #region Properties

        /// <summary> Determines whether the lazy value has been loaded. </summary>
        protected bool HasValue { get; set; }

        /// <summary> Contains the lazy value after it has been loaded, or a <see langword="default"/> value until then. </summary>
        protected T Value { get; set; }

        /// <summary> The operation that returns the lazy value. It will only be called the first time the value is requested. </summary>
        protected Func<T> Load { get; set; }

        #endregion

        #region Lock

        /// <summary> A lock used when loading the lazy value the first time it is requested. </summary>
        protected object _lock = new object();

        #endregion

        #region Implicit Conversion

        /// <summary> Returns the lazy value. If it has not been loaded yet, it will be loaded first. </summary>
        public static implicit operator T(Lazily<T> lazy)
        {
            if (!lazy.HasValue)
            {
                lock (lazy._lock)
                {
                    if (!lazy.HasValue)
                    {
                        lazy.Value = lazy.Load();
                        lazy.HasValue = true;
                    }
                }
            }

            return lazy.Value;
        }

        #endregion
    }
}