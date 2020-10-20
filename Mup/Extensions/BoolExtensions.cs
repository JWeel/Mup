namespace Mup.Extensions
{
    public static class BoolExtensions
    {
        #region Not

        /// <summary> Returns <see langword="true" /> if this nullable boolean is <see langword="null" /> or <see langword="false" />. </summary>
        public static bool Not(this bool? nullable) =>
            ((!nullable.HasValue) || (!nullable.Value));
            
        #endregion
    }
}