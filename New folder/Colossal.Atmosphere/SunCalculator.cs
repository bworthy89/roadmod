using System;
using Colossal.Atmosphere.Internal;
using Unity.Mathematics;

namespace Colossal.Atmosphere;

public class SunCalculator
{
	public struct Twiligth
	{
		public TimeFrame astronomical;

		public TimeFrame nautical;

		public TimeFrame civil;
	}

	public struct TimeFrame
	{
		public DateTime start;

		public DateTime end;

		public override string ToString()
		{
			return $"Start: {start} End: {end}";
		}
	}

	public struct DayInfo
	{
		public DateTime dawn;

		public TimeFrame sunrise;

		public DateTime transit;

		public TimeFrame sunset;

		public DateTime dusk;

		public Twiligth morningTwilight;

		public Twiligth nightTwilight;

		public override string ToString()
		{
			return $"Dawn: {dawn}\nSunrise: {sunrise}\nTransit: {transit}\nSunset: {sunset}\nDusk: {dusk}\nAstronomical morning twilight: {morningTwilight.astronomical}\nNautical morning twilight: {morningTwilight.nautical}\nCivil morning twilight: {morningTwilight.civil}\nCivil night twilight: {nightTwilight.civil}\nNautical night twilight: {nightTwilight.nautical}\nAstronomical morning twilight: {nightTwilight.astronomical}";
		}
	}

	private static readonly double J2000 = 2451545.0;

	private static readonly double h0 = math.radians(-0.83);

	private static readonly double d0 = math.radians(0.53);

	private static readonly double h1 = math.radians(-6f);

	private static readonly double h2 = math.radians(-12f);

	private static readonly double h3 = math.radians(-18f);

	private static readonly double P = math.radians(102.9372);

	private static readonly double e = math.radians(23.45);

	private static readonly double th0 = math.radians(280.16);

	private static readonly double th1 = math.radians(360.9856235);

	private static readonly double M0 = math.radians(357.5291);

	private static readonly double M1 = math.radians(0.98560028);

	private static readonly double C1 = math.radians(1.9148);

	private static readonly double C2 = math.radians(0.02);

	private static readonly double C3 = math.radians(0.0003);

	private static readonly double J0 = 0.0009;

	private static readonly double J1 = 0.0053;

	private static readonly double J2 = -0.0069;

	private static double GetEclipticLongitude(double M, double C)
	{
		return M + P + C + 3.1415927410125732;
	}

	private static double GetHourAngle(double h, double phi, double d)
	{
		return math.acos((math.sin(h) - math.sin(phi) * math.sin(d)) / (math.cos(phi) * math.cos(d)));
	}

	private static double GetSunDeclination(double Lsun)
	{
		return math.asin(math.sin(Lsun) * math.sin(e));
	}

	private static double GetSolarMeanAnomaly(double Js)
	{
		return M0 + M1 * (Js - J2000);
	}

	private static double GetEquationOfCenter(double M)
	{
		return C1 * math.sin(M) + C2 * math.sin(2.0 * M) + C3 * math.sin(3.0 * M);
	}

	private static double GetJulianCycle(double J, double lw)
	{
		return math.round(J - J2000 - J0 - lw / (Math.PI * 2.0));
	}

	private static double GetSolarTransit(double Js, double M, double Lsun)
	{
		return Js + J1 * math.sin(M) + J2 * math.sin(2.0 * Lsun);
	}

	private static double GetApproxSolarTransit(double Ht, double lw, double n)
	{
		return J2000 + J0 + (Ht + lw) / 6.2831854820251465 + n;
	}

	private static double GetSunsetJulianDate(double w0, double M, double Lsun, double lw, double n)
	{
		return GetSolarTransit(GetApproxSolarTransit(w0, lw, n), M, Lsun);
	}

	private static double GetSunriseJulianDate(double Jtransit, double Jset)
	{
		return Jtransit - (Jset - Jtransit);
	}

	private static double GetRightAscension(double Lsun)
	{
		return math.atan2(math.sin(Lsun) * math.cos(e), math.cos(Lsun));
	}

	private static double GetSiderealTime(double J, double lw)
	{
		return th0 + th1 * (J - J2000) - lw;
	}

	private static double GetAzimuth(double th, double a, double phi, double d)
	{
		double x = th - a;
		return math.atan2(math.sin(x), math.cos(x) * math.sin(phi) - math.tan(d) * math.cos(phi)) + 3.1415927410125732;
	}

	private static double GetAltitude(double th, double a, double phi, double d)
	{
		double x = th - a;
		return math.asin(math.sin(phi) * math.sin(d) + math.cos(phi) * math.cos(d) * math.cos(x));
	}

	public static DayInfo GetDayInfo(DateTime date, float latitude, float longitude)
	{
		double lw = math.radians(0f - longitude);
		double phi = math.radians(latitude);
		double julianCycle = GetJulianCycle(date.ConvertToJulianDateTime(), lw);
		double approxSolarTransit = GetApproxSolarTransit(0.0, lw, julianCycle);
		double solarMeanAnomaly = GetSolarMeanAnomaly(approxSolarTransit);
		double equationOfCenter = GetEquationOfCenter(solarMeanAnomaly);
		double eclipticLongitude = GetEclipticLongitude(solarMeanAnomaly, equationOfCenter);
		double sunDeclination = GetSunDeclination(eclipticLongitude);
		double solarTransit = GetSolarTransit(approxSolarTransit, solarMeanAnomaly, eclipticLongitude);
		double hourAngle = GetHourAngle(h0, phi, sunDeclination);
		double hourAngle2 = GetHourAngle(h0 + d0, phi, sunDeclination);
		double hourAngle3 = GetHourAngle(h1, phi, sunDeclination);
		double hourAngle4 = GetHourAngle(h2, phi, sunDeclination);
		double hourAngle5 = GetHourAngle(h3, phi, sunDeclination);
		double sunsetJulianDate = GetSunsetJulianDate(hourAngle, solarMeanAnomaly, eclipticLongitude, lw, julianCycle);
		double sunsetJulianDate2 = GetSunsetJulianDate(hourAngle2, solarMeanAnomaly, eclipticLongitude, lw, julianCycle);
		double sunsetJulianDate3 = GetSunsetJulianDate(hourAngle3, solarMeanAnomaly, eclipticLongitude, lw, julianCycle);
		double sunsetJulianDate4 = GetSunsetJulianDate(hourAngle4, solarMeanAnomaly, eclipticLongitude, lw, julianCycle);
		double sunsetJulianDate5 = GetSunsetJulianDate(hourAngle5, solarMeanAnomaly, eclipticLongitude, lw, julianCycle);
		double sunriseJulianDate = GetSunriseJulianDate(solarTransit, sunsetJulianDate);
		double sunriseJulianDate2 = GetSunriseJulianDate(solarTransit, sunsetJulianDate2);
		double sunriseJulianDate3 = GetSunriseJulianDate(solarTransit, sunsetJulianDate4);
		double sunriseJulianDate4 = GetSunriseJulianDate(solarTransit, sunsetJulianDate5);
		double sunriseJulianDate5 = GetSunriseJulianDate(solarTransit, sunsetJulianDate3);
		return new DayInfo
		{
			dawn = sunriseJulianDate5.ConvertToDateTime(),
			sunrise = 
			{
				start = sunriseJulianDate.ConvertToDateTime(),
				end = sunriseJulianDate2.ConvertToDateTime()
			},
			transit = solarTransit.ConvertToDateTime(),
			sunset = 
			{
				start = sunsetJulianDate2.ConvertToDateTime(),
				end = sunsetJulianDate.ConvertToDateTime()
			},
			dusk = sunsetJulianDate3.ConvertToDateTime(),
			morningTwilight = 
			{
				astronomical = 
				{
					start = sunriseJulianDate4.ConvertToDateTime(),
					end = sunriseJulianDate3.ConvertToDateTime()
				},
				nautical = 
				{
					start = sunriseJulianDate3.ConvertToDateTime(),
					end = sunriseJulianDate5.ConvertToDateTime()
				},
				civil = 
				{
					start = sunriseJulianDate5.ConvertToDateTime(),
					end = sunriseJulianDate.ConvertToDateTime()
				}
			},
			nightTwilight = 
			{
				civil = 
				{
					start = sunsetJulianDate.ConvertToDateTime(),
					end = sunsetJulianDate3.ConvertToDateTime()
				},
				nautical = 
				{
					start = sunsetJulianDate3.ConvertToDateTime(),
					end = sunsetJulianDate4.ConvertToDateTime()
				},
				astronomical = 
				{
					start = sunsetJulianDate4.ConvertToDateTime(),
					end = sunsetJulianDate5.ConvertToDateTime()
				}
			}
		};
	}

	private static TopocentricCoordinates GetSunPosition(double J, double lw, double phi)
	{
		double solarMeanAnomaly = GetSolarMeanAnomaly(J);
		double equationOfCenter = GetEquationOfCenter(solarMeanAnomaly);
		double eclipticLongitude = GetEclipticLongitude(solarMeanAnomaly, equationOfCenter);
		double sunDeclination = GetSunDeclination(eclipticLongitude);
		double rightAscension = GetRightAscension(eclipticLongitude);
		double siderealTime = GetSiderealTime(J, lw);
		return new TopocentricCoordinates
		{
			azimuth = GetAzimuth(siderealTime, rightAscension, phi, sunDeclination),
			altitude = GetAltitude(siderealTime, rightAscension, phi, sunDeclination)
		};
	}

	public static TopocentricCoordinates GetSunPosition(DateTime date, float latitude, float longitude)
	{
		return GetSunPosition(date.ConvertToJulianDateTime(), 0f - math.radians(longitude), math.radians(latitude));
	}
}
