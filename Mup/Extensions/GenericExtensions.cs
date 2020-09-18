using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mup.Extensions
{
    public static class EightExtensions
    {
        #region Enumeration Each/Defer/Enumerate

        /// <summary> Performs a specified action for each element in a sequence. </summary>
        public static void Each<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var element in enumerable)
                action(element);
        }

        /// <summary> Performs a specified action for each element and their index in a sequence. </summary>
        public static void Each<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            int index = -1;
            foreach (var element in enumerable)
            {
                checked
                { index++; }
                action(element, index);
            }
        }

        public static ICollection<Exception> EachSafe<T>(this IEnumerable<T> enumerable, Action<T> action) =>
             enumerable.Aggregate(new List<Exception>(),
                (exceptions, element) => exceptions.With(e => action.WithArgument(element).InvokeExcept(e.Add)));

        /// <summary> Defers an action to be performed for each enumerated element in a sequence. </summary>
        public static IEnumerable<T> Defer<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var element in enumerable)
                yield return element.With(action);
        }

        /// <summary> Defers an action to be performed for each enumerated element and their index in a sequence. </summary>
        public static IEnumerable<T> Defer<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            int index = -1;
            foreach (var element in enumerable)
            {
                checked
                { index++; }
                yield return element.With(x => action(x, index));
            }
        }

        /// <summary> Moves through each element in a sequence without the overhead of storing them in a different data structure. </summary>
        public static void Iterate<T>(this IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
        }

        /// <summary> Moves through a specified number of elements in a sequence. </summary>
        /// <param name="iterations"> The number of elements to iterate over. If this exceeds the number of elements in the sequence, the remaining enumerations are ignored. </param>
        public static void Iterate<T>(this IEnumerable<T> enumerable, int iterations)
        {
            var enumerations = 0;
            var enumerator = enumerable.GetEnumerator();
            while ((enumerations++ < iterations) && (enumerator.MoveNext())) ;
        }

        #endregion

        #region Identical To First By

        /// <summary> Determines whether all items in a sequence are identical to the first item, using the default
        /// equality comparer of a property accessed through a selector to determine whether items are equal.
        /// <para/> If the sequence is empty, this will return <see langword="false"/>. </summary>
        /// <param name="treatNullsAsEqual"> Can be set to <see langword="true"/> to treat <see langword="null"/> values as identical to the first item. </param>
        public static bool IdenticalToFirstBy<TItem, TProperty>(this IEnumerable<TItem> source,
            Func<TItem, TProperty> selector, bool treatNullsAsEqual = false)
        {
            var enumerator = source.GetEnumerator();
            if (false == enumerator.MoveNext())
            {
                return false;
            }

            var firstValue = selector(enumerator.Current);
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if ((treatNullsAsEqual) && (null == item))
                {
                    continue;
                }
                else if (null == item)
                {
                    return false;
                }

                var currentValue = selector(item);
                if ((null == firstValue) && (null != currentValue))
                {
                    return false;
                }
                if ((null == firstValue) && (null == currentValue))
                {
                    continue;
                }
                if ((treatNullsAsEqual) && (null == currentValue))
                {
                    continue;
                }
                if (false == currentValue.Equals(firstValue))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Find Not Matching First By

        /// <summary> Returns a sequence of items in a specified sequence that are not identical to its first item, using the
        /// default equality comparer of a property accessed through a selector to determine whether items are equal. </summary>
        /// <param name="treatNullsAsEqual"> Can be set to <see langword="true"/> to treat <see langword="null"/> values as identical to the first item. </param>
        public static IEnumerable<TItem> FindNotMatchingFirstBy<TItem, TProperty>(this IEnumerable<TItem> source,
            Func<TItem, TProperty> selector, bool treatNullsAsEqual = false)
        {
            var enumerator = source.GetEnumerator();
            if (false == enumerator.MoveNext())
            {
                return source;
            }

            var firstValue = selector(enumerator.Current);
            return enumerator.Enumerate().Where(item =>
            {
                var currentValue = selector(item);
                if ((null == firstValue) && (null != currentValue))
                {
                    return true;
                }
                if ((null == firstValue) && (null == currentValue))
                {
                    return false;
                }
                if ((treatNullsAsEqual) && (null == currentValue))
                {
                    return false;
                }
                return false == firstValue.Equals(currentValue);
            });
        }

        #endregion

        #region Get Enum Values

        /// <summary> If this type is an <see langword="enum"/>, all values defined for this type are returned. 
        /// <br/> Otherwise, an <see cref="InvalidOperationException"/> will be thrown. </summary>
        /// <exception cref="InvalidOperationException"> Type is not a enum type. </exception>
        public static TEnum[] GetValues<TEnum>(this Type enumType) where  TEnum : Enum => 
             (TEnum[]) Enum.GetValues(enumType);

        #endregion

        #region To/From

        /// <summary> Generates an interval of integers using this int as inclusive start and a passed value as exclusive end. </summary>
        /// <remarks> The interval generated is [start,end) with a size equal to end - start. </remarks>
        public static IEnumerable<int> To(this int start, int end) =>
             Enumerable.Range(start, end - start);

        /// <summary> Generates an interval of integers using this int as exclusive end and a passed value as inclusive start. </summary>
        /// <remarks> The interval generated is [start,end) with a size equal to end - start. </remarks>
        public static IEnumerable<int> From(this int end, int start) =>
             start.To(end);

        ///// <summary> Generates a sequence of integers within a specified range, using the instance as start point and a passed value as end point.
        ///// <para/> An optional <see langword="bool"/> can be set to <see langword="true"/> to have the end value included in the range. </summary>
        //public static IEnumerable<int> To(this int start, int end, bool inclusive = false)
        //    => Enumerable.Range(start, end - start + (inclusive ? 1 : 0));

        ///// <summary> Generates a sequence of integers within a specified range, using the instance as end point and a passed value as start point.
        ///// <para/> An optional <see langword="bool"/> can be set to <see langword="true"/> to have the end value included in the range. </summary>
        //public static IEnumerable<int> From(this int end, int start, bool inclusive = false)
        //    => start.To(end, inclusive);

        #endregion

        #region Parse Or Default

        public static TEnum ParseEnumOrDefault<TEnum>(this string value) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, out var enumValue))
                return enumValue;
            else
                return default;
        }

        public static int ParseIntOrDefault(this string value)
        {
            if (int.TryParse(value, out var enumValue))
                return enumValue;
            else
                return default;
        }

        #endregion

        #region First And Mutate

        /// <summary> Finds the first element of the sequence and returns it after mutating it with an action. Returns a default value if no element is found. </summary>
        public static T FirstAndMutateOrDefault<T>(this IEnumerable<T> source, Action<IEnumerable<T>, T> action)
        {
            return source.FirstAndMutateOrDefault(_ => true, action);
        }

        /// <summary> Finds the first element of the sequence that satisfies a condition and returns it after mutating it with an action. Returns a default value if no element is found. </summary>
        public static T FirstAndMutateOrDefault<T>(this IEnumerable<T> source, Predicate<T> predicate, Action<IEnumerable<T>, T> action)
        {
            if (source.TryGetFirstOrDefault(predicate, out var first))
                action(source, first);
            return first;
        }

        /// <summary> Finds the first element of the sequence and returns it after mutating it with an action. Throws <see cref="InvalidOperationException"/> if no element is found. </summary>
        public static T FirstAndMutate<T>(this IEnumerable<T> source, Action<IEnumerable<T>, T> action)
        {
            return source.FirstAndMutate(_ => true, action);
        }

        /// <summary> Finds the first element of the sequence that satisfies a condition and returns it after mutating it with an action. Throws <see cref="InvalidOperationException"/> if no element is found. </summary>
        public static T FirstAndMutate<T>(this IEnumerable<T> source, Func<T, bool> predicate, Action<IEnumerable<T>, T> action)
        {
            var first = source.First(predicate);
            action(source, first);
            return first;
        }

        #endregion

        #region Distinct By

        /// <summary> Returns distinct elements in a sequence, with distinctiveness based on comparing a property accessed through a selector. </summary>
        /// <remarks> Uses the default equality comparer to compare properties. </remarks>
        // We could also do 'source.GroupBy(selector).Select(x => x.First())'
        public static IEnumerable<T> DistinctBy<T, TProperty>(this IEnumerable<T> source, Func<T, TProperty> selector)
        {
            var uniqueValues = new HashSet<TProperty>();
            foreach (var element in source)
            {
                if (uniqueValues.Add(selector(element)))
                {
                    yield return element;
                }
            }
        }

        #endregion

        #region Select Distinct By

        /// <summary> Performs a group by followed by a select using the first of the group resulting in a transformed distinct by. </summary>
        public static IEnumerable<TTarget> SelectDistinctBy<TSource, TProperty, TTarget>(this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector, Func<TSource, TTarget> converter) =>
             source
                .GroupBy(selector)
                .Select(group => converter(group.First()));

        /// <summary> Performs a group by followed by an indexed select using the first of the group resulting in a transformed distinct by. </summary>
        public static IEnumerable<TTarget> SelectDistinctBy<TSource, TProperty, TTarget>(this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector, Func<TSource, int, TTarget> converter) =>
             source
                .GroupBy(selector)
                .Select((group, index) => converter(group.First(), index));

        #endregion

        #region Range

        /// <summary> Returns a range of indices from <see langword="0"/> up to this value (exclusive upper bound). </summary>
        /// <exception cref="ArgumentOutOfRangeException"> The <paramref name="size"/> is less than <see langword="0"/>. </exception>
        public static IEnumerable<int> Range(this int size) =>
             size.IsNegative() ? throw new ArgumentOutOfRangeException(nameof(size)) : Enumerable.Range(0, size);

        #endregion

        #region Sum Range

        /// <summary> Calculates the sum of all integers between <see langword="0"/> and this value (exclusive upper bound). </summary>
        public static int SumRange(this int x) =>
             x.IsNegative() ? throw new ArgumentOutOfRangeException(nameof(x)) : x * (x - 1) / 2;

        #endregion

        #region Except By

        /// <summary> Returns elements in the sequence that are not found in a second sequence, with distinctiveness based on a property accessed through a selector.
        /// <para/> A boolean value indicates whether or not to return duplicates in the first sequence multiple times, assuming they are not in the second sequence. </summary>
        public static IEnumerable<T> ExceptBy<T, TProperty>(this IEnumerable<T> source, IEnumerable<T> exclusions,
            Func<T, TProperty> selector, bool allowDuplicateReturnValues = false)
        {
            var excludes = new HashSet<TProperty>(exclusions.Select(selector));
            foreach (var element in source)
            {
                // regular operation -> duplicate return values allowed
                if (allowDuplicateReturnValues && !excludes.Contains(selector(element)))
                    yield return element;

                // set operation -> no duplicate return values
                if (!allowDuplicateReturnValues && excludes.Add(selector(element)))
                    yield return element;
            }
        }

        #endregion

        #region In

        /// <summary> Determines whether this value is equal to any parameter. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, params T[] allowedValues) =>
             value.In((IEnumerable<T>) allowedValues);// allowedValues.Contains(value);

        /// <summary> Determines whether this value is equal to any value in an enumerable. </summary>
        /// <remarks> Uses <see cref="EqualityComparer{T}.Default"/> to compare values. This property returns a static readonly member, and is thus created only once. </remarks>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEnumerable<T> allowedValues) =>
             allowedValues.Contains(value);

        /// <summary> Determines whether this value is equal to any parameter by using a specified <see cref="IEqualityComparer{}"/>. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEqualityComparer<T> comparer, params T[] allowedValues) =>
             value.In(allowedValues, comparer);// allowedValues.Contains(value);

        /// <summary> Determines whether this value is equal to any value in an enumerable by using a specified <see cref="IEqualityComparer{}"/>. </summary>
        /// <remarks> Uses <see cref="EqualityComparer{T}.Default"/> to compare values. This property returns a static readonly member, and is thus created only once. </remarks>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEnumerable<T> allowedValues, IEqualityComparer<T> comparer) =>
             allowedValues.Contains(value, comparer);

        #endregion

        #region Has Attribute

        /// <summary> Returns <see langword="true"/> if the type of the passed object has an attribute of a specified type. </summary>
        public static bool HasAttribute<TAttribute>(this object value, bool inherit = false) where  TAttribute : Attribute => 
             value.GetType()
                .GetCustomAttributes(typeof(TAttribute), inherit)
                .Length > 0;

        /// <summary> Returns <see langword="true"/> if the type of the passed object has an attribute of a specified type. </summary>
        public static bool HasAttribute<T>(this T _, Type attributeType, bool inherit = false) =>
             typeof(T)
                .GetCustomAttributes(attributeType, inherit)
                .Length > 0;

        #endregion

        #region AutoResetEvent extensions

        /// <summary> Blocks the current thread until the current <see cref="WaitHandle"/> has received a signal a specified number of times. </summary>
        public static void WaitMultipleTimes(this AutoResetEvent autoReset, int amountOfTimes) =>
             amountOfTimes.InvokeThisManyTimes(() => autoReset.WaitOne());

        #endregion

        #region Is Null Or Empty Or WhiteSpace

        /// <summary> Indicates whether this object is <see langword="null"/>. </summary>
        public static bool IsNull<T>(this T value) where T : class => (null == value);

        /// <summary> Indicates whether this nullable struct is <see langword="null"/>. </summary>
        public static bool IsNull<T>(this T? value) where T : struct => (null == value);

        /// <summary> Indicates whether this string is <see langword="null"/> or <see cref="string.Empty"/>. </summary>
        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);

        /// <summary> Indicates whether this string is <see langword="null"/>, <see cref="string.Empty"/>, or consists only of white-space characters. </summary>
        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        /// <summary> Indicates whether this object is not <see langword="null"/>. </summary>
        public static bool IsNotNull<T>(this T value) where T : class => (null != value);

        /// <summary> Indicates whether this nullable struct is not <see langword="null"/>. </summary>
        public static bool IsNotNull<T>(this T? value) where T : struct => (null != value);

        /// <summary> Indicates whether this string is neither <see langword="null"/> nor <see cref="string.Empty"/>. </summary>
        public static bool IsNotNullNorEmpty(this string value) => !value.IsNullOrEmpty();

        /// <summary> Indicates whether this string is not <see langword="null"/>, not <see cref="string.Empty"/>, or does not consist only of white-space characters. </summary>
        public static bool IsNotNullNorWhiteSpace(this string value) => !value.IsNullOrWhiteSpace();

        #endregion

        #region Is Null Or Empty Array

        /// <summary> Indicates whether this array is <see langword="null"/> or has no elements. </summary>
        public static bool IsNullOrEmpty<T>(this T[] array) => array.IsNull() || array.Length == 0;

        #endregion

        #region Self

        // This needs to be a global method, not an extension. eg - using static GlobalMethods;

        public static Func<T, T> Self<T>(this T _) => t => t;

        public static T Self2<T>(this T value) => value;

        #endregion

        #region Pass/DoNothing

        // rename to NoOp ?? or Noop ??

        // maybe rename to Void. get rid of one of the two.

        // comment is incorrect? unable to use it in expression-bodied statements

        // Reminder: Using <T> instead of object so that value types don't get boxed, counts for all the other methods too
        // Does mean some methods need to be duplicated because of class vs nullable<struct> with constraints

        /// <summary> Does nothing. Can be used in expression-bodied statements and lambdas. </summary>
        public static void Pass<T>(this T _) { }

        /// <summary> Does nothing. Can be used in expression-bodied statements and lambdas. </summary>
        public static void DoNothing<T>(this T _) { }

        #endregion

        #region None

        /// <summary> Returns <see langword="true"/> if the given <see cref="IEnumerable{T}"/> contains no elements. </summary>
        /// <exception cref="ArgumentNullException"> Provided <paramref name="source"/> is null.</exception>
        public static bool None<T>(this IEnumerable<T> source) => !source.Any();

        /// <summary> Returns <see langword="true"/> if the given <see cref="IEnumerable{T}"/> is <see langword="null"/> or contains no elements. </summary>
        public static bool NullOrNone<T>(this IEnumerable<T> source) => (source?.Any()).IsFalse();

        #endregion

        #region Nullable Boolean Extensions

        /// <summary> Returns <see langword="true"/> if this nullable boolean is <see langword="true"/>. </summary>
        public static bool IsTrue(this bool? value) => value == true;

        /// <summary> Returns <see langword="true"/> if this nullable boolean is <see langword="false"/>. </summary>
        public static bool IsFalse(this bool? value) => value == false;

        /// <summary> Returns the value behind this nullable boolean, or a default <see cref="bool"/> value if it is <see langword="null"/>. </summary>
        public static bool ToBoolean(this bool? value) => value ?? default;

        #endregion

        #region Less Than / Greater Than / Not Equal / Positive / Negative

        #region Int

        /// <summary> Determines whether the current <see cref="int"/> is less than the specified <see cref="int"/> value. </summary>
        public static bool IsLessThan(this int current, int value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="int"/> is less than or equal to the specified <see cref="int"/> value. </summary>
        public static bool IsLessOrEquals(this int current, int value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="int"/> is greater than the specified <see cref="int"/> value. </summary>
        public static bool IsGreaterThan(this int current, int value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="int"/> is greater than or equal to the specified <see cref="int"/> value. </summary>
        public static bool IsGreaterOrEquals(this int current, int value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="int"/> is not equal to the specified <see cref="int"/> value. </summary>
        public static bool NotEquals(this int current, int value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="int"/> is a positive number. </summary>
        public static bool IsPositive(this int current) => current > 0;

        /// <summary> Determines whether the current <see cref="int"/> is a negative number. </summary>
        public static bool IsNegative(this int current) => current < 0;

        #endregion

        #region Long

        /// <summary> Determines whether the current <see cref="long"/> is less than the specified <see cref="long"/> value. </summary>
        public static bool IsLessThan(this long current, long value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="long"/> is less than or equal to the specified <see cref="long"/> value. </summary>
        public static bool IsLessOrEquals(this long current, long value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="long"/> is greater than the specified <see cref="long"/> value. </summary>
        public static bool IsGreaterThan(this long current, long value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="long"/> is greater than or equal to the specified <see cref="long"/> value. </summary>
        public static bool IsGreaterOrEquals(this long current, long value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="long"/> is not equal to the specified <see cref="long"/> value. </summary>
        public static bool NotEquals(this long current, long value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="long"/> is a positive number. </summary>
        public static bool IsPositive(this long current) => current > 0L;

        /// <summary> Determines whether the current <see cref="long"/> is a negative number. </summary>
        public static bool IsNegative(this long current) => current < 0L;

        #endregion

        #region Short

        /// <summary> Determines whether the current <see cref="short"/> is less than the specified <see cref="short"/> value. </summary>
        public static bool IsLessThan(this short current, short value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="short"/> is less than or equal to the specified <see cref="short"/> value. </summary>
        public static bool IsLessOrEquals(this short current, short value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="short"/> is greater than the specified <see cref="short"/> value. </summary>
        public static bool IsGreaterThan(this short current, short value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="short"/> is greater than or equal to the specified <see cref="short"/> value. </summary>
        public static bool IsGreaterOrEquals(this short current, short value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="short"/> is not equal to the specified <see cref="short"/> value. </summary>
        public static bool NotEquals(this short current, short value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="short"/> is a positive number. </summary>
        public static bool IsPositive(this short current) => current > 0;

        /// <summary> Determines whether the current <see cref="short"/> is a negative number. </summary>
        public static bool IsNegative(this short current) => current < 0;

        #endregion

        #region Byte

        /// <summary> Determines whether the current <see cref="byte"/> is less than the specified <see cref="byte"/> value. </summary>
        public static bool IsLessThan(this byte current, byte value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="byte"/> is less than or equal to the specified <see cref="byte"/> value. </summary>
        public static bool IsLessOrEquals(this byte current, byte value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="byte"/> is greater than the specified <see cref="byte"/> value. </summary>
        public static bool IsGreaterThan(this byte current, byte value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="byte"/> is greater than or equal to the specified <see cref="byte"/> value. </summary>
        public static bool IsGreaterOrEquals(this byte current, byte value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="byte"/> is not equal to the specified <see cref="byte"/> value. </summary>
        public static bool NotEquals(this byte current, byte value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="byte"/> is a positive number. </summary>
        public static bool IsPositive(this byte current) => current > 0;

        /// <summary> Determines whether the current <see cref="byte"/> is a negative number. </summary>
        public static bool IsNegative(this byte current) => current < 0;

        #endregion

        #region Unsigned Int

        /// <summary> Determines whether the current <see cref="uint"/> is less than the specified <see cref="uint"/> value. </summary>
        public static bool IsLessThan(this uint current, uint value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="uint"/> is less than or equal to the specified <see cref="uint"/> value. </summary>
        public static bool IsLessOrEquals(this uint current, uint value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="uint"/> is greater than the specified <see cref="uint"/> value. </summary>
        public static bool IsGreaterThan(this uint current, uint value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="uint"/> is greater than or equal to the specified <see cref="uint"/> value. </summary>
        public static bool IsGreaterOrEquals(this uint current, uint value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="uint"/> is not equal to the specified <see cref="uint"/> value. </summary>
        public static bool NotEquals(this uint current, uint value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="uint"/> is a positive number. </summary>
        public static bool IsPositive(this uint current) => current > 0u;

        /// <summary> Determines whether the current <see cref="uint"/> is a negative number. </summary>
        public static bool IsNegative(this uint current) => current < 0u;

        #endregion

        #region Unsigned Long

        /// <summary> Determines whether the current <see cref="ulong"/> is less than the specified <see cref="ulong"/> value. </summary>
        public static bool IsLessThan(this ulong current, ulong value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="ulong"/> is less than or equal to the specified <see cref="ulong"/> value. </summary>
        public static bool IsLessOrEquals(this ulong current, ulong value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="ulong"/> is greater than the specified <see cref="ulong"/> value. </summary>
        public static bool IsGreaterThan(this ulong current, ulong value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="ulong"/> is greater than or equal to the specified <see cref="ulong"/> value. </summary>
        public static bool IsGreaterOrEquals(this ulong current, ulong value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="ulong"/> is not equal to the specified <see cref="ulong"/> value. </summary>
        public static bool NotEquals(this ulong current, ulong value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="ulong"/> is a positive number. </summary>
        public static bool IsPositive(this ulong current) => current > 0ul;

        /// <summary> Determines whether the current <see cref="ulong"/> is a negative number. </summary>
        public static bool IsNegative(this ulong current) => current < 0ul;

        #endregion

        #region Unsigned Short

        /// <summary> Determines whether the current <see cref="ushort"/> is less than the specified <see cref="ushort"/> value. </summary>
        public static bool IsLessThan(this ushort current, ushort value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="ushort"/> is less than or equal to the specified <see cref="ushort"/> value. </summary>
        public static bool IsLessOrEquals(this ushort current, ushort value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="ushort"/> is greater than the specified <see cref="ushort"/> value. </summary>
        public static bool IsGreaterThan(this ushort current, ushort value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="ushort"/> is greater than or equal to the specified <see cref="ushort"/> value. </summary>
        public static bool IsGreaterOrEquals(this ushort current, ushort value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="ushort"/> is not equal to the specified <see cref="ushort"/> value. </summary>
        public static bool NotEquals(this ushort current, ushort value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="ushort"/> is a positive number. </summary>
        public static bool IsPositive(this ushort current) => current > 0;

        /// <summary> Determines whether the current <see cref="ushort"/> is a negative number. </summary>
        public static bool IsNegative(this ushort current) => current < 0;

        #endregion

        #region Signed Byte

        /// <summary> Determines whether the current <see cref="sbyte"/> is less than the specified <see cref="sbyte"/> value. </summary>
        public static bool IsLessThan(this sbyte current, sbyte value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="sbyte"/> is less than or equal to the specified <see cref="sbyte"/> value. </summary>
        public static bool IsLessOrEquals(this sbyte current, sbyte value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="sbyte"/> is greater than the specified <see cref="sbyte"/> value. </summary>
        public static bool IsGreaterThan(this sbyte current, sbyte value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="sbyte"/> is greater than or equal to the specified <see cref="sbyte"/> value. </summary>
        public static bool IsGreaterOrEquals(this sbyte current, sbyte value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="sbyte"/> is not equal to the specified <see cref="sbyte"/> value. </summary>
        public static bool NotEquals(this sbyte current, sbyte value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="sbyte"/> is a positive number. </summary>
        public static bool IsPositive(this sbyte current) => current > 0;

        /// <summary> Determines whether the current <see cref="sbyte"/> is a negative number. </summary>
        public static bool IsNegative(this sbyte current) => current < 0;

        #endregion

        #region Float

        /// <summary> Determines whether the current <see cref="float"/> is less than the specified <see cref="float"/> value. </summary>
        public static bool IsLessThan(this float current, float value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="float"/> is less than or equal to the specified <see cref="float"/> value. </summary>
        public static bool IsLessOrEquals(this float current, float value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="float"/> is greater than the specified <see cref="float"/> value. </summary>
        public static bool IsGreaterThan(this float current, float value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="float"/> is greater than or equal to the specified <see cref="float"/> value. </summary>
        public static bool IsGreaterOrEquals(this float current, float value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="float"/> is not equal to the specified <see cref="float"/> value. </summary>
        public static bool NotEquals(this float current, float value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="float"/> is a positive number. </summary>
        public static bool IsPositive(this float current) => current > 0f;

        /// <summary> Determines whether the current <see cref="float"/> is a negative number. </summary>
        public static bool IsNegative(this float current) => current < 0f;

        #endregion

        #region Double

        /// <summary> Determines whether the current <see cref="double"/> is less than the specified <see cref="double"/> value. </summary>
        public static bool IsLessThan(this double current, double value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="double"/> is less than or equal to the specified <see cref="double"/> value. </summary>
        public static bool IsLessOrEquals(this double current, double value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="double"/> is greater than the specified <see cref="double"/> value. </summary>
        public static bool IsGreaterThan(this double current, double value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="double"/> is greater than or equal to the specified <see cref="double"/> value. </summary>
        public static bool IsGreaterOrEquals(this double current, double value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="double"/> is not equal to the specified <see cref="double"/> value. </summary>
        public static bool NotEquals(this double current, double value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="double"/> is a positive number. </summary>
        public static bool IsPositive(this double current) => current > 0d;

        /// <summary> Determines whether the current <see cref="double"/> is a negative number. </summary>
        public static bool IsNegative(this double current) => current < 0d;

        #endregion

        #region Decimal

        /// <summary> Determines whether the current <see cref="decimal"/> is less than the specified <see cref="decimal"/> value. </summary>
        public static bool IsLessThan(this decimal current, decimal value) =>
             current < value;

        /// <summary> Determines whether the current <see cref="decimal"/> is less than or equal to the specified <see cref="decimal"/> value. </summary>
        public static bool IsLessOrEquals(this decimal current, decimal value) =>
             current <= value;

        /// <summary> Determines whether the current <see cref="decimal"/> is greater than the specified <see cref="decimal"/> value. </summary>
        public static bool IsGreaterThan(this decimal current, decimal value) =>
             current > value;

        /// <summary> Determines whether the current <see cref="decimal"/> is greater than or equal to the specified <see cref="decimal"/> value. </summary>
        public static bool IsGreaterOrEquals(this decimal current, decimal value) =>
             current >= value;

        /// <summary> Determines whether the current <see cref="decimal"/> is not equal to the specified <see cref="decimal"/> value. </summary>
        public static bool NotEquals(this decimal current, decimal value) =>
             !current.Equals(value);

        /// <summary> Determines whether the current <see cref="decimal"/> is a positive number. </summary>
        public static bool IsPositive(this decimal current) => current > 0m;

        /// <summary> Determines whether the current <see cref="decimal"/> is a negative number. </summary>
        public static bool IsNegative(this decimal current) => current < 0m;

        #endregion

        #endregion

        #region Join

        /// <summary> Concatenates the members of the <see cref="string"/> collection, using the specified separator between each member. </summary>
        public static string Join(this IEnumerable<string> strings, string separator) =>
             string.Join(separator, strings);

        #endregion

        #region Repeat (string)

        // conflicts with Repeat<T> which returns IEnumerable<T> -> should use different name

        /// <summary> Returns a <see cref="string"/> made of the current value repeated a specified number of times. </summary>
        public static string Repeat(this string value, int count) =>
             value.Repeat(count, string.Empty);

        /// <summary> Returns a <see cref="string"/> made of the current value repeated a specified number of times, with a separator between each repetition. </summary>
        public static string Repeat(this string value, int count, string separator) =>
             count.Range().Select(_ => value).Join(separator);

        #endregion

        #region String Replace Multi

        public static string ReplaceMultiWithStringDotReplace(this string value, string replacement, params string[] replaceables)
        {
            if ((replaceables?.None()).IsTrue())
                throw new ArgumentException($"Parameter '{nameof(replaceables)}' cannot be null or empty.");

            foreach (var replaceable in replaceables)
                value = value.Replace(replaceable, replacement);

            return value;
        }

        public static string ReplaceMultiWithRegexDotReplace(this string value, string replacement, params string[] replaceables)
        {
            if ((replaceables?.None()).IsTrue())
                throw new ArgumentException($"Parameter '{nameof(replaceables)}' cannot be null or empty.");

            string pattern = replaceables.Select(x => $"({x})").Join("|");
            return Regex.Replace(value, pattern, replacement, RegexOptions.Compiled);
        }

        public static string ReplaceMultiWithStringBuilderDotReplace(this string value, string replacement, params string[] replaceables)
        {
            if ((replaceables?.None()).IsTrue())
                throw new ArgumentException($"Parameter '{nameof(replaceables)}' cannot be null or empty.");

            var builder = new StringBuilder(value);
            foreach (var replaceable in replaceables)
                builder.Replace(replaceable, replacement);

            return builder.ToString();
        }

        /// <summary> Replaces all occurrences of specified <paramref name="replaceables"/> in this <see cref="string"/> with a specified <paramref name="replacement"/>. </summary>
        public static string Replace(this string value, string replacement, params string[] replaceables)
        {
            replaceables.Each(x => value = value.Replace(x, replacement));
            return value;
        }

        #endregion

        #region With Side Effect

        // maybe rename to Side or SideEffect
        /// <summary> Returns this instance after passing it as argument to the invocation of a given <paramref name="action"/>. </summary>
        /// <remarks> The instance will be returned even if <paramref name="action"/> is null (in which case it will not be invoked). </remarks>
        [DebuggerStepThrough]
        public static T With<T>(this T value, Action<T> action)
        {
            action?.Invoke(value);
            return value;
        }

        #endregion

        #region And/Or/Nor

        /// <summary> Returns <see langword="true"/> if both booleans are <see langword="true"/>. </summary>
        public static bool And(this bool initial, bool and) => initial && and;

        /// <summary> Returns <see langword="true"/> if one or both booleans are <see langword="true"/>. </summary>
        public static bool Or(this bool initial, bool or) => initial || or;

        /// <summary> Returns <see langword="true"/> if both booleans are <see langword="false"/>. </summary>
        public static bool Nor(this bool initial, bool nor) => !initial && !nor;

        /// <summary> Returns <see langword="true"/> if one boolean is <see langword="true"/> and the other is <see langword="false"/>. </summary>
        public static bool Xor(this bool initial, bool xor) => (initial && !xor) || (!initial && xor);

        #endregion

        #region Is List

        /// <summary> Returns <see langword="true"/> if the <see cref="Type"/> is a generic <see cref="List{}"/>. </summary>
        public static bool IsList(this Type type) =>
             type.IsGenericType && (typeof(List<>) == type.GetGenericTypeDefinition());

        #endregion

        #region Split Words

        /// <summary> Splits a camelcased <see cref="string"/> of concatenated words into separate words. </summary>
        public static IEnumerable<string> SplitWords(this string value)
        {
            const string pattern = @"[A-Z][a-z]+|[A-Z]+(?![a-z])|[a-z]+";
            var matches = Regex.Matches(value, pattern);
            return matches.Select(x => x.Value);
        }

        #endregion

        #region Coalesce Null

        // TODO use Func<T> parameters for alternatives

        // why ever use this over ?? ??? -> maybe in some method chaining you need parentheses otherwise? no proof yet
        // another downside is it doesn't allow throwing exceptions in the alternative
        // but it lets you chain a property/method call without needing parentheses
        /// <summary> Returns the value if it is not <see langword="null"/>, or an alternative value if it is. </summary>
        public static T CoalesceNull<T>(this T value, T alternative) where  T : class => 
             value ?? alternative;

        // why ever use this over ?? ???
        /// <summary> Returns the value if it is not <see langword="null"/>, or an alternative value if it is. </summary>
        public static T CoalesceNull<T>(this T? value, T alternative) where  T : struct => 
             value ?? alternative;

        /// <summary> Returns the value if it is not <see cref="string.IsNullOrEmpty(string)"/>, or an alternative value if it is. </summary>
        public static string CoalesceNullOrEmpty(this string value, string alternative) =>
             value.IsNotNullNorEmpty() ? value : alternative;

        /// <summary> Returns the value if it is not <see cref="string.IsNullOrWhiteSpace(string)"/>, or an alternative value if it is. </summary>
        public static string CoalesceNullOrWhiteSpace(this string value, string alternative) =>
             value.IsNotNullNorWhiteSpace() ? value : alternative;

        #endregion

        #region Into List/Array/Set/Dictionary

        /// <summary> Returns a <see cref="List{T}"/> containing only this <paramref name="value"/>. </summary> 
        public static List<T> IntoList<T>(this T value) => new List<T>(1) { value };

        /// <summary> Returns an array of type <typeparamref name="T"/> containing only this <paramref name="value"/>. </summary> 
        public static T[] IntoArray<T>(this T value) => new T[1] { value };

        /// <summary> Returns a <see cref="HashSet{T}"/> containing only this <paramref name="value"/>. </summary> 
        public static HashSet<T> IntoSet<T>(this T value) => new HashSet<T>(1) { value };

        /// <summary> Returns a <see cref="Queue{T}"/> containing only this <paramref name="value"/>. </summary> 
        public static Queue<T> IntoQueue<T>(this T value) => new Queue<T>(value.Yield());

        /// <summary> Returns a <see cref="Stack{T}"/> containing only this <paramref name="value"/>. </summary> 
        public static Stack<T> IntoStack<T>(this T value) => new Stack<T>(value.Yield());

        /// <summary> Returns a <see cref="Dictionary{T,T}"/> containing only the key and value in this <paramref name="pair"/>. </summary> 
        public static Dictionary<TKey, TValue> IntoDictionary<TKey, TValue>(this KeyValuePair<TKey, TValue> pair) =>
             new Dictionary<TKey, TValue>(1) { { pair.Key, pair.Value } };

        /// <summary> Returns a <see cref="Dictionary{T,T}"/> containing only the key and value in this <paramref name="tuple"/>. </summary> 
        public static Dictionary<TKey, TValue> IntoDictionary<TKey, TValue>(this (TKey Key, TValue Value) tuple) =>
             new Dictionary<TKey, TValue>(1) { { tuple.Key, tuple.Value } };

        /// <summary> Returns a <see cref="Dictionary{T,T}"/> containing only this <paramref name="key"/> with the given <paramref name="value"/>. </summary> 
        public static Dictionary<TKey, TValue> IntoDictionaryWithValue<TKey, TValue>(this TKey key, TValue value) =>
             new Dictionary<TKey, TValue>(1) { { key, value } };

        /// <summary> Returns a <see cref="Dictionary{T,T}"/> containing only this <paramref name="value"/> with the given <paramref name="key"/>. </summary> 
        public static Dictionary<TKey, TValue> IntoDictionaryWithKey<TKey, TValue>(this TValue value, TKey key) =>
             new Dictionary<TKey, TValue>(1) { { key, value } };

        #endregion

        #region Into Enumerable (Yield)

        /// <summary> Returns this <paramref name="value"/> wrapped inside an <see cref="IEnumerable{T}"/>. </summary> 
        public static IEnumerable<T> Yield<T>(this T value) { yield return value; }

        #endregion

        #region FluentValidation Extensions

        ///// <summary> Specifies a custom error message that all validators currently attached to the
        ///// <see cref="IRuleBuilderOptions{T,T}"/> will use if their respective validation fails. </summary>
        //public static IRuleBuilderOptions<T, TProperty> WithMessageForAll<T, TProperty>(this IRuleBuilderOptions<T, TProperty> ruleBuilder,
        //    string messageFormat, params object[] parameters)
        //{
        //    string formattedMessage = string.Format(messageFormat, parameters);
        //    ruleBuilder.Configure(config =>
        //        config.Validators
        //            .Concat(config.DependentRules.SelectMany(rule => rule.Validators))
        //            .ForEach(validator => validator.ErrorMessageSource = new StaticStringSource(formattedMessage)));
        //    return ruleBuilder;
        //}

        //public static IRuleBuilderOptions<T, TProperty> AndIfItPasses<T, TProperty>(this IRuleBuilderOptions<T, TProperty> ruleBuilder,
        //    Action<IRuleBuilderInitial<T, TProperty>> action)
        //{
        //    Expression<Func<T, TProperty>> expression = null;
        //    ruleBuilder.Configure(config => expression = config.Expression as Expression<Func<T, TProperty>>);
        //    ruleBuilder.DependentRules(dependent => action(dependent.RuleFor(expression)));
        //    return ruleBuilder;
        //}

        #endregion

        #region Split Head Tail

        /// <summary> Separates the first element of a sequence from the unenumerated remainder.
        /// <para/> Note: This advances the enumerator by one. Further or concurrent enumeration performed on the original sequence can have unintended consequences. </summary>
        /// <remarks> This method aims to return the head as a singular value. If the sequence might contain zero elements, use the overload with parameter <see langword="1"/> to avoid exceptions. </remarks>
        /// <exception cref="InvalidOperationException">Source contains no elements.</exception>
        public static (T Head, IEnumerable<T> Tail) SplitHeadTail<T>(this IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Sequence contains no elements.");
            return (enumerator.Current, EnumerateTail(enumerator));

            static IEnumerable<T> EnumerateTail(IEnumerator<T> enumerator)
            {
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }
        }

        /// <summary> Separates the first <paramref name="x"/> elements in a sequence from the unenumerated remainder.
        /// <para/> If the sequence contains less elements than <paramref name="x"/>, the length of the head will match the total amount of elements, and the tail will be empty.
        /// <para/> Note: This advances the enumerator up to <paramref name="x"/> times. Further or concurrent enumeration performed on the original sequence can have unintended consequences. </summary>
        public static (T[] Head, IEnumerable<T> Tail) SplitHeadTail<T>(this IEnumerable<T> source, int x)
        {
            var enumerator = source.GetEnumerator();
            return (HeadToArray(enumerator, x), EnumerateTail(enumerator));

            static T[] HeadToArray(IEnumerator<T> enumerator, int size)
            {
                int count = 0;
                static IEnumerable<T> EnumerateUntilSize(IEnumerator<T> enumerator, int count, int size)
                {
                    while (count++ < size && enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }
                return EnumerateUntilSize(enumerator, count, size).ToArray();
            }
            static IEnumerable<T> EnumerateTail(IEnumerator<T> enumerator)
            {
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }
        }

        #endregion

        #region With Index

        /// <summary> Projects each element of a sequence into a <see cref="KeyValuePair{,}"/> where <i>key</i> is the position of the element in the sequence (index) and <i>value</i> is the element. </summary>
        public static IEnumerable<KeyValuePair<int, T>> WithIndex<T>(this IEnumerable<T> source) =>
             source.Select((x, index) => KeyValuePair.Create(index, x));

        #endregion

        #region Random Element

        /// <summary> Returns a cryptographically strong random element from the array. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="array"/> cannot be null. </exception>
        /// <exception cref="ArgumentException"> Parameter <paramref name="array"/> cannot be an empty array. </exception>
        public static T Random<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException($"Parameter '{nameof(array)}' cannot be null.");
            if (array.Length == 0)
                throw new ArgumentException($"Parameter '{nameof(array)}' cannot be an empty array.");
            return array[RandomNumberGenerator.GetInt32(array.Length)];
        }

        /// <summary> Returns a cryptographically strong random element from the array, or a default value if the array is empty. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="array"/> cannot be null. </exception>
        public static T RandomOrDefault<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException($"Parameter '{nameof(array)}' cannot be null.");
            if (array.Length == 0)
                return default;
            return array[RandomNumberGenerator.GetInt32(array.Length)];
        }

        #endregion

        #region Random Alphanumeric Characters

        /// <summary> <c>abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789</c> </summary>
        private static readonly char[] ALPHANUMERIC_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        /// <summary> Generates a <see cref="string"/> out of this many random alphanumeric characters.
        /// <para/> Returns an empty string if <paramref name="value"/> is <see langword="0"/>. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Parameter <paramref name="value"/> may not be a negative number. </exception>
        public static string AlphanumericCharacters(this int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Parameter '{nameof(value)}' may not be a negative number.");
            if (value == 0)
                return string.Empty;

            return new StringBuilder(value)
                .With(x => value.InvokeThisManyTimes(() => x.Append(ALPHANUMERIC_CHARS.Random())))
                .ToString();
            //var builder = new StringBuilder(value);
            //value.InvokeThisManyTimes(() => builder.Append(ALPHANUMERIC_CHARS.Random()));
            //return builder.ToString();
        }

        #endregion

        #region Format

        /// <summary> Replaces one or more format items in the <see cref="string"/> with the <see cref="string"/> representations of corresponding objects in an array. </summary>
        public static string Format(this string value, params object[] arguments) =>
             string.Format(value, arguments);

        /// <summary> Replaces one or more format items in the <see cref="string"/> with the <see cref="string"/> representations of corresponding objects in an array. A parameter supplies culture-specific formatting information. </summary>
        public static string Format(this string value, IFormatProvider formatProvider, params object[] arguments) =>
             string.Format(formatProvider, value, arguments);

        #endregion

        #region Invoke Multiple Times

        /// <summary> Invokes a specified <paramref name="action"/> this many times. </summary>aram>
        public static void InvokeThisManyTimes(this int x, Action action) =>
             x.Range().Each(_ => action());

        /// <summary> Invokes this action a specified number of times. </summary>
        public static void InvokeMultipleTimes(this Action action, int amount) =>
             amount.InvokeThisManyTimes(action);

        #endregion

        #region Factory

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="Activator.CreateInstance(Type)"/>. </summary>
        public static object Activate(this Type type) =>
             Activator.CreateInstance(type);

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="Activator.CreateInstance(Type)"/> and casts it to type <typeparamref name="T"/>. </summary>
        /// <typeparam name="T"> The type to cast to. </typeparam>
        /// <exception cref="InvalidCastException"> Specified cast is not valid. s</exception>
        public static T Activate<T>(this Type type) =>
             type.Activate().CastTo<T>();

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="Expression.New(Type)"/>. </summary>
        public static object Express(this Type type) =>
             Expression.Lambda<Func<object>>(Expression.MemberInit(Expression.New(type))).Compile().Invoke();

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="Expression.New(Type)"/> and casts it to type <typeparamref name="T"/>. </summary>
        /// <typeparam name="T"> The type to cast to. </typeparam>
        /// <exception cref="InvalidCastException"> Specified cast is not valid. s</exception>
        public static T Express<T>(this Type type) =>
             type.Express().CastTo<T>();

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="FormatterServices.GetUninitializedObject(Type)"/>. </summary>
        public static object Uninitialize(this Type type) =>
             FormatterServices.GetUninitializedObject(type);

        /// <summary> Returns a new instance of this <paramref name="type"/> using <see cref="FormatterServices.GetUninitializedObject(Type)"/> and casts it to type <typeparamref name="T"/>. </summary>
        /// <typeparam name="T"> The type to cast to. </typeparam>
        /// <exception cref="InvalidCastException"> Specified cast is not valid. s</exception>
        public static T Uninitialize<T>(this Type type) =>
             type.Uninitialize().CastTo<T>();

        #endregion

        #region Symmetric Except

        /// <summary> Returns the symmetric difference (disjunctive union) of two sequences. </summary>
        public static HashSet<T> SymmetricExcept<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            var hashSet = source is HashSet<T> self ? self : source.ToHashSet();
            hashSet.SymmetricExceptWith(other);
            return hashSet;
        }

        #endregion

        #region Preserve Stack Trace

        private const string PRESERVE_STACK_TRACE_METHOD_NAME = "InternalPreserveStackTrace";
        private const BindingFlags PRESERVE_STACK_TRACE_BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly MethodInfo PRESERVE_STACK_TRACE = typeof(Exception).GetMethod(PRESERVE_STACK_TRACE_METHOD_NAME, PRESERVE_STACK_TRACE_BINDING_FLAGS);

        /// <summary> Causes this exception to not reset its stack trace when rethrown. </summary>
        /// <remarks> Note that even though the stack trace will not be reset, it will not be identical to what it was originally,
        /// because a line is added to the stack trace for the line the exception was rethrown on. </remarks>
        public static void PreserveStackTrace(this Exception exception) =>
             PRESERVE_STACK_TRACE.Invoke(exception, null);

        #endregion

        #region Rethrow

        public static void Rethrow(this Exception exception)
        {
            exception.PreserveStackTrace();
            throw exception;
        }

        #endregion

        #region Arbitrary Throw

        /// <summary> Throws an exception. Can be added arbitrarily to anything non-void in a method chain. </summary>
        public static void Throw<T>(this T _) => throw new Exception();

        #endregion

        #region Arbitrary Return

        /// <summary> Returns a specified value. Can be added arbitrarily to anything non-void in a method chain. </summary>
        /// <remarks> You should never use this. </remarks>
        /// <param name="value"> The value to return. </param>
        public static TReturn Return<TArbitrary, TReturn>(this TArbitrary _, TReturn value) => value;

        #endregion

        #region EmptyEnumerable

        public static IEnumerable<T> EmptyEnumerable<T>(this T _)
        {
            yield break;
        }

        #endregion

        #region Is Even / Odd

        /// <summary> Determines whether this <see cref="int"/> value is even. </summary>
        public static bool IsEven(this int value) => value % 2 == 0;

        /// <summary> Determines whether this <see cref="int"/> value is odd. </summary>
        public static bool IsOdd(this int value) => value % 2 == 1;

        #endregion

        #region New

        // does this even have a use case ever
        /// <summary> Returns a new instance of the same type. </summary>
        public static T New<T>(this T _) where T : new() => new T();

        #endregion

        #region Is Digit / Letter / WhiteSpace

        /// <summary> Indicates whether this Unicode character is categorized as a decimal digit. </summary>
        public static bool IsDigit(this char c) => char.IsDigit(c);

        /// <summary> Indicates whether the character at the specified position in this string is categorized as a decimal digit. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Argument 'index' was out of range of valid values. </exception>
        public static bool IsDigit(this string s, int index) => char.IsDigit(s, index);

        /// <summary> Indicates whether the specified Unicode character is categorized as a Unicode letter. </summary>
        public static bool IsLetter(this char c) => char.IsLetter(c);

        /// <summary> Indicates whether the character at the specified position in a specified string is categorized as a Unicode letter. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Argument 'index' was out of range of valid values. </exception>
        public static bool IsLetter(this string s, int index) => char.IsLetter(s, index);

        /// <summary> Indicates whether the specified Unicode character is categorized as a letter or a decimal digit </summary>
        public static bool IsLetterOrDigit(this char c) => char.IsLetterOrDigit(c);

        /// <summary> Indicates whether the character at the specified position in a specified string is categorized as a letter or a decimal digit. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Argument 'index' was out of range of valid values. </exception>
        public static bool IsLetterOrDigit(this string s, int index) => char.IsLetterOrDigit(s, index);

        /// <summary> Indicates whether the specified Unicode character is categorized as white space. </summary>
        public static bool IsWhiteSpace(this char c) => char.IsWhiteSpace(c);

        /// <summary> Indicates whether the character at the specified position in a specified string is categorized as white space. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Argument 'index' was out of range of valid values. </exception>
        public static bool IsWhiteSpace(this string s, int index) => char.IsWhiteSpace(s, index);

        #endregion

        #region Get Numeric Value

        /// <summary> Converts this Unicode character to a double-precision floating point number. </summary>
        public static double GetNumericValue(this char c) => char.GetNumericValue(c);

        /// <summary> Converts the Unicode character at the specified position in this specified string to a double-precision floating point number. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Argument 'index' was out of range of valid values. </exception>
        public static double GetNumericValue(this string s, int index) => char.GetNumericValue(s, index);

        #endregion

        #region Char To Upper / Lower

        /// <summary> Converts the value of a Unicode character to its uppercase equivalent. </summary>
        public static char ToUpper(this char c) => char.ToUpper(c);

        /// <summary> Converts the value of a specified Unicode character to its uppercase equivalent using specified culture-specific formatting information. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="culture"/> is null. </exception>
        public static char ToUpper(this char c, CultureInfo culture) => char.ToUpper(c, culture);

        /// <summary> Converts the value of a Unicode character to its uppercase equivalent using the casing rules of the invariant culture. </summary>
        public static char ToUpperInvariant(this char c) => char.ToUpperInvariant(c);

        /// <summary> Converts the value of a Unicode character to its lowercase equivalent. </summary>
        public static char ToLower(this char c) => char.ToLower(c);

        /// <summary> Converts the value of a specified Unicode character to its lowercase equivalent using specified culture-specific formatting information. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="culture"/> is null. </exception>
        public static char ToLower(this char c, CultureInfo culture) => char.ToLower(c, culture);

        /// <summary> Converts the value of a Unicode character to its lowercase equivalent using the casing rules of the invariant culture. </summary>
        public static char ToLowerInvariant(this char c) => char.ToLowerInvariant(c);

        #endregion

        #region Pow

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this int x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this long x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this short x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this byte x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this uint x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this ulong x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this ushort x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this sbyte x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this float x, double power) => Math.Pow(x, power);

        /// <summary> Returns this number raised to the specified power. </summary>
        /// <param name="power"> A double-precision floating-point number that specifies a power. </param>
        public static double Pow(this double x, double power) => Math.Pow(x, power);

        #endregion

        #region Delegate Extensions

        /// <summary> Returns the result of invoking this <see cref="Func{}"/>, or returns an alternative value if the <see cref="Func{}"/> is <see langword="null"/>. </summary>
        public static T CoalesceInvoke<T>(this Func<T> function, T alternative = default) =>
             function.IsNull() ? alternative : function();

        #endregion

        #region StringComparison Extensions

        /// <summary> Returns a <see cref="StringComparer"/> that corresponds to this <see cref="StringComparison"/>. </summary>
        public static StringComparer ToComparer(this StringComparison comparison) =>
            comparison switch
            {
                StringComparison.CurrentCulture => StringComparer.CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => StringComparer.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
                StringComparison.Ordinal => StringComparer.Ordinal,
                StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                _ => throw new InvalidOperationException(),
            };

        #endregion

        #region Cast To

        /// <summary> Explicitly casts this object to type <typeparamref name="T"/>. </summary>
        /// <remarks> Value types will be boxed.
        /// It might be possible to do it without boxing: https://stackoverflow.com/questions/1189144/c-sharp-non-boxing-conversion-of-generic-enum-to-int
        /// But it is probably not worth it. </remarks>
        /// <exception cref="InvalidCastException"> Specified cast is not valid. </exception>
        public static T CastTo<T>(this object value) => (T) value;

        /// <summary> Returns an instance of type <typeparamref name="T"/> by casting this value, or <see langword="null"/> if the cast is invalid. </summary>
        public static T As<T>(this object value) where  T : class => 
             value as T;

        #endregion

        #region IOException Extensions

        private const int IO_EXCEPTION_SHARING_VIOLATION = 0x20;
        private const int IO_EXCEPTION_LOCK_VIOLATION = 0x21;
        private const int IO_EXCEPTION_ERROR_CODE_MASK = 0x0000FFFF;

        public static bool IsAccessViolation(this IOException ex)
        {
            var errorCode = ex.HResult & IO_EXCEPTION_ERROR_CODE_MASK;
            return (IO_EXCEPTION_SHARING_VIOLATION == errorCode) || (IO_EXCEPTION_LOCK_VIOLATION == errorCode);
        }

        #endregion

        #region TryGetFirst

        /// <summary> Returns the first element of a sequence, or a default value if the sequence contains no elements. </summary>
        public static bool TryGetFirstOrDefault<T>(this IEnumerable<T> source, out T value) =>
             source.TryGetFirstOrDefault(_ => true, out value);

        /// <summary> Returns the first element of a sequence that satisfies a condition, or a default value if no such element is found. </summary>
        public static bool TryGetFirstOrDefault<T>(this IEnumerable<T> source, Predicate<T> predicate, out T value)
        {
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    value = enumerator.Current;
                    return true;
                }
            }
            value = default;
            return false;
        }

        #endregion

        #region Repeat (non-string)


        /// <summary> Generates a sequence that contains one repeated value. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Parameter <paramref name="amount"/> may not be a negative number. </exception>
        public static IEnumerable<T> Repeat<T>(this T value, int amount) =>
             Enumerable.Repeat(value, amount);
        // is superceded by string.Repeat for strings!

        #endregion

        #region Enum Extensions

        /// <summary> Converts this <see cref="Enum"/> to an int. </summary>
        // unfortunately no way to prevent boxing
        // possibly with https://stackoverflow.com/questions/1189144/c-sharp-non-boxing-conversion-of-generic-enum-to-int
        public static int ToInt<T>(this T value) where T : Enum => (int) (ValueType) value;

        #endregion

        #region String Count

        /// <summary> Returns the number of occurences of a specified non-empty substring in this <see cref="string"/> instance. </summary>
        /// <param name="substring"> The substring to count. May not be <see langword="null"/> or <see cref="string.Empty"/>. </param>
        /// <param name="occurencesOverlap"> Determines whether occurences that overlap with other occurences should be counted separately.
        /// <para/> For example: pattern <c>aba</c> in source <c>ababa</c> counts <b>2</b> occurences when <see langword="true"/>, and <b>1</b> when <see langword="false"/>. </param>
        /// <exception cref="ArgumentException"> Parameter <paramref name="substring"/> may not be null or empty. </exception>
        public static int Count(this string source, string substring, bool occurencesOverlap = false)
        {
            if (source.IsNullOrEmpty())
                return 0;

            if (substring.IsNullOrEmpty())
                throw new ArgumentException($"Parameter '{nameof(substring)}' may not be null or empty.");

            if (occurencesOverlap)
            {
                int count = 0;
                int index = -1;
                while (-1 != (index = source.IndexOf(substring, ++index)) && ++count > 0) ;
                //while (-1 != (index = source.IndexOf(substring, index)))
                //{
                //    index++;
                //    count++;
                //}
                return count;
            }
            else return (source.Length - source.Replace(substring, string.Empty).Length) / substring.Length;
        }

        public static int CountWithIndexOf(this string source, string substring, StringComparison stringComparison = StringComparison.InvariantCulture)
        {
            if (source.IsNullOrEmpty())
                return 0;

            if (substring.IsNullOrEmpty())
                throw new ArgumentException($"Parameter '{nameof(substring)}' may not be null or empty.");

            int count = 0;
            int index = 0;
            while (-1 != (index = source.IndexOf(substring, index, stringComparison)))
            {
                index++;
                count++;
            }
            return count;
        }

        public static int CountWithSplit(this string source, string substring)
        {
            if (source.IsNullOrEmpty())
                return 0;

            if (substring.IsNullOrEmpty())
                throw new ArgumentException($"Parameter '{nameof(substring)}' may not be null or empty.");

            return source.Split(substring).Length - 1;
        }

        public static int CountWithReplace(this string source, string substring)
        {
            if (source.IsNullOrEmpty())
                return 0;

            if (substring.IsNullOrEmpty())
                throw new ArgumentException($"Parameter '{nameof(substring)}' may not be null or empty.");

            return (source.Length - source.Replace(substring, string.Empty).Length) / substring.Length;
        }

        public static int CountWithRegex(this string source, string substring)
        {
            if (source.IsNullOrEmpty())
                return 0;

            if (substring.IsNullOrEmpty())
                throw new ArgumentException($"Parameter '{nameof(substring)}' may not be null or empty.");

            return Regex.Matches(source, substring, RegexOptions.Compiled).Count;
        }

        #endregion

        #region Zero-based Alphabetical Characters

        private const int CHAR_INDEX_a = 97;
        private const int CHAR_INDEX_A = 65;

        /// <summary> Returns the lowercase alphabetical character found by mapping this value to one of 26 alphabetically ordered indexes. </summary>
        public static char AsAlphabeticalCharacter(this int index) =>
             (char) (CHAR_INDEX_a + index % 26);

        /// <summary> Returns the uppercase alphabetical character found by mapping this value to one of 26 alphabetically ordered indexes. </summary>
        public static char AsAlphabeticalCharacterUppercase(this int index) =>
             (char) (CHAR_INDEX_A + index % 26);

        #endregion

        #region Repeated Character String

        /// <summary> Returns a string containing this character repeated a specified number of times. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Parameter <paramref name="amount"/> must be non-negative. </exception>
        public static string RepeatIntoString(this char c, int amount) =>
             new string(c, amount);

        #endregion

        #region Remove First

        /// <summary> Removes the first element from this collection and returns <see langword="true"/>, or <see langword="false"/> if there is nothing to remove. </summary>
        public static bool RemoveFirst<T>(this ICollection<T> collection) =>
             collection.Count.IsGreaterThan(0) && collection.Remove(collection.First());

        #endregion

        #region Change Type

        /// <summary> Calls <see cref="Convert.ChangeType(object, Type)"/> to return this value converted to an instance of type <typeparamref name="T"/>. </summary>
        /// <exception cref="InvalidCastException"> This conversion is not supported. -or- value is null and T is a value type. -or- value does not implement the System.IConvertible interface. </exception>
        /// <exception cref="FormatException"> Specified value is not in a format recognized by T. </exception>
        /// <exception cref="OverflowException"> Specified value represents a number that is out of the range of T. </exception>
        // boxing required because of ChangeType parameter.
        // instead, might be possible to use https://stackoverflow.com/questions/1189144/c-sharp-non-boxing-conversion-of-generic-enum-to-int
        public static T ChangeType<T>(this object value) =>
             (T) Convert.ChangeType(value, typeof(T));

        /// <summary> Returns an object of the specified type and whose value is equivalent to this object. </summary>
        /// <exception cref="InvalidCastException"> This conversion is not supported. -or- value is null and <paramref name="type"/> is a value type. -or- value does not implement the System.IConvertible interface. </exception>
        /// <exception cref="FormatException"> Specified value is not in a format recognized by <paramref name="type"/>. </exception>
        /// <exception cref="OverflowException"> Specified value represents a number that is out of the range of <paramref name="type"/>. </exception>
        /// <exception cref="ArgumentNullException"> Specified <paramref name="type"/> is null. </exception>
        public static object ChangeType(this object value, Type type) =>
             Convert.ChangeType(value, type);

        #endregion

        #region Try Get Method

        /// <summary> Searches for the specified public method whose parameters match the specified argument types, and returns it in an <see langword="out"/> parameter if it was found. </summary>
        public static bool TryGetMethod(this Type type, string methodName, Type[] argumentTypes, out MethodInfo method)
        {
            method = type.GetMethod(methodName, argumentTypes);
            return method.IsNotNull();
        }

        #endregion

        #region Parse Generic

        /// <summary> Converts the string representation of a value of type <typeparamref name="T"/> to an instance of that type. </summary>
        public static T Parse<T>(this string value)
        {
            return (typeof(T)) switch
            {
                var type when type == typeof(string) =>
                     value.ChangeType<T>(),

                var type when type == typeof(Uri) =>
                     new Uri(value).ChangeType<T>(),

                var type when type.TryGetMethod("Parse", typeof(string).IntoArray(), out var parseMethod) =>
                     parseMethod.Invoke(null, value.IntoArray()).ChangeType<T>(),

                _ => throw new NotSupportedException($"No 'Parse' method found for type '{typeof(T).Name}'."),
            };
        }

        /// <summary> Converts the string representation of a value of type <typeparamref name="T"/> to an instance of that type. A return value indicates whether the conversion succeeded. </summary>
        public static bool TryParse<T>(this string value, out T result)
        {
            result = default;
            switch (typeof(T))
            {
                case var type when type == typeof(string):
                    result = value.ChangeType<T>();
                    return true;
                case var type when type == typeof(Uri):
                    result = new Uri(value).ChangeType<T>();
                    return true;
                case var type when type.TryGetMethod("TryParse", new[] { typeof(string), typeof(T).MakeByRefType() }, out var tryParseMethod):
                    var parameters = new object[] { value, null };
                    var couldParse = (bool) tryParseMethod.Invoke(null, parameters);
                    result = (T) parameters[1];
                    return couldParse;
                default:
                    throw new NotSupportedException($"No 'TryParse'method found for type '{typeof(T).Name}'.");
            }
        }

        #endregion

        #region Uptype Action

        /// <summary> Changes the signature of an <see cref="Action"/>&lt;<typeparamref name="TDerived"/>&gt; to <see cref="Action"/>&lt;<typeparamref name="TBase"/>&gt; without changing what happens inside. </summary>
        /// <remarks> Be careful when using this. Passing an instance of a different type than <typeparamref name="TDerived"/> into the uptyped action can lead to an <see cref="InvalidCastException"/>. </remarks>
        public static Action<TBase> Uptype<TDerived, TBase>(this Action<TDerived> action) where  TDerived : TBase => 
                 new Action<TBase>(o => action((TDerived) o));

        #endregion

        #region Flatten

        /// <summary> Flattens a sequence of sequences into one sequence. </summary>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source) =>
             source.SelectMany(x => x);

        #endregion

        #region Duplicates

        /// <summary> Returns duplicate elements in a sequence, determined using default equality comparison. </summary>
        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source) =>
             source.DuplicatesBy(x => x);

        /// <summary> Returns duplicate elements in a sequence, determined using default quality comparison to compare a projected value accessed through a selector. </summary>
        public static IEnumerable<T> DuplicatesBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector) =>
             source.DuplicatesBy(selector, EqualityComparer<TKey>.Default);

        /// <summary> Returns duplicate elements in a sequence, determined using a specified comparer. </summary>
        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer) =>
             source.DuplicatesBy(x => x, comparer);

        /// <summary> Returns duplicate elements in a sequence, determined using a specified comparer to compare a projected value accessed through a selector. </summary>
        public static IEnumerable<T> DuplicatesBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, IEqualityComparer<TKey> comparer) =>
             source
                .GroupBy(selector, comparer)
                .Where(x => x.Take(2).Count() > 1)
                .Flatten();

        #endregion

        #region Increment Capped

        public static void IncrementCapped(ref int value, int cap, int incrementSize = 1)
        {
            checked
            {
                if ((value + incrementSize) < cap)
                    value += incrementSize;
                else
                    value = cap;
            }
        }

        #endregion

        #region Combined ToArray/Select

        /// <summary> Projects each element of a sequence into a new form and creates an array from these values. </summary>
        public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) =>
             source.Select(selector).ToArray();

        #endregion

        #region Multiple Elements

        /// <summary> Determines whether a sequence contains more than one element. </summary>
        public static bool Multiple<T>(this IEnumerable<T> source)
        {
            //return source.GetEnumerator()
            //    .StartChain()
            //    .Into(x => x.MoveNext()
            //        .IfTrue(() => x.MoveNext())
            //        .Value)
            //    .Value;

            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                return false;
            return enumerator.MoveNext();
        }

        #endregion

        #region Not Unique

        /// <summary> Determines whether a sequence contains more than one distinct element. </summary>
        public static bool NotUnique<T>(this IEnumerable<T> source) =>
             source
                .Distinct()
                .Multiple();

        /// <summary> Determines whether a sequence contains more than one distinct element with distinctiveness based on comparing a property accessed through a selector. </summary>
        public static bool NotUniqueBy<TSource, TProperty>(this IEnumerable<TSource> source, Func<TSource, TProperty> selector) =>
             source
                .Select(selector)
                .NotUnique();

        #endregion

        #region As Func

        /// <summary> Encapsulates this <paramref name="value"/> inside a <see cref="Func{}"/> which returns it without side effects. </summary>
        public static Func<T> AsFunc<T>(this T value) => () => value;

        /// <summary> Encapsulates this <paramref name="value"/> inside a <see cref="Func{}"/> which returns it without side effects. </summary>
        public static Func<T> Funcify<T>(this T value) => () => value;

        #endregion

        #region Extract Property Handlers

        /// <summary> Extracts from an <see cref="Expression{}"/> pointing to a property the property name, a <see cref="Func{,}"/> for getting its value and an <see cref="Action{,}"/> for setting the value.
        /// <para/> The expression should look like <c>x =&gt; x.Property</c>. </summary>
        /// <param name="propertyExpression"> The expression that should look like <c>x =&gt; x.Property</c>. </param>
        /// <exception cref="InvalidOperationException"> Member in expression is not a property. </exception>
        /// <exception cref="MissingMethodException"> Property does not have a setter. </exception>
        public static (string PropertyName, Func<T, TProperty> PropertyGetter, Action<T, TProperty> PropertySetter)
            ExtractPropertyHandlers<T, TProperty>(this Expression<Func<T, TProperty>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            var property = memberExpression?.Member as PropertyInfo ?? throw new InvalidOperationException("Member in expression is not a property.");
            var propertyName = property.Name;
            var propertyGetter = propertyExpression.Compile();
            var propertySetMethod = property.GetSetMethod() ?? throw new MissingMethodException("Property does not have a setter.");
            var propertySetter = (Action<T, TProperty>) Delegate.CreateDelegate(typeof(Action<T, TProperty>), propertySetMethod);
            return (propertyName, propertyGetter, propertySetter);
        }

        /// <summary> Extracts from an <see cref="Expression{}"/> pointing to a property the property name, a <see cref="Func{,}"/> for getting its value and an <see cref="Action{,}"/> for setting the value.
        /// <para/> The expression should look like <c>x =&gt; x.Property</c>. </summary>
        /// <param name="propertyExpression"> The expression that should look like <c>x =&gt; x.Property</c>. </param>
        /// <exception cref="InvalidOperationException"> Member in expression is not a property. </exception>
        /// <exception cref="MissingMethodException"> Property does not have a setter. </exception>
        public static (string PropertyName, Func<T, TProperty> PropertyGetter, Action<T, TProperty> PropertySetter)
            ExtractPropertyHandlers<T, TProperty>(this T _, Expression<Func<T, TProperty>> expression) =>
             expression.ExtractPropertyHandlers();

        #endregion

        #region Concat

        ///// <summary> Creates a sequence out of these values. </summary>
        //public static IEnumerable<T> YieldWith<T>(this T left, T right)
        //{
        //    yield return left;
        //    yield return right;
        //}

        /// <summary> Concatenates this sequence and a specified value. </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> left, T right) =>
             left.Concat(right.Yield());

        /// <summary> Creates a sequence out of the concatenation of this value and a specified sequence. </summary>
        public static IEnumerable<T> YieldWith<T>(this T left, IEnumerable<T> right) =>
             left.Yield().Concat(right);

        /// <summary> Creates a sequence out of these values. </summary>
        public static IEnumerable<T> YieldWith<T>(this T left, params T[] right) =>
             left.Yield().Concat(right);

        #endregion

        #region Enumerator to Enumerable

        /// <summary> Lazily constructs a sequence by advancing the enumerator until it has no elements left. </summary>
        public static IEnumerable Enumerate(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary> Lazily constructs a sequence by advancing the enumerator until it has no elements left.
        /// <para/> Elements are cast to type <typeparamref name="T"/> before being yielded. </summary>
        /// <exception cref="InvalidCastException"> Objects inside the enumerator cannot be cast to type <typeparamref name="T"/>. </exception>
        public static IEnumerable<T> Enumerate<T>(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return (T) enumerator.Current;
        }

        /// <summary> Lazily constructs a sequence by advancing the enumerator until it has no elements left. </summary>
        public static IEnumerable<T> Enumerate<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        #endregion

        #region Reverse String

        /// <summary> Returns a string that is equal to this <paramref name="value"/> in inverted order. </summary>
        public static string Reverse(this string value) => value.EnumerateGraphemeClusters().Reverse().Join("");

        /// <summary> Enumerates the grapheme clusters contained within this <paramref name="value"/>. </summary>
        private static IEnumerable<string> EnumerateGraphemeClusters(this string value) =>
             StringInfo.GetTextElementEnumerator(value).Enumerate<string>();

        #endregion

        #region TryMoveNext

        /// <summary> Advances the enumerator to the next element of the collection and returns it in an <see langword="out"/> parameter if it succeeded. </summary>
        public static bool TryMoveNext<T>(this IEnumerator<T> enumerator, out T current)
        {
            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }
            else
            {
                current = default;
                return false;
            }
        }

        #endregion

        #region With Argument

        /// <summary> Returns an action which encapsulates the specified action with its parameter prefilled. </summary>
        /// <param name="action"> An action that will be invoked with the specified argument. </param>
        /// <param name="argument"> The argument that is passed to the action when it is invoked. </param>
        public static Action WithArgument<T>(this Action<T> action, T argument) => () => action(argument);

        /// <summary> Returns an action which encapsulates the specified action with its parameter prefilled. </summary>
        /// <param name="action"> An action that will be invoked with an argument returned by <paramref name="argumentGetter"/>. </param>
        /// <param name="argumentGetter"> A function that returns the argument that is passed to <paramref name="action"/> when it is invoked. </param>
        public static Action WithArgument<T>(this Action<T> action, Func<T> argumentGetter) => () => action(argumentGetter());

        /// <summary> Returns an action which encapsulates the specified action with its parameters prefilled. </summary>
        public static Action WithArguments<T1, T2>(this Action<T1, T2> action, T1 argument1, T2 argument2) => () => action(argument1, argument2);

        /// <summary> Returns an action which encapsulates the specified action with its parameters prefilled. </summary>
        public static Action WithArguments<T1, T2, T3>(this Action<T1, T2, T3> action, T1 argument1, T2 argument2, T3 argument3) => () => action(argument1, argument2, argument3);

        /// <summary> Returns an action which encapsulates the specified action with its parameters prefilled. </summary>
        public static Action WithArguments<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T1 argument1, T2 argument2, T3 argument3, T4 argument4) => () => action(argument1, argument2, argument3, argument4);

        /// <summary> Returns an action which encapsulates the specified action with its parameters prefilled. </summary>
        public static Action WithArguments<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> action, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5) => () => action(argument1, argument2, argument3, argument4, argument5);

        /// <summary> Returns an action which encapsulates the specified action with its last parameter prefilled. </summary>
        public static Action<T1> WithLastArgument<T1, T2>(this Action<T1, T2> action, T2 argument) => p1 => action(p1, argument);

        /// <summary> Returns an action which encapsulates the specified action with its last parameter prefilled. </summary>
        public static Action<T1, T2> WithLastArgument<T1, T2, T3>(this Action<T1, T2, T3> action, T3 argument) => (p1, p2) => action(p1, p2, argument);

        /// <summary> Returns an action which encapsulates the specified action with its last parameter prefilled. </summary>
        public static Action<T1, T2, T3> WithLastArgument<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T4 argument) => (p1, p2, p3) => action(p1, p2, p3, argument);

        /// <summary> Returns an action which encapsulates the specified action with its last parameter prefilled. </summary>
        public static Action<T1, T2, T3, T4> WithLastArgument<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> action, T5 argument) => (p1, p2, p3, p4) => action(p1, p2, p3, p4, argument);

        #endregion

        #region Jagged Array <-> 2d Array

        /// <summary> Creates a jagged array from a two-dimensional array. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="source"/> is null. </exception>
        public static T[][] ToJaggedArray<T>(this T[,] source) =>
             source
                .Cast<T>()
                .WithIndex()
                .GroupBy(kvp => kvp.Key / source.GetLength(1))
                .Select(group => group.ToArray(kvp => kvp.Value))
                .ToArray();

        /// <summary> Creates a two-dimensional array from a rectangular jagged array. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="source"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Jagged array is not a rectangle. </exception>
        public static T[,] ToTwoDimensionalArray<T>(this T[][] source)
        {
            if (source.None())
                return new T[0, 0];

            var firstDimension = source.Length;
            var secondDimension = source[0].Length;
            if (source.Any(x => x.Length != secondDimension))
                throw new ArgumentOutOfRangeException("Jagged array is not a rectangle.");

            T[,] result = new T[firstDimension, secondDimension];
            source
                .WithIndex()
                .Each(x => x.Value
                    .WithIndex()
                    .Each(y => result[x.Key, y.Key] = y.Value));
            return result;
        }

        #endregion

        #region Try Catch Invoke / Invoke Except

        // this one is kinda confusing, because the true means an exception is caught. also no uses atm. maybe remove it.
        /// <summary> Invokes an action. A return value indicates whether an exception was caught during invocation, which will be returned in an <see langword="out"/> parameter when <see langword="true"/>. </summary>
        /// <param name="action"> The action to invoke. </param>
        /// <param name="exception"> The exception caught while invoking the <paramref name="action"/>. Will contain a <see langword="default"/> value when no exception was thrown. </param>
        public static bool TryCatchInvoke(this Action action, out Exception exception)
        {
            try
            {
                action();
                exception = default;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary> Invokes an action. If an exception is thrown during invocation, it will be caught and passed to the invocation of an exception-handling action. </summary>
        /// <param name="action"> The action to invoke. </param>
        /// <param name="exceptionHandler"> The action to invoke when an exception is caught during invocation of the preceding <paramref name="action"/>. <br/> Is not invoked if no exception is caught. </param>
        public static void InvokeExcept(this Action action, Action<Exception> exceptionHandler)
        {
            //action.TryCatchInvoke(out var exception).IfTrue(() => exceptionHandler(exception));
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exceptionHandler(ex);
            }
        }

        /// <summary> Invokes an action. If an exception of type <typeparamref name="TException"/> is thrown during invocation, it will be caught and passed to the invocation of an exception-handling action.
        /// <br/> If an exception of a different type is thrown, it is not caught and will bubble up. </summary>
        /// <param name="action"> The action to invoke. </param>
        /// <param name="exceptionHandler"> The action to invoke when an exception is caught during invocation of the preceding <paramref name="action"/>. <br/> Is not invoked if no exception is caught. </param>
        public static void InvokeExcept<TException>(this Action action, Action<TException> exceptionHandler)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                exceptionHandler(ex);
            }
        }

        #endregion

        #region Try Catch

        public static bool TryCatch<T>(this T value, Action<T> action, out Exception exception)
        {
            try
            {
                action(value);
                exception = default;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        public static bool TryCatch<T, TResult>(this T value, Func<T, TResult> func, out TResult result, out Exception exception)
        {
            try
            {
                result = func(value);
                exception = default;
                return true;
            }
            catch (Exception ex)
            {
                result = default;
                exception = ex;
                return false;
            }
        }

        #endregion

        #region Pass Into

        /// <summary> Passes this value into the invocation of a <see cref="Func{,}"/> and returns the result. </summary>
        [DebuggerStepThrough]
        public static TResult Into<TValue, TResult>(this TValue value, Func<TValue, TResult> func) =>
             func(value);

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/>. </summary>
        public static void Into<T>(this T value, Action<T> action) =>
             action(value);

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with an additional specified argument. </summary>
        public static T Into<T, TArg1>(this T value, Action<T, TArg1> action, TArg1 arg1) =>
             value.With(x => action(x, arg1));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2>(this T value, Action<T, TArg1, TArg2> action, TArg1 arg1, TArg2 arg2) =>
             value.With(x => action(value, arg1, arg2));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3>(this T value, Action<T, TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3) =>
             value.With(x => action(value, arg1, arg2, arg3));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3, TArg4>(this T value, Action<T, TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) =>
             value.With(x => action(value, arg1, arg2, arg3, arg4));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3, TArg4, TArg5>(this T value, Action<T, TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) =>
             value.With(x => action(value, arg1, arg2, arg3, arg4, arg5));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this T value, Action<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) =>
             value.With(x => action(value, arg1, arg2, arg3, arg4, arg5, arg6));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this T value, Action<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) =>
             value.With(x => action(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        /// <summary> Passes this value into the invocation of an <see cref="Action{}"/> with additional specified arguments. </summary>
        public static T Into<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this T value, Action<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) =>
             value.With(x => action(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

        #endregion

        #region Flatten Generic Type Hierarchy

        public static IEnumerable<Type> FlattenGenericArguments(this Type type)
        {
            yield return type;
            foreach (var genericType in type.GetGenericArguments().SelectMany(FlattenGenericArguments))
                yield return genericType;
        }

        #endregion

        #region To Queue / Stack

        /// <summary> Creates a <see cref="Queue{}"/> from an <see cref="IEnumerable{}"/> </summary>
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) =>
            new Queue<T>(source);

        /// <summary> Creates a <see cref="Stack{}"/> from an <see cref="IEnumerable{}"/> </summary>
        public static Stack<T> ToStack<T>(this IEnumerable<T> source) =>
            new Stack<T>(source);

        #endregion

        #region Except

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using default equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except) =>
             source.Where(x => !x.Equals(except));

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using specified equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except, IEqualityComparer<T> equalityComparer) =>
             source.Where(x => !equalityComparer.Equals(x, except));

        #endregion

        #region Characters Into String

        /// <summary> Converts an array of Unicode characters into a <see cref="string"/>. </summary>
        public static string IntoString(this char[] characters) =>
             new string(characters);

        /// <summary> Converts a sequence of Unicode characters into a <see cref="string"/>. </summary>
        public static string IntoString(this IEnumerable<char> characters) =>
             characters
                .ToArray()
                .IntoString();

        #endregion

        #region Without Whitespace

        /// <summary> Returns a copy of this <see cref="string"/> with all white-space characters removed. </summary>
        public static string WithoutWhitespace(this string value) =>
             value
                .Where(c => !c.IsWhiteSpace())
                .IntoString();

        #endregion

        #region Traverse

        /// <summary> Returns a sequence of values obtained by traversing a hierarchy for as long as a predicate is satisfied.
        /// <para/> The sequence includes the root element.
        /// <para/> Example: <c>exception.Traverse(ex => ex.InnerException)</c> </summary>
        public static IEnumerable<T> Traverse<T>(this T value, Func<T, T> traverse, Predicate<T> predicate)
        {
            for (var current = value; predicate(current); current = traverse(current))
                yield return current;
        }

        /// <summary> Returns a sequence of values obtained by traversing a hierarchy until it gets to <see langword="null"/>.
        /// <para/> The sequence includes the root element.
        /// <para/> Example: <c>exception.Traverse(ex => ex.InnerException)</c> </summary>
        public static IEnumerable<T> Traverse<T>(this T value, Func<T, T> traverse) where  T : class => 
             value.Traverse(traverse, IsNotNull);

        #endregion

        #region Extract Inner Messages

        /// <summary> Joins the messages of this <see cref="Exception"/> and all inner exceptions to one single string, separated by a specified value. </summary>
        /// <param name="separator"> The value that separates each message. </param>
        public static string JoinMessageWithInnerMessages(this Exception ex, string separator) =>
            ex.Traverse(x => x.InnerException).Select(x => x.Message).Join(separator);

        /// <summary> Joins the messages of this <see cref="Exception"/> and all inner exceptions to one single string, separated by <see cref="Environment.NewLine"/>. </summary>
        public static string JoinMessageWithInnerMessages(this Exception ex) =>
            ex.JoinMessageWithInnerMessages(Environment.NewLine);

        #endregion

        #region Negate Predicate

        /// <summary> Returns a predicate that is equal to the negation of the specified predicate. </summary>
        public static Func<T, bool> Negate<T>(this Func<T, bool> predicate) =>
             x => !predicate(x);

        /// <summary> Returns a predicate that is equal to the negation of the specified predicate. </summary>
        public static Predicate<T> Negate<T>(this Predicate<T> predicate) =>
             x => !predicate(x);

        #endregion

        #region Any But Not All

        /// <summary> Determines whether at least one element in a sequence satisfies a condition, and at least one does not. </summary>
        public static bool AnyButNotAll<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            bool any = false;
            bool negated = false;
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                    any = true;
                else
                    negated = true;
                if (any && negated)
                    return true;
            }
            return false;
        }

        #endregion

        #region Create And Set

        public static T CreateAndSet<T, TProp>(params (Action<T, TProp> Setter, TProp Value)[] setters)
            where T : new() =>
             new T().With(instance => setters.Each(x => x.Setter(instance, x.Value)));

        public static TInstance CreateAndSet<TInstance, T1, T2>((Action<TInstance, T1> Setter, T1 Value) setter1,
            (Action<TInstance, T1> Setter, T1 Value) setter2)
            where TInstance : new() =>
             new TInstance().With(instance => instance
                        .Into(setter1.Setter, setter1.Value)
                        .Into(setter2.Setter, setter2.Value));

        public static TInstance CreateAndSet<TInstance, T1, T2, T3>((Action<TInstance, T1> Setter, T1 Value) setter1,
            (Action<TInstance, T1> Setter, T1 Value) setter2, (Action<TInstance, T3> Setter, T3 Value) setter3)
            where TInstance : new() =>
             new TInstance().With(instance => instance
                        .Into(setter1.Setter, setter1.Value)
                        .Into(setter2.Setter, setter2.Value)
                        .Into(setter3.Setter, setter3.Value));

        public static TInstance CreateAndSet<TInstance, T1, T2, T3, T4>((Action<TInstance, T1> Setter, T1 Value) setter1,
            (Action<TInstance, T2> Setter, T2 Value) setter2, (Action<TInstance, T3> Setter, T3 Value) setter3,
            (Action<TInstance, T4> Setter, T4 Value) setter4)
            where TInstance : new() =>
             new TInstance().With(instance => instance
                        .Into(setter1.Setter, setter1.Value)
                        .Into(setter2.Setter, setter2.Value)
                        .Into(setter3.Setter, setter3.Value)
                        .Into(setter4.Setter, setter4.Value));

        public static T CreateAndSet<T>(params (Action<T, object> Setter, object Value)[] setters)
            where T : new() =>
             new T().With(instance => setters.Each(x => instance.Into(x.Setter, x.Value)));

        #endregion

        #region Get Hidden Property Value

        /// <summary> Returns the value of a hidden property with a specified name. </summary>
        /// <exception cref="AmbiguousMatchException"> More than one property is found with the specified name. </exception>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="propertyName"/> is null. </exception>
        /// <exception cref="NullReferenceException"> No property exists on the type with specified <paramref name="propertyName"/>. </exception>
        public static object GetHiddenPropertyValue<T>(this T instance, string propertyName) =>
            typeof(T)
                .GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(instance);

        // reason i dont want to make a version that auto-casts is then you need to also specify T

        #endregion

        #region Capitalize / Uncapitalize

        /// <summary> Returns a copy of this <see cref="string"/> with its first character in upper case. </summary>
        public static string Capitalize(this string value)
        {
            if (value.IsNullOrEmpty())
                return value;
            if (value.Length == 1)
                return value.ToUpperInvariant();
            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }

        /// <summary> Returns a copy of this <see cref="string"/> with its first character in lower case. </summary>
        public static string Uncapitalize(this string value)
        {
            if (value.IsNullOrEmpty())
                return value;
            if (value.Length == 1)
                return value.ToLowerInvariant();
            return value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
        }

        public static string CapitalizeWithSplitHeadTail(this string value)
        {
            if (value.IsNullOrEmpty())
                return value;
            if (value.Length == 1)
                return value.ToUpperInvariant();
            return value
                .SplitHeadTail()
                .Into(x => x.Head.ToUpperInvariant().YieldWith(x.Tail).IntoString());
        }

        public static string UncapitalizeWithSplitHeadTail(this string value)
        {
            if (value.IsNullOrEmpty())
                return value;
            if (value.Length == 1)
                return value.ToLowerInvariant();
            return value
                .SplitHeadTail()
                .Into(x => x.Head.ToLowerInvariant().YieldWith(x.Tail).IntoString());
        }

        #endregion

        #region Reverse IList

        // its slower than regular reverse...
        public static IEnumerable<T> Reverse<T>(this IList<T> source)
        {
            for (int i = source.Count - 1; i >= 0; i--)
                yield return source[i];
        }

        #endregion

        #region Access

        /// <summary> Returns this value.
        /// <para/> Can be used to circumvent <i>Error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement</i>. </summary>
        /// <remarks> If this is actually needed it probably means reliance on side-effects. Use with caution. </remarks>
        public static T Access<T>(this T value) => value;

        #endregion

        #region Is Numeric

        ///// <summary> Determines whether this value is one of the following types:
        ///// <br/> <see cref="int"/>, <see cref="long"/>, <see cref="short"/>, <see cref="byte"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="ushort"/>, <see cref="sbyte"/>,
        ///// <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/></summary>
        //public static bool IsNumeric<T>(this T _)
        //    => typeof(T).IsNumeric();

        /// <summary> Determines whether this <see cref="Type"/> is one of the following:
        /// <br/> <see cref="int"/>, <see cref="long"/>, <see cref="short"/>, <see cref="byte"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="ushort"/>, <see cref="sbyte"/>,
        /// <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/></summary>
        public static bool IsNumeric(this Type type) =>
             type.In(typeof(int), typeof(long), typeof(short), typeof(byte),
                typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
                typeof(float), typeof(double), typeof(decimal));

        /// <summary> Determines whether this <see cref="Type"/> is <see cref="string"/>. </summary>
        public static bool IsString(this Type type) =>
             type == typeof(string);

        #endregion

        #region Increment Properties

        /// <summary> Sets all numeric and string properties of this instance to a value equivalent to an integer that is incremented for each property. </summary>
        /// <param name="from"> The starting value for the incrementing integer. </param>
        /// <param name="bindingFlags"> Flags to specify which properties to set. If not provided, will default to <c>Public | Instance | FlattenHierarchy</c>. </param>
        public static T SetNumericAndStringProperties<T>(this T instance, int from = 0, BindingFlags? bindingFlags = null)
        {
            static void SetValue(T instance, PropertyInfo property, int value)
            {
                if (property.PropertyType.IsNumeric())
                    property.SetValue(instance, value.ChangeType(property.PropertyType));
                else if (property.PropertyType.IsString())
                    property.SetValue(instance, value.ToString());
                else
                    throw new NotSupportedException($"Type '{property.PropertyType.Name}' is not supported.");
            }
            typeof(T)
                .GetProperties(bindingFlags ?? BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(prop => prop.SetMethod.IsNotNull())
                .Where(prop => prop.PropertyType.Into(t => t.IsNumeric() || t.IsString()))
                .OrderBy(prop => prop.Name)
                .Each(prop => SetValue(instance, prop, from++));
            return instance;
        }

        #endregion

        #region This Ref

        /// <summary> Changes this reference to a struct by the result of a func that takes its value as argument. </summary>
        public static void ReplaceWith<T>(this ref T value, Func<T, T> effect) where  T : struct => 
             value = effect(value);

        public static void ReplaceWith<T>(this ref T? value, Func<T?, T?> effect) where  T : struct => 
             value = effect(value);

        #endregion

        #region Default To

        /// <summary> Replaces the value referenced by this variable to a provided alternative if the original is <see langword="null"/>. </summary>
        public static void DefaultTo<T>(this ref T? original, T alternative) where T : struct
        {
            if (!original.HasValue)
                original = alternative;
        }

        #endregion

        #region As Nullable

        /// <summary> Returns this value wrapped inside a <see cref="Nullable{}"/>. </summary>
        public static T? AsNullable<T>(this T value) where T : struct => value;

        #endregion

        #region Any In

        public static bool AnyIn<TSource, TProperty>(this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector, IEnumerable<TProperty> properties) =>
            source
                .Select(selector)
                .Any(x => x.In(properties));

        #endregion

        #region Get Property Backing Field

        /// <summary> Returns a <see cref="FieldInfo"/> representing the field that backs a property with a given name. </summary>
        public static FieldInfo GetPropertyBackingField(this Type type, string propertyName) =>
            type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

        #region Pairs

        /// <summary>
        /// Returns inclusive pairs of 2 values each. 
        /// <para/> Differs from (exclusive) <c>Pairs</c> because here elements can occur in two pairs: once as key and once as value.
        /// <para/> Example:
        /// <br/> <c>Source: 1 2 3 4 5</c>
        /// <br/> <c>Pairs: {1,2} {3,4} {5,null}</c>
        /// <br/> <c>PairsInclusive: {1,2} {2,3} {3,4} {4,5}</c>
        /// <para/> If there is only 1 element, the returned key-value pair has default value for Value.
        /// </summary>
        public static IEnumerable<KeyValuePair<T, T>> PairsInclusive<T>(this IEnumerable<T> source)
        {
            T current = default;
            bool loopedFirst = false;
            bool loopedMoreThanOnce = false;
            foreach (var next in source)
            {
                if (false == loopedFirst)
                {
                    loopedFirst = true;
                }
                else
                {
                    loopedMoreThanOnce = true;
                    yield return KeyValuePair.Create(current, next);
                }
                current = next;
            }
            if (loopedFirst && (false == loopedMoreThanOnce))
            {
                yield return KeyValuePair.Create(current, default(T));
            }
        }

        #endregion
    }
}