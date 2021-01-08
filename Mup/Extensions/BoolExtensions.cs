namespace Mup.Extensions
{
    public static class BoolExtensions
    {
        #region Null Or False

        /// <summary> Returns <see langword="true" /> if this nullable boolean is <see langword="null" /> or <see langword="false" />. </summary>
        public static bool NullOrFalse(this bool? nullable) =>
            ((!nullable.HasValue) || (!nullable.Value));
            
        #endregion
    }
}