﻿using System;

namespace ExplorerEx.Utils; 

internal static class DoubleUtil {
	internal const double DblEpsilon = 1.1102230246251567e-016;

	/// <summary>
	/// AreClose - Returns whether or not two doubles are "close".  That is, whether or
	/// not they are within epsilon of each other.  Note that this epsilon is proportional
	/// to the numbers themselves to that AreClose survives scalar multiplication.
	/// There are plenty of ways for this to return false even for numbers which
	/// are theoretically identical, so no code calling this should fail to work if this
	/// returns false.  This is important enough to repeat:
	/// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
	/// used for optimizations *only*.
	/// </summary>
	/// <returns>
	/// bool - the result of the AreClose comparison.
	/// </returns>
	/// <param name="value1">The first double to compare.</param>
	/// <param name="value2">The second double to compare.</param>
	public static bool AreClose(double value1, double value2) {
		// in case they are Infinities (then epsilon check does not work)
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (value1 == value2) {
			return true;
		}

		// This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
		var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DblEpsilon;
		var delta = value1 - value2;
		return -eps < delta && eps > delta;
	}

	/// <summary>
	/// GreaterThan - Returns whether or not the first double is greater than the second double.
	/// That is, whether or not the first is strictly greater than *and* not within epsilon of
	/// the other number.  Note that this epsilon is proportional to the numbers themselves
	/// to that AreClose survives scalar multiplication.
	/// There are plenty of ways for this to return false even for numbers which
	/// are theoretically identical, so no code calling this should fail to work if this
	/// returns false.  This is important enough to repeat:
	/// NB: This method should be used for optimizations only.
	/// </summary>
	/// <returns>
	/// bool - the result of the GreaterThan comparison.
	/// </returns>
	/// <param name="value1">The first double to compare.</param>
	/// <param name="value2">The second double to compare.</param>
	public static bool GreaterThan(double value1, double value2) {
		return value1 > value2 && !AreClose(value1, value2);
	}

	/// <summary>
	/// GreaterThanOrClose - Returns whether or not the first double is greater than or close to
	/// the second double.  That is, whether or not the first is strictly greater than or within
	/// epsilon of the other number.  Note that this epsilon is proportional to the numbers
	/// themselves to that AreClose survives scalar multiplication.
	/// There are plenty of ways for this to return false even for numbers which
	/// are theoretically identical, so no code calling this should fail to work if this
	/// returns false.  This is important enough to repeat:
	/// NB: This method should be used for optimizations only.
	/// </summary>
	/// <returns>
	/// bool - the result of the GreaterThanOrClose comparison.
	/// </returns>
	/// <param name="value1">The first double to compare.</param>
	/// <param name="value2">The second double to compare.</param>
	public static bool GreaterThanOrClose(double value1, double value2) {
		return value1 > value2 || AreClose(value1, value2);
	}

	/// <summary>
	/// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
	/// but this is faster.
	/// </summary>
	/// <returns>
	/// bool - the result of the IsZero comparison.
	/// </returns>
	/// <param name="value">The double to compare to 0.</param>
	public static bool IsZero(double value) {
		return Math.Abs(value) < 10.0 * DblEpsilon;
	}

	/// <summary>
	/// LessThan - Returns whether or not the first double is less than the second double.
	/// That is, whether or not the first is strictly less than *and* not within epsilon of
	/// the other number.  Note that this epsilon is proportional to the numbers themselves
	/// to that AreClose survives scalar multiplication.
	/// There are plenty of ways for this to return false even for numbers which
	/// are theoretically identical, so no code calling this should fail to work if this
	/// returns false.  This is important enough to repeat:
	/// NB: This method should be used for optimizations only.
	/// </summary>
	/// <returns>
	/// bool - the result of the LessThan comparison.
	/// </returns>
	/// <param name="value1">The first double to compare.</param>
	/// <param name="value2">The second double to compare.</param>
	public static bool LessThan(double value1, double value2) {
		return value1 < value2 && !AreClose(value1, value2);
	}

	/// <summary>
	/// LessThanOrClose - Returns whether or not the first double is less than or close to
	/// the second double.  That is, whether or not the first is strictly less than or within
	/// epsilon of the other number.  Note that this epsilon is proportional to the numbers
	/// themselves to that AreClose survives scalar multiplication.
	/// There are plenty of ways for this to return false even for numbers which
	/// are theoretically identical, so no code calling this should fail to work if this
	/// returns false.  This is important enough to repeat:
	/// NB: This method should be used for optimizations only.
	/// </summary>
	/// <returns>
	/// bool - the result of the LessThanOrClose comparison.
	/// </returns>
	/// <param name="value1">The first double to compare.</param>
	/// <param name="value2">The second double to compare.</param>
	public static bool LessThanOrClose(double value1, double value2) {
		return value1 < value2 || AreClose(value1, value2);
	}
}