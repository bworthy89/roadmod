using System;
using Colossal.Json;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.Settings;

public struct ScreenResolution : IEquatable<ScreenResolution>, IComparable<ScreenResolution>, IJsonReadable, IJsonWritable
{
	public int width;

	public int height;

	public RefreshRate refreshRate;

	public double refreshRateDelta => Math.Abs(Math.Round(refreshRate.value) - refreshRate.value);

	public bool isValid
	{
		get
		{
			if (width > 0 && height > 0 && refreshRate.numerator != 0)
			{
				return refreshRate.denominator != 0;
			}
			return false;
		}
	}

	private static void SupportValueTypesForAOT()
	{
		JSON.SupportTypeForAOT<ScreenResolution>();
		JSON.SupportTypeForAOT<RefreshRate>();
	}

	public ScreenResolution(Resolution resolution)
	{
		width = resolution.width;
		height = resolution.height;
		refreshRate = resolution.refreshRateRatio;
	}

	public bool Equals(ScreenResolution other)
	{
		int num = width;
		int num2 = height;
		uint numerator = refreshRate.numerator;
		uint denominator = refreshRate.denominator;
		int num3 = other.width;
		int num4 = other.height;
		uint numerator2 = other.refreshRate.numerator;
		uint denominator2 = other.refreshRate.denominator;
		if (num == num3 && num2 == num4 && numerator == numerator2)
		{
			return denominator == denominator2;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ScreenResolution other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (width, height, refreshRate).GetHashCode();
	}

	public static bool operator ==(ScreenResolution left, ScreenResolution right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ScreenResolution left, ScreenResolution right)
	{
		return !left.Equals(right);
	}

	public void Sanitize()
	{
		if (refreshRate.numerator == 0 || refreshRate.denominator == 0 || double.IsNaN(refreshRate.value))
		{
			refreshRate = Screen.currentResolution.refreshRateRatio;
		}
	}

	public int CompareTo(ScreenResolution other)
	{
		int num = width.CompareTo(other.width);
		if (num != 0)
		{
			return num;
		}
		int num2 = height.CompareTo(other.height);
		if (num2 != 0)
		{
			return num2;
		}
		return refreshRate.value.CompareTo(other.refreshRate.value);
	}

	public void Read(IJsonReader reader)
	{
		reader.ReadMapBegin();
		reader.ReadProperty("width");
		reader.Read(out width);
		reader.ReadProperty("height");
		reader.Read(out height);
		reader.ReadProperty("numerator");
		reader.Read(out refreshRate.numerator);
		reader.ReadProperty("denominator");
		reader.Read(out refreshRate.denominator);
		reader.ReadMapEnd();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(ScreenResolution).FullName);
		writer.PropertyName("width");
		writer.Write(width);
		writer.PropertyName("height");
		writer.Write(height);
		writer.PropertyName("numerator");
		writer.Write(refreshRate.numerator);
		writer.PropertyName("denominator");
		writer.Write(refreshRate.denominator);
		writer.TypeEnd();
	}

	public override string ToString()
	{
		return $"{width}x{height}x{refreshRate.value}Hz";
	}
}
