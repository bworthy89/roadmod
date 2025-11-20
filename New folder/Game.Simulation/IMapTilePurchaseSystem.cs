using Game.Areas;

namespace Game.Simulation;

public interface IMapTilePurchaseSystem
{
	bool selecting { get; set; }

	int cost { get; }

	TilePurchaseErrorFlags status { get; }

	float GetFeatureAmount(MapFeature feature);

	void PurchaseSelection();

	void Update();
}
