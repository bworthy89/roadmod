using Game.City;
using Game.Economy;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Simulation;

public interface ITradeSystem
{
	float GetBestTradePriceAmongTypes(Resource resource, OutsideConnectionTransferType types, bool import, DynamicBuffer<CityModifier> cityEffects);
}
