using Mup.Models;

namespace Mup.Extensions
{
    public static class ResultExtensions
    {
        #region Result Methods

        public static Result<T> Result<T>(this T value) =>
            new Result<T>(value);

        public static Result<T> Fail<T>(this T value, string error) =>
            new Result<T>(error);
            
        #endregion
    }
}