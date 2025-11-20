using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public interface IZoneBuildingComponent
{
	void GetBuildingPrefabComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level);

	void GetBuildingArchetypeComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level);

	void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level);
}
