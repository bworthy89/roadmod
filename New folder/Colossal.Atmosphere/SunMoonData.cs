using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Colossal.Atmosphere;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct SunMoonData
{
	public struct SunTimes
	{
		public JulianDateTime solarNoon;

		public JulianDateTime nadir;

		public JulianDateTime sunrise;

		public JulianDateTime sunset;

		public JulianDateTime sunriseEnd;

		public JulianDateTime sunsetStart;

		public JulianDateTime dawn;

		public JulianDateTime dusk;

		public JulianDateTime nauticalDawn;

		public JulianDateTime nauticalDusk;

		public JulianDateTime nightEnd;

		public JulianDateTime night;

		public JulianDateTime goldenHourEnd;

		public JulianDateTime goldenHour;

		public override string ToString()
		{
			return $"SolarNoon: {solarNoon}\nNadir: {nadir}\nSunrise: {sunrise}\nSunset: {sunset}\nSunriseEnd: {sunriseEnd}\nSunsetStart: {sunsetStart}\nDawn: {dawn}\nDusk: {dusk}\nNauticalDawn: {nauticalDawn}\nNauticalDusk: {nauticalDusk}\nNightEnd: {nightEnd}\nNight: {night}\nGoldenHourEnd: {goldenHourEnd}\nGoldenHour: {goldenHour}\n";
		}
	}

	private static readonly double e = math.radians(23.4397);

	private const double J0 = 0.0009;

	private const double kDayMs = 86400000.0;

	private const double J2000 = 2451545.0;

	public static readonly double h0 = math.radians(-0.833);

	public static readonly double h1 = math.radians(-0.3);

	public static readonly double d0 = math.radians(-6.0);

	public static readonly double d1 = math.radians(-12.0);

	public static readonly double d2 = math.radians(-18.0);

	public static readonly double g0 = math.radians(6.0);

	private double GetRightAscension(double l, double b)
	{
		return math.atan2(math.sin(l) * math.cos(e) - math.tan(b) * math.sin(e), math.cos(l));
	}

	private double GetDeclination(double l, double b)
	{
		return math.asin(math.sin(b) * math.cos(e) + math.cos(b) * math.sin(e) * math.sin(l));
	}

	private double GetAzimuth(double H, double phi, double dec)
	{
		return math.atan2(math.sin(H), math.cos(H) * math.sin(phi) - math.tan(dec) * math.cos(phi)) + Math.PI;
	}

	private double GetAltitude(double H, double phi, double dec)
	{
		return math.asin(math.sin(phi) * math.sin(dec) + math.cos(phi) * math.cos(dec) * math.cos(H));
	}

	private double GetSiderealTime(double d, double lw)
	{
		return math.radians(280.16 + 360.9856235 * d) - lw;
	}

	private double GetAstroRefraction(double h)
	{
		if (h < 0.0)
		{
			h = 0.0;
		}
		return 0.0002967 / math.tan(h + 0.00312536 / (h + 0.08901179));
	}

	private double GetSolarMeanAnomaly(double d)
	{
		return math.radians(357.5291 + 0.98560028 * d);
	}

	private double GetEclipticLongitude(double M)
	{
		double num = math.radians(1.9148 * math.sin(M) + 0.02 * math.sin(2.0 * M) + 0.0003 * math.sin(3.0 * M));
		double num2 = math.radians(102.9372);
		return M + num + num2 + Math.PI;
	}

	private EquatorialCoordinate GetSunCoords(double d)
	{
		double solarMeanAnomaly = GetSolarMeanAnomaly(d);
		double eclipticLongitude = GetEclipticLongitude(solarMeanAnomaly);
		return new EquatorialCoordinate
		{
			declination = GetDeclination(eclipticLongitude, 0.0),
			rightAscension = GetRightAscension(eclipticLongitude, 0.0)
		};
	}

	public TopocentricCoordinates GetSunPosition(JulianDateTime date, double latitude, double longitude)
	{
		double lw = math.radians(0.0 - longitude);
		double phi = math.radians(latitude);
		double d = date.ToDouble() - 2451545.0;
		EquatorialCoordinate sunCoords = GetSunCoords(d);
		double h = GetSiderealTime(d, lw) - sunCoords.rightAscension;
		return new TopocentricCoordinates
		{
			azimuth = GetAzimuth(h, phi, sunCoords.declination),
			altitude = GetAltitude(h, phi, sunCoords.declination)
		};
	}

	private double GetJulianCycle(double d, double lw)
	{
		return math.round(d - 0.0009 - lw / (Math.PI * 2.0));
	}

	private double GetApproximateSolarTransit(double Ht, double lw, double n)
	{
		return 0.0009 + (Ht + lw) / (Math.PI * 2.0) + n;
	}

	private double GetSolarTransit(double ds, double M, double L)
	{
		return 2451545.0 + ds + 0.0053 * math.sin(M) - 0.0069 * math.sin(2.0 * L);
	}

	private double GetHourAngle(double h, double phi, double d)
	{
		return math.acos((math.sin(h) - math.sin(phi) * math.sin(d)) / (math.cos(phi) * math.cos(d)));
	}

	private double GetTimeForSunAltitude(double h, double lw, double phi, double dec, double n, double M, double L)
	{
		double hourAngle = GetHourAngle(h, phi, dec);
		double approximateSolarTransit = GetApproximateSolarTransit(hourAngle, lw, n);
		return GetSolarTransit(approximateSolarTransit, M, L);
	}

	public SunTimes GetSunTimes(JulianDateTime date, double latitude, double longitude)
	{
		double lw = math.radians(0.0 - longitude);
		double phi = math.radians(latitude);
		double d = date.ToDouble() - 2451545.0;
		double julianCycle = GetJulianCycle(d, lw);
		double approximateSolarTransit = GetApproximateSolarTransit(0.0, lw, julianCycle);
		double solarMeanAnomaly = GetSolarMeanAnomaly(approximateSolarTransit);
		double eclipticLongitude = GetEclipticLongitude(solarMeanAnomaly);
		double declination = GetDeclination(eclipticLongitude, 0.0);
		double solarTransit = GetSolarTransit(approximateSolarTransit, solarMeanAnomaly, eclipticLongitude);
		SunTimes result = default(SunTimes);
		result.solarNoon = new JulianDateTime(solarTransit);
		result.nadir = new JulianDateTime(solarTransit - 0.5);
		double timeForSunAltitude = GetTimeForSunAltitude(h0, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		double j = solarTransit - (timeForSunAltitude - solarTransit);
		result.sunrise = new JulianDateTime(j);
		result.sunset = new JulianDateTime(timeForSunAltitude);
		timeForSunAltitude = GetTimeForSunAltitude(h1, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		j = solarTransit - (timeForSunAltitude - solarTransit);
		result.sunriseEnd = new JulianDateTime(j);
		result.sunsetStart = new JulianDateTime(timeForSunAltitude);
		timeForSunAltitude = GetTimeForSunAltitude(d0, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		j = solarTransit - (timeForSunAltitude - solarTransit);
		result.dawn = new JulianDateTime(j);
		result.dusk = new JulianDateTime(timeForSunAltitude);
		timeForSunAltitude = GetTimeForSunAltitude(d1, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		j = solarTransit - (timeForSunAltitude - solarTransit);
		result.nauticalDawn = new JulianDateTime(j);
		result.nauticalDusk = new JulianDateTime(timeForSunAltitude);
		timeForSunAltitude = GetTimeForSunAltitude(d2, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		j = solarTransit - (timeForSunAltitude - solarTransit);
		result.nightEnd = new JulianDateTime(j);
		result.night = new JulianDateTime(timeForSunAltitude);
		timeForSunAltitude = GetTimeForSunAltitude(g0, lw, phi, declination, julianCycle, solarMeanAnomaly, eclipticLongitude);
		j = solarTransit - (timeForSunAltitude - solarTransit);
		result.goldenHourEnd = new JulianDateTime(j);
		result.goldenHour = new JulianDateTime(timeForSunAltitude);
		return result;
	}

	private MoonCoords GetMoonCoords(double d)
	{
		double num = math.radians(218.316 + 13.176396 * d);
		double x = math.radians(134.963 + 13.064993 * d);
		double x2 = math.radians(93.272 + 13.22935 * d);
		double l = num + math.radians(6.289 * math.sin(x));
		double b = math.radians(5.128 * math.sin(x2));
		double distance = 385001.0 - 20905.0 * math.cos(x);
		return new MoonCoords
		{
			equatorialCoords = new EquatorialCoordinate
			{
				rightAscension = GetRightAscension(l, b),
				declination = GetDeclination(l, b)
			},
			distance = distance
		};
	}

	public MoonCoordinate GetMoonPosition(JulianDateTime date, double latitude, double longitude)
	{
		double lw = math.radians(0.0 - longitude);
		double num = math.radians(latitude);
		double d = date.ToDouble() - 2451545.0;
		MoonCoords moonCoords = GetMoonCoords(d);
		double num2 = GetSiderealTime(d, lw) - moonCoords.equatorialCoords.rightAscension;
		double altitude = GetAltitude(num2, num, moonCoords.equatorialCoords.declination);
		double parallacticAngle = math.atan2(math.sin(num2), math.tan(num) * math.cos(moonCoords.equatorialCoords.declination) - math.sin(moonCoords.equatorialCoords.declination) * math.cos(num2));
		altitude += GetAstroRefraction(altitude);
		return new MoonCoordinate
		{
			topoCoords = new TopocentricCoordinates
			{
				azimuth = GetAzimuth(num2, num, moonCoords.equatorialCoords.declination),
				altitude = altitude
			},
			distance = moonCoords.distance,
			parallacticAngle = parallacticAngle
		};
	}

	public MoonIllumination GetMoonIllumination(JulianDateTime date)
	{
		double d = date.ToDouble() - 2451545.0;
		EquatorialCoordinate sunCoords = GetSunCoords(d);
		MoonCoords moonCoords = GetMoonCoords(d);
		double num = 149598000.0;
		double x = math.acos(math.sin(sunCoords.declination) * math.sin(moonCoords.equatorialCoords.declination) + math.cos(sunCoords.declination) * math.cos(moonCoords.equatorialCoords.declination) * math.cos(sunCoords.rightAscension - moonCoords.equatorialCoords.rightAscension));
		double num2 = math.atan2(num * math.sin(x), moonCoords.distance - num * math.cos(x));
		double num3 = math.atan2(math.cos(sunCoords.declination) * math.sin(sunCoords.rightAscension - moonCoords.equatorialCoords.rightAscension), math.sin(sunCoords.declination) * math.cos(moonCoords.equatorialCoords.declination) - math.cos(sunCoords.declination) * math.sin(moonCoords.equatorialCoords.declination) * math.cos(sunCoords.rightAscension - moonCoords.equatorialCoords.rightAscension));
		return new MoonIllumination
		{
			fraction = (1.0 + math.cos(num2)) / 2.0,
			phase = 0.5 + 0.5 * num2 * (double)((!(num3 < 0.0)) ? 1 : (-1)) / Math.PI,
			angle = num3
		};
	}
}
