using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Common;

public struct PseudoRandomSeed : IComponentData, IQueryTypeParameter, ISerializable
{
	public static readonly ushort kEffectCondition = 24997;

	public static readonly ushort kSubObject = 30691;

	public static readonly ushort kSecondaryObject = 6624;

	public static readonly ushort kSplitEdge = 22175;

	public static readonly ushort kEdgeNodes = 43059;

	public static readonly ushort kColorVariation = 47969;

	public static readonly ushort kBuildingState = 61698;

	public static readonly ushort kSubLane = 38092;

	public static readonly ushort kDummyPassengers = 16686;

	public static readonly ushort kLightState = 2545;

	public static readonly ushort kBrightnessLimit = 13328;

	public static readonly ushort kDrivingStyle = 45236;

	public static readonly ushort kFlowOffset = 8934;

	public static readonly ushort kMeshGroup = 60951;

	public static readonly ushort kCollapse = 12473;

	public static readonly ushort kDummyName = 29193;

	public static readonly ushort kTemperatureLimit = 4505;

	public static readonly ushort kAreaBorder = 35490;

	public static readonly ushort kParkedCars = 49180;

	public static readonly ushort kAnimationVariation = 33304;

	public static readonly ushort kFollowDistance = 57050;

	public ushort m_Seed;

	public PseudoRandomSeed(ushort seed)
	{
		m_Seed = seed;
	}

	public PseudoRandomSeed(ref Random random)
	{
		m_Seed = (ushort)random.NextUInt(65536u);
	}

	public Random GetRandom(uint reason)
	{
		Random result = new Random(math.max(1u, m_Seed ^ reason));
		result.NextUInt();
		result.NextUInt();
		return result;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Seed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Seed);
	}
}
