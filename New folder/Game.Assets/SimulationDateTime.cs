using System;
using Colossal.Json;
using Colossal.UI.Binding;

namespace Game.Assets;

public struct SimulationDateTime : IEquatable<SimulationDateTime>, IJsonReadable, IJsonWritable
{
	public int year;

	public int month;

	public int hour;

	public int minute;

	private static void SupportValueTypesForAOT()
	{
		JSON.SupportTypeForAOT<SimulationDateTime>();
	}

	public SimulationDateTime(int year, int month, int hour, int minute)
	{
		this.year = year;
		this.month = month;
		this.hour = hour;
		this.minute = minute;
	}

	public bool Equals(SimulationDateTime other)
	{
		int num = year;
		int num2 = month;
		int num3 = hour;
		int num4 = minute;
		int num5 = other.year;
		int num6 = other.month;
		int num7 = other.hour;
		int num8 = other.minute;
		if (num == num5 && num2 == num6 && num3 == num7)
		{
			return num4 == num8;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SimulationDateTime other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (year, month, hour, minute).GetHashCode();
	}

	public static bool operator ==(SimulationDateTime left, SimulationDateTime right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SimulationDateTime left, SimulationDateTime right)
	{
		return !left.Equals(right);
	}

	public void Read(IJsonReader reader)
	{
		reader.ReadMapBegin();
		reader.ReadProperty("year");
		reader.Read(out year);
		reader.ReadProperty("month");
		reader.Read(out month);
		reader.ReadProperty("hour");
		reader.Read(out hour);
		reader.ReadProperty("minute");
		reader.Read(out minute);
		reader.ReadMapEnd();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("year");
		writer.Write(year);
		writer.PropertyName("month");
		writer.Write(month);
		writer.PropertyName("hour");
		writer.Write(hour);
		writer.PropertyName("minute");
		writer.Write(minute);
		writer.TypeEnd();
	}
}
