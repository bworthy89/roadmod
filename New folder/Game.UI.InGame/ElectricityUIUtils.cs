using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;

namespace Game.UI.InGame;

public static class ElectricityUIUtils
{
	public static bool HasVoltageLayers(Layer layers)
	{
		return (layers & (Layer.PowerlineLow | Layer.PowerlineHigh)) != 0;
	}

	public static VoltageLocaleKey GetVoltage(Layer layers)
	{
		return (layers & (Layer.PowerlineLow | Layer.PowerlineHigh)) switch
		{
			Layer.PowerlineLow => VoltageLocaleKey.Low, 
			Layer.PowerlineHigh => VoltageLocaleKey.High, 
			_ => VoltageLocaleKey.Both, 
		};
	}

	public static Layer GetPowerLineLayers(EntityManager entityManager, Entity prefabEntity)
	{
		Layer layer = Layer.None;
		if (entityManager.HasComponent<TransformerData>(prefabEntity))
		{
			layer |= Layer.PowerlineLow;
		}
		if (entityManager.TryGetBuffer(prefabEntity, isReadOnly: true, out DynamicBuffer<Game.Prefabs.SubNet> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity prefab = buffer[i].m_Prefab;
				if (entityManager.HasComponent<ElectricityConnectionData>(prefab) && entityManager.TryGetComponent<NetData>(prefab, out var component))
				{
					layer |= component.m_LocalConnectLayers;
				}
			}
		}
		return layer & (Layer.PowerlineLow | Layer.PowerlineHigh);
	}
}
