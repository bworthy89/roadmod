using System;

namespace Colossal.Atmosphere;

public struct JulianDateTime
{
	private static readonly JulianDateTime J2000 = new JulianDateTime(2451545.0);

	private const double kSecPerDay = 86400.0;

	private const double kOmegaE = 1.00273790934;

	private long m_Day;

	private double m_Fraction;

	public static implicit operator JulianDateTime(DateTime utc)
	{
		return new JulianDateTime(utc);
	}

	public static implicit operator double(JulianDateTime aDate)
	{
		return aDate.ToDouble();
	}

	public static implicit operator DateTime(JulianDateTime aDate)
	{
		return aDate.ToDateTime();
	}

	public JulianDateTime(double j)
	{
		m_Day = (long)j;
		m_Fraction = j - (double)m_Day;
	}

	public JulianDateTime(JulianDateTime j)
	{
		m_Day = j.m_Day;
		m_Fraction = j.m_Fraction;
	}

	public JulianDateTime(DateTime utc)
	{
		long num = utc.Year;
		long num2 = utc.Month;
		long num3 = utc.Day;
		long num4 = 1461 * (num + 4800 + (num2 - 14) / 12) / 4;
		num4 += 367 * (num2 - 2 - 12 * ((num2 - 14) / 12)) / 12;
		num4 -= 3 * ((num + 4900 + (num2 - 14) / 12) / 100) / 4;
		num4 += num3 - 32075;
		double num5 = utc.TimeOfDay.TotalDays - 0.5;
		if (num5 < 0.0)
		{
			num5 += 1.0;
			num4--;
		}
		m_Day = num4;
		m_Fraction = num5;
	}

	public double ToDouble()
	{
		return (double)m_Day + m_Fraction;
	}

	public DateTime ToDateTime()
	{
		long num = m_Day;
		double num2 = m_Fraction + 0.5;
		if (num2 >= 1.0)
		{
			num2 -= 1.0;
			num++;
		}
		num += 68569;
		long num3 = 4 * num / 146097;
		num -= (146097 * num3 + 3) / 4;
		long num4 = 4000 * (num + 1) / 1461001;
		num -= 1461 * num4 / 4 - 31;
		long num5 = 80 * num / 2447;
		int day = (int)(num - 2447 * num5 / 80);
		num = num5 / 11;
		int month = (int)(num5 + 2 - 12 * num);
		int year = (int)(100 * (num3 - 49) + num4 + num);
		return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddDays(num2);
	}

	public override string ToString()
	{
		if (double.IsNaN(ToDouble()))
		{
			return "N/A";
		}
		return ToDateTime().ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
	}

	public double ToGMST()
	{
		double num = (ToDouble() + 0.5) % 1.0;
		double num2 = (J2000 - num) / 36525.0;
		double num3 = 24110.54841 + num2 * (8640184.812866 + num2 * (0.093104 - num2 * 6.2E-06));
		num3 = (num3 + 86636.555366976 * num) % 86400.0;
		if (num3 < 0.0)
		{
			num3 += 86400.0;
		}
		return 6.2831854820251465 * (num3 / 86400.0);
	}

	public double ToLMST(double longitude)
	{
		return (ToGMST() + longitude) % 3.1415927410125732 * 2.0;
	}

	public void AddSeconds(double seconds)
	{
		m_Fraction += seconds / 86400.0;
		while (m_Fraction >= 1.0)
		{
			m_Fraction -= 1.0;
			m_Day++;
		}
		while (m_Fraction < 0.0)
		{
			m_Fraction += 1.0;
			m_Day--;
		}
	}

	public double Subtract(JulianDateTime j)
	{
		m_Day -= j.m_Day;
		m_Fraction -= j.m_Fraction;
		if (m_Fraction < 0.0)
		{
			m_Fraction += 1.0;
			m_Day--;
		}
		return ToDouble();
	}

	public double Subtract(double j)
	{
		return ToDouble() - j;
	}

	public static double operator -(JulianDateTime l, double r)
	{
		return new JulianDateTime(l).Subtract(r);
	}

	public static double operator -(JulianDateTime l, JulianDateTime j)
	{
		return new JulianDateTime(l).Subtract(j);
	}
}
