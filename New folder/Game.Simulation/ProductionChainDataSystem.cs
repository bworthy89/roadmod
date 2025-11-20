using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class ProductionChainDataSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ProductionChainData : IAccumulable<ProductionChainData>, ISerializable
	{
		public void Accumulate(ProductionChainData other)
		{
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
		}
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
	}

	public void SetDefaults(Context context)
	{
	}

	[Preserve]
	public ProductionChainDataSystem()
	{
	}
}
