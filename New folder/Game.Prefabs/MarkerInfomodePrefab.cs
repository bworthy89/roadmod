using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class MarkerInfomodePrefab : InfomodeBasePrefab
{
	public MarkerType m_Type;

	public override string infomodeTypeLocaleKey => "Marker";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewMarkerData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewMarkerData
		{
			m_Type = m_Type
		});
	}
}
