using Unity.Mathematics;

namespace Colossal.Atmosphere;

public struct TopocentricCoordinates
{
	public double azimuth;

	public double altitude;

	private static readonly string[] kCardinals = new string[9] { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };

	private float Remap(float value, float from1, float to1, float from2, float to2)
	{
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	public float3 ToLocalCoordinates(out float planetTime)
	{
		double num = 1.5707963705062866 - altitude;
		double num2 = azimuth;
		planetTime = math.saturate(Remap((float)num, -1.5f, 0f, 1.5f, 1f));
		return ConvertToLocalCoordinates((float)num, (float)num2);
	}

	public static float3 ConvertToLocalCoordinates(float theta, float phi)
	{
		float num = math.sin(theta);
		return new float3(num * math.sin(phi), math.cos(theta), num * math.cos(phi));
	}

	public void Quantize(double resolutionRadians)
	{
		azimuth = math.round(azimuth / resolutionRadians) * resolutionRadians;
		altitude = math.round(altitude / resolutionRadians) * resolutionRadians;
	}

	private static string DegreesToCardinal(double degrees)
	{
		return kCardinals[(int)math.round(degrees % 360.0 / 45.0)];
	}

	private static string FormatAzimuth(double azimuth)
	{
		double num = math.degrees(azimuth);
		return $"{azimuth} ({num}° - {DegreesToCardinal(num)})";
	}

	private static string FormatAltitude(double altitude)
	{
		double num = math.degrees(altitude);
		return $"{altitude} ({num}°)";
	}

	public override string ToString()
	{
		return "(azimuth: " + FormatAzimuth(azimuth) + ", altitude: " + FormatAltitude(altitude) + ")";
	}
}
