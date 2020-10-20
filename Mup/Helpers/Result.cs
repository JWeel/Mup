namespace Mup.Models
{
    public class Result<T>
    {
        #region Constructors
            
        public Result(T value)
        {
            this.Value = value;
        }

        public Result(string error)
        {
            this.Error = error ?? string.Empty;
        }

        #endregion

        #region Properties
            
        public T Value { get; }
        
        public string Error { get; }

        #endregion
    }
}