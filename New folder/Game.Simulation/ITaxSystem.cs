using Game.City;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public interface ITaxSystem
{
	int TaxRate { get; set; }

	JobHandle Readers { get; }

	TaxParameterData GetTaxParameterData();

	int GetTaxRate(TaxAreaType areaType);

	void SetTaxRate(TaxAreaType areaType, int rate);

	int2 GetTaxRateRange(TaxAreaType areaType);

	int GetResidentialTaxRate(int jobLevel);

	void SetResidentialTaxRate(int jobLevel, int rate);

	int GetCommercialTaxRate(Resource resource);

	void SetCommercialTaxRate(Resource resource, int rate);

	int GetIndustrialTaxRate(Resource resource);

	void SetIndustrialTaxRate(Resource resource, int rate);

	int GetOfficeTaxRate(Resource resource);

	void SetOfficeTaxRate(Resource resource, int rate);

	int GetTaxRateEffect(TaxAreaType areaType, int taxRate);

	int GetEstimatedTaxAmount(TaxAreaType areaType, TaxResultType resultType, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats);

	int GetEstimatedResidentialTaxIncome(int jobLevel, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats);

	int GetEstimatedCommercialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats);

	int GetEstimatedIndustrialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats);

	int GetEstimatedOfficeTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats);
}
