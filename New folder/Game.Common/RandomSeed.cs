using System;
using Unity.Mathematics;

namespace Game.Common;

public struct RandomSeed
{
	private static Unity.Mathematics.Random m_Random = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);

	private uint m_Seed;

	public static RandomSeed Next()
	{
		return new RandomSeed
		{
			m_Seed = m_Random.NextUInt()
		};
	}

	public Unity.Mathematics.Random GetRandom(int index)
	{
		uint num = m_Seed ^ (uint)(370248451 * index);
		return new Unity.Mathematics.Random(math.select(num, 1851936439u, num == 0));
	}
}
