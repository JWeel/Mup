/*
Microsoft Public License (Ms-PL)
FNA - Copyright 2009-2020 Ethan Lee and the MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software,
you accept this license. If you do not accept the license, do not use the
software.

1. Definitions

The terms "reproduce," "reproduction," "derivative works," and "distribution"
have the same meaning here as under U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the
software.

A "contributor" is any person that distributes its contribution under this
license.

"Licensed patents" are a contributor's patent claims that read directly on its
contribution.

2. Grant of Rights

(A) Copyright Grant- Subject to the terms of this license, including the
license conditions and limitations in section 3, each contributor grants you a
non-exclusive, worldwide, royalty-free copyright license to reproduce its
contribution, prepare derivative works of its contribution, and distribute its
contribution or any derivative works that you create.

(B) Patent Grant- Subject to the terms of this license, including the license
conditions and limitations in section 3, each contributor grants you a
non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of
its contribution in the software or derivative works of the contribution in the
software.

3. Conditions and Limitations

(A) No Trademark License- This license does not grant you rights to use any
contributors' name, logo, or trademarks.

(B) If you bring a patent claim against any contributor over patents that you
claim are infringed by the software, your patent license from such contributor
to the software ends automatically.

(C) If you distribute any portion of the software, you must retain all
copyright, patent, trademark, and attribution notices that are present in the
software.

(D) If you distribute any portion of the software in source code form, you may
do so only under this license by including a complete copy of this license with
your distribution. If you distribute any portion of the software in compiled or
object code form, you may only do so under a license that complies with this
license.

(E) The software is licensed "as-is." You bear the risk of using it. The
contributors give no express warranties, guarantees or conditions. You may have
additional consumer rights under your local laws which this license cannot
change. To the extent permitted under your local laws, the contributors exclude
the implied warranties of merchantability, fitness for a particular purpose and
non-infringement.
*/

using System;
using System.Diagnostics;

namespace Mup.External
{
	/// <summary>
	/// Describes a 2D-vector.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("{DebugDisplayString,nq}")]
	public struct Vector : IEquatable<Vector>
	{
		#region Public Static Properties

		/// <summary>
		/// Returns a <see cref="Vector"/> with components 0, 0.
		/// </summary>
		public static Vector Zero
		{
			get
			{
				return zeroVector;
			}
		}

		/// <summary>
		/// Returns a <see cref="Vector"/> with components 1, 1.
		/// </summary>
		public static Vector One
		{
			get
			{
				return unitVector;
			}
		}

		/// <summary>
		/// Returns a <see cref="Vector"/> with components 1, 0.
		/// </summary>
		public static Vector UnitX
		{
			get
			{
				return unitXVector;
			}
		}

		/// <summary>
		/// Returns a <see cref="Vector"/> with components 0, 1.
		/// </summary>
		public static Vector UnitY
		{
			get
			{
				return unitYVector;
			}
		}

		#endregion

		#region Internal Properties

		internal string DebugDisplayString
		{
			get
			{
				return string.Concat(
					X.ToString(), " ",
					Y.ToString()
				);
			}
		}

		#endregion

		#region Public Fields

		/// <summary>
		/// The x coordinate of this <see cref="Vector"/>.
		/// </summary>
		public float X;

		/// <summary>
		/// The y coordinate of this <see cref="Vector"/>.
		/// </summary>
		public float Y;

		#endregion

		#region Private Static Fields

		private static readonly Vector zeroVector = new Vector(0f, 0f);
		private static readonly Vector unitVector = new Vector(1f, 1f);
		private static readonly Vector unitXVector = new Vector(1f, 0f);
		private static readonly Vector unitYVector = new Vector(0f, 1f);

		#endregion

		#region Public Constructors

		/// <summary>
		/// Constructs a 2d vector with X and Y from two values.
		/// </summary>
		/// <param name="x">The x coordinate in 2d-space.</param>
		/// <param name="y">The y coordinate in 2d-space.</param>
		public Vector(float x, float y)
		{
			this.X = x;
			this.Y = y;
		}

		/// <summary>
		/// Constructs a 2d vector with X and Y set to the same value.
		/// </summary>
		/// <param name="value">The x and y coordinates in 2d-space.</param>
		public Vector(float value)
		{
			this.X = value;
			this.Y = value;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Compares whether current instance is equal to specified <see cref="Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public override bool Equals(object obj)
		{
			return (obj is Vector) && Equals((Vector) obj);
		}

		/// <summary>
		/// Compares whether current instance is equal to specified <see cref="Vector"/>.
		/// </summary>
		/// <param name="other">The <see cref="Vector"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public bool Equals(Vector other)
		{
			return (	X == other.X &&
					Y == other.Y	);
		}

		/// <summary>
		/// Gets the hash code of this <see cref="Vector"/>.
		/// </summary>
		/// <returns>Hash code of this <see cref="Vector"/>.</returns>
		public override int GetHashCode()
		{
			return X.GetHashCode() + Y.GetHashCode();
		}

		/// <summary>
		/// Returns the length of this <see cref="Vector"/>.
		/// </summary>
		/// <returns>The length of this <see cref="Vector"/>.</returns>
		public float Length()
		{
			return (float) Math.Sqrt((X * X) + (Y * Y));
		}

		/// <summary>
		/// Returns the squared length of this <see cref="Vector"/>.
		/// </summary>
		/// <returns>The squared length of this <see cref="Vector"/>.</returns>
		public float LengthSquared()
		{
			return (X * X) + (Y * Y);
		}

		/// <summary>
		/// Turns this <see cref="Vector"/> to a unit vector with the same direction.
		/// </summary>
		public void Normalize()
		{
			float val = 1.0f / (float) Math.Sqrt((X * X) + (Y * Y));
			X *= val;
			Y *= val;
		}

		/// <summary>
		/// Returns a <see cref="String"/> representation of this <see cref="Vector"/> in the format:
		/// {X:[<see cref="X"/>] Y:[<see cref="Y"/>]}
		/// </summary>
		/// <returns>A <see cref="String"/> representation of this <see cref="Vector"/>.</returns>
		public override string ToString()
		{
			return (
				"{X:" + X.ToString() +
				" Y:" + Y.ToString() +
				"}"
			);
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Performs vector addition on <paramref name="value1"/> and <paramref name="value2"/>.
		/// </summary>
		/// <param name="value1">The first vector to add.</param>
		/// <param name="value2">The second vector to add.</param>
		/// <returns>The result of the vector addition.</returns>
		public static Vector Add(Vector value1, Vector value2)
		{
			value1.X += value2.X;
			value1.Y += value2.Y;
			return value1;
		}

		/// <summary>
		/// Performs vector addition on <paramref name="value1"/> and
		/// <paramref name="value2"/>, storing the result of the
		/// addition in <paramref name="result"/>.
		/// </summary>
		/// <param name="value1">The first vector to add.</param>
		/// <param name="value2">The second vector to add.</param>
		/// <param name="result">The result of the vector addition.</param>
		public static void Add(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X + value2.X;
			result.Y = value1.Y + value2.Y;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 2d-triangle.
		/// </summary>
		/// <param name="value1">The first vector of 2d-triangle.</param>
		/// <param name="value2">The second vector of 2d-triangle.</param>
		/// <param name="value3">The third vector of 2d-triangle.</param>
		/// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 2d-triangle.</param>
		/// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 2d-triangle.</param>
		/// <returns>The cartesian translation of barycentric coordinates.</returns>
		public static Vector Barycentric(
			Vector value1,
			Vector value2,
			Vector value3,
			float amount1,
			float amount2
		) {
			return new Vector(
				MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
				MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2)
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 2d-triangle.
		/// </summary>
		/// <param name="value1">The first vector of 2d-triangle.</param>
		/// <param name="value2">The second vector of 2d-triangle.</param>
		/// <param name="value3">The third vector of 2d-triangle.</param>
		/// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 2d-triangle.</param>
		/// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 2d-triangle.</param>
		/// <param name="result">The cartesian translation of barycentric coordinates as an output parameter.</param>
		public static void Barycentric(
			ref Vector value1,
			ref Vector value2,
			ref Vector value3,
			float amount1,
			float amount2,
			out Vector result
		) {
			result.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
			result.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains CatmullRom interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">The first vector in interpolation.</param>
		/// <param name="value2">The second vector in interpolation.</param>
		/// <param name="value3">The third vector in interpolation.</param>
		/// <param name="value4">The fourth vector in interpolation.</param>
		/// <param name="amount">Weighting factor.</param>
		/// <returns>The result of CatmullRom interpolation.</returns>
		public static Vector CatmullRom(
			Vector value1,
			Vector value2,
			Vector value3,
			Vector value4,
			float amount
		) {
			return new Vector(
				MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
				MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount)
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains CatmullRom interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">The first vector in interpolation.</param>
		/// <param name="value2">The second vector in interpolation.</param>
		/// <param name="value3">The third vector in interpolation.</param>
		/// <param name="value4">The fourth vector in interpolation.</param>
		/// <param name="amount">Weighting factor.</param>
		/// <param name="result">The result of CatmullRom interpolation as an output parameter.</param>
		public static void CatmullRom(
			ref Vector value1,
			ref Vector value2,
			ref Vector value3,
			ref Vector value4,
			float amount,
			out Vector result
		) {
			result.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
			result.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
		}

		/// <summary>
		/// Clamps the specified value within a range.
		/// </summary>
		/// <param name="value1">The value to clamp.</param>
		/// <param name="min">The min value.</param>
		/// <param name="max">The max value.</param>
		/// <returns>The clamped value.</returns>
		public static Vector Clamp(Vector value1, Vector min, Vector max)
		{
			return new Vector(
				MathHelper.Clamp(value1.X, min.X, max.X),
				MathHelper.Clamp(value1.Y, min.Y, max.Y)
			);
		}

		/// <summary>
		/// Clamps the specified value within a range.
		/// </summary>
		/// <param name="value1">The value to clamp.</param>
		/// <param name="min">The min value.</param>
		/// <param name="max">The max value.</param>
		/// <param name="result">The clamped value as an output parameter.</param>
		public static void Clamp(
			ref Vector value1,
			ref Vector min,
			ref Vector max,
			out Vector result
		) {
			result.X = MathHelper.Clamp(value1.X, min.X, max.X);
			result.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
		}

		/// <summary>
		/// Returns the distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The distance between two vectors.</returns>
		public static float Distance(Vector value1, Vector value2)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			return (float) Math.Sqrt((v1 * v1) + (v2 * v2));
		}

		/// <summary>
		/// Returns the distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The distance between two vectors as an output parameter.</param>
		public static void Distance(ref Vector value1, ref Vector value2, out float result)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			result = (float) Math.Sqrt((v1 * v1) + (v2 * v2));
		}

		/// <summary>
		/// Returns the squared distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The squared distance between two vectors.</returns>
		public static float DistanceSquared(Vector value1, Vector value2)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			return (v1 * v1) + (v2 * v2);
		}

		/// <summary>
		/// Returns the squared distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The squared distance between two vectors as an output parameter.</param>
		public static void DistanceSquared(
			ref Vector value1,
			ref Vector value2,
			out float result
		) {
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			result = (v1 * v1) + (v2 * v2);
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by the components of another <see cref="Vector"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Divisor <see cref="Vector"/>.</param>
		/// <returns>The result of dividing the vectors.</returns>
		public static Vector Divide(Vector value1, Vector value2)
		{
			value1.X /= value2.X;
			value1.Y /= value2.Y;
			return value1;
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by the components of another <see cref="Vector"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Divisor <see cref="Vector"/>.</param>
		/// <param name="result">The result of dividing the vectors as an output parameter.</param>
		public static void Divide(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X / value2.X;
			result.Y = value1.Y / value2.Y;
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="divider">Divisor scalar.</param>
		/// <returns>The result of dividing a vector by a scalar.</returns>
		public static Vector Divide(Vector value1, float divider)
		{
			float factor = 1 / divider;
			value1.X *= factor;
			value1.Y *= factor;
			return value1;
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="divider">Divisor scalar.</param>
		/// <param name="result">The result of dividing a vector by a scalar as an output parameter.</param>
		public static void Divide(ref Vector value1, float divider, out Vector result)
		{
			float factor = 1 / divider;
			result.X = value1.X * factor;
			result.Y = value1.Y * factor;
		}

		/// <summary>
		/// Returns a dot product of two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The dot product of two vectors.</returns>
		public static float Dot(Vector value1, Vector value2)
		{
			return (value1.X * value2.X) + (value1.Y * value2.Y);
		}

		/// <summary>
		/// Returns a dot product of two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The dot product of two vectors as an output parameter.</param>
		public static void Dot(ref Vector value1, ref Vector value2, out float result)
		{
			result = (value1.X * value2.X) + (value1.Y * value2.Y);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains hermite spline interpolation.
		/// </summary>
		/// <param name="value1">The first position vector.</param>
		/// <param name="tangent1">The first tangent vector.</param>
		/// <param name="value2">The second position vector.</param>
		/// <param name="tangent2">The second tangent vector.</param>
		/// <param name="amount">Weighting factor.</param>
		/// <returns>The hermite spline interpolation vector.</returns>
		public static Vector Hermite(
			Vector value1,
			Vector tangent1,
			Vector value2,
			Vector tangent2,
			float amount
		) {
			Vector result = new Vector();
			Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
			return result;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains hermite spline interpolation.
		/// </summary>
		/// <param name="value1">The first position vector.</param>
		/// <param name="tangent1">The first tangent vector.</param>
		/// <param name="value2">The second position vector.</param>
		/// <param name="tangent2">The second tangent vector.</param>
		/// <param name="amount">Weighting factor.</param>
		/// <param name="result">The hermite spline interpolation vector as an output parameter.</param>
		public static void Hermite(
			ref Vector value1,
			ref Vector tangent1,
			ref Vector value2,
			ref Vector tangent2,
			float amount,
			out Vector result
		) {
			result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
			result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains linear interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
		/// <returns>The result of linear interpolation of the specified vectors.</returns>
		public static Vector Lerp(Vector value1, Vector value2, float amount)
		{
			return new Vector(
				MathHelper.Lerp(value1.X, value2.X, amount),
				MathHelper.Lerp(value1.Y, value2.Y, amount)
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains linear interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
		/// <param name="result">The result of linear interpolation of the specified vectors as an output parameter.</param>
		public static void Lerp(
			ref Vector value1,
			ref Vector value2,
			float amount,
			out Vector result
		) {
			result.X = MathHelper.Lerp(value1.X, value2.X, amount);
			result.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a maximal values from the two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The <see cref="Vector"/> with maximal values from the two vectors.</returns>
		public static Vector Max(Vector value1, Vector value2)
		{
			return new Vector(
				value1.X > value2.X ? value1.X : value2.X,
				value1.Y > value2.Y ? value1.Y : value2.Y
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a maximal values from the two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The <see cref="Vector"/> with maximal values from the two vectors as an output parameter.</param>
		public static void Max(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X > value2.X ? value1.X : value2.X;
			result.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a minimal values from the two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The <see cref="Vector"/> with minimal values from the two vectors.</returns>
		public static Vector Min(Vector value1, Vector value2)
		{
			return new Vector(
				value1.X < value2.X ? value1.X : value2.X,
				value1.Y < value2.Y ? value1.Y : value2.Y
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a minimal values from the two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The <see cref="Vector"/> with minimal values from the two vectors as an output parameter.</param>
		public static void Min(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X < value2.X ? value1.X : value2.X;
			result.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a multiplication of two vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <returns>The result of the vector multiplication.</returns>
		public static Vector Multiply(Vector value1, Vector value2)
		{
			value1.X *= value2.X;
			value1.Y *= value2.Y;
			return value1;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a multiplication of <see cref="Vector"/> and a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="scaleFactor">Scalar value.</param>
		/// <returns>The result of the vector multiplication with a scalar.</returns>
		public static Vector Multiply(Vector value1, float scaleFactor)
		{
			value1.X *= scaleFactor;
			value1.Y *= scaleFactor;
			return value1;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a multiplication of <see cref="Vector"/> and a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="scaleFactor">Scalar value.</param>
		/// <param name="result">The result of the multiplication with a scalar as an output parameter.</param>
		public static void Multiply(ref Vector value1, float scaleFactor, out Vector result)
		{
			result.X = value1.X * scaleFactor;
			result.Y = value1.Y * scaleFactor;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a multiplication of two vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <param name="result">The result of the vector multiplication as an output parameter.</param>
		public static void Multiply(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X * value2.X;
			result.Y = value1.Y * value2.Y;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains the specified vector inversion.
		/// direction of <paramref name="value"/>.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/>.</param>
		/// <returns>The result of the vector inversion.</returns>
		public static Vector Negate(Vector value)
		{
			value.X = -value.X;
			value.Y = -value.Y;
			return value;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains the specified vector inversion.
		/// direction of <paramref name="value"/> in <paramref name="result"/>.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/>.</param>
		/// <param name="result">The result of the vector inversion as an output parameter.</param>
		public static void Negate(ref Vector value, out Vector result)
		{
			result.X = -value.X;
			result.Y = -value.Y;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a normalized values from another vector.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/>.</param>
		/// <returns>Unit vector.</returns>
		public static Vector Normalize(Vector value)
		{
			float val = 1.0f / (float) Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
			value.X *= val;
			value.Y *= val;
			return value;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains a normalized values from another vector.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/>.</param>
		/// <param name="result">Unit vector as an output parameter.</param>
		public static void Normalize(ref Vector value, out Vector result)
		{
			float val = 1.0f / (float) Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
			result.X = value.X * val;
			result.Y = value.Y * val;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains reflect vector of the given vector and normal.
		/// </summary>
		/// <param name="vector">Source <see cref="Vector"/>.</param>
		/// <param name="normal">Reflection normal.</param>
		/// <returns>Reflected vector.</returns>
		public static Vector Reflect(Vector vector, Vector normal)
		{
			Vector result;
			float val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
			result.X = vector.X - (normal.X * val);
			result.Y = vector.Y - (normal.Y * val);
			return result;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains reflect vector of the given vector and normal.
		/// </summary>
		/// <param name="vector">Source <see cref="Vector"/>.</param>
		/// <param name="normal">Reflection normal.</param>
		/// <param name="result">Reflected vector as an output parameter.</param>
		public static void Reflect(ref Vector vector, ref Vector normal, out Vector result)
		{
			float val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
			result.X = vector.X - (normal.X * val);
			result.Y = vector.Y - (normal.Y * val);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains cubic interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <param name="amount">Weighting value.</param>
		/// <returns>Cubic interpolation of the specified vectors.</returns>
		public static Vector SmoothStep(Vector value1, Vector value2, float amount)
		{
			return new Vector(
				MathHelper.SmoothStep(value1.X, value2.X, amount),
				MathHelper.SmoothStep(value1.Y, value2.Y, amount)
			);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains cubic interpolation of the specified vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <param name="amount">Weighting value.</param>
		/// <param name="result">Cubic interpolation of the specified vectors as an output parameter.</param>
		public static void SmoothStep(
			ref Vector value1,
			ref Vector value2,
			float amount,
			out Vector result
		) {
			result.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
			result.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains subtraction of on <see cref="Vector"/> from a another.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <returns>The result of the vector subtraction.</returns>
		public static Vector Subtract(Vector value1, Vector value2)
		{
			value1.X -= value2.X;
			value1.Y -= value2.Y;
			return value1;
		}

		/// <summary>
		/// Creates a new <see cref="Vector"/> that contains subtraction of on <see cref="Vector"/> from a another.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/>.</param>
		/// <param name="value2">Source <see cref="Vector"/>.</param>
		/// <param name="result">The result of the vector subtraction as an output parameter.</param>
		public static void Subtract(ref Vector value1, ref Vector value2, out Vector result)
		{
			result.X = value1.X - value2.X;
			result.Y = value1.Y - value2.Y;
		}

		#endregion

		#region Public Static Operators

		/// <summary>
		/// Inverts values in the specified <see cref="Vector"/>.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/> on the right of the sub sign.</param>
		/// <returns>Result of the inversion.</returns>
		public static Vector operator -(Vector value)
		{
			value.X = -value.X;
			value.Y = -value.Y;
			return value;
		}

		/// <summary>
		/// Compares whether two <see cref="Vector"/> instances are equal.
		/// </summary>
		/// <param name="value1"><see cref="Vector"/> instance on the left of the equal sign.</param>
		/// <param name="value2"><see cref="Vector"/> instance on the right of the equal sign.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public static bool operator ==(Vector value1, Vector value2)
		{
			return (	value1.X == value2.X &&
					value1.Y == value2.Y	);
		}

		/// <summary>
		/// Compares whether two <see cref="Vector"/> instances are equal.
		/// </summary>
		/// <param name="value1"><see cref="Vector"/> instance on the left of the equal sign.</param>
		/// <param name="value2"><see cref="Vector"/> instance on the right of the equal sign.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public static bool operator !=(Vector value1, Vector value2)
		{
			return !(value1 == value2);
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/> on the left of the add sign.</param>
		/// <param name="value2">Source <see cref="Vector"/> on the right of the add sign.</param>
		/// <returns>Sum of the vectors.</returns>
		public static Vector operator +(Vector value1, Vector value2)
		{
			value1.X += value2.X;
			value1.Y += value2.Y;
			return value1;
		}

		/// <summary>
		/// Subtracts a <see cref="Vector"/> from a <see cref="Vector"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/> on the left of the sub sign.</param>
		/// <param name="value2">Source <see cref="Vector"/> on the right of the sub sign.</param>
		/// <returns>Result of the vector subtraction.</returns>
		public static Vector operator -(Vector value1, Vector value2)
		{
			value1.X -= value2.X;
			value1.Y -= value2.Y;
			return value1;
		}

		/// <summary>
		/// Multiplies the components of two vectors by each other.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/> on the left of the mul sign.</param>
		/// <param name="value2">Source <see cref="Vector"/> on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication.</returns>
		public static Vector operator *(Vector value1, Vector value2)
		{
			value1.X *= value2.X;
			value1.Y *= value2.Y;
			return value1;
		}

		/// <summary>
		/// Multiplies the components of vector by a scalar.
		/// </summary>
		/// <param name="value">Source <see cref="Vector"/> on the left of the mul sign.</param>
		/// <param name="scaleFactor">Scalar value on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication with a scalar.</returns>
		public static Vector operator *(Vector value, float scaleFactor)
		{
			value.X *= scaleFactor;
			value.Y *= scaleFactor;
			return value;
		}

		/// <summary>
		/// Multiplies the components of vector by a scalar.
		/// </summary>
		/// <param name="scaleFactor">Scalar value on the left of the mul sign.</param>
		/// <param name="value">Source <see cref="Vector"/> on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication with a scalar.</returns>
		public static Vector operator *(float scaleFactor, Vector value)
		{
			value.X *= scaleFactor;
			value.Y *= scaleFactor;
			return value;
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by the components of another <see cref="Vector"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/> on the left of the div sign.</param>
		/// <param name="value2">Divisor <see cref="Vector"/> on the right of the div sign.</param>
		/// <returns>The result of dividing the vectors.</returns>
		public static Vector operator /(Vector value1, Vector value2)
		{
			value1.X /= value2.X;
			value1.Y /= value2.Y;
			return value1;
		}

		/// <summary>
		/// Divides the components of a <see cref="Vector"/> by a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="Vector"/> on the left of the div sign.</param>
		/// <param name="divider">Divisor scalar on the right of the div sign.</param>
		/// <returns>The result of dividing a vector by a scalar.</returns>
		public static Vector operator /(Vector value1, float divider)
		{
			float factor = 1 / divider;
			value1.X *= factor;
			value1.Y *= factor;
			return value1;
		}

		#endregion
	}
}