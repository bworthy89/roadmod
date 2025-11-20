using Unity.Entities;

namespace Game.Prefabs;

public struct FeedbackConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_HappyFaceNotification;

	public Entity m_SadFaceNotification;

	public float m_GarbageProducerGarbageFactor;

	public float m_GarbageVehicleFactor;

	public float m_HospitalAmbulanceFactor;

	public float m_HospitalHelicopterFactor;

	public float m_HospitalCapacityFactor;

	public float m_DeathcareHearseFactor;

	public float m_DeathcareCapacityFactor;

	public float m_DeathcareProcessingFactor;

	public float m_ElectricityConsumptionFactor;

	public float m_ElectricityProductionFactor;

	public float m_TransformerRadius;

	public float m_WaterConsumptionFactor;

	public float m_WaterCapacityFactor;

	public float m_WaterConsumerSewageFactor;

	public float m_SewageCapacityFactor;

	public float m_TransportVehicleCapacityFactor;

	public float m_TransportDispatchCenterFactor;

	public float m_TransportStationRange;

	public float m_TransportStopRange;

	public float m_MailProducerMailFactor;

	public float m_PostFacilityVanFactor;

	public float m_PostFacilityTruckFactor;

	public float m_PostFacilityCapacityFactor;

	public float m_PostFacilityProcessingFactor;

	public float m_TelecomCapacityFactor;

	public float m_ElementarySchoolCapacityFactor;

	public float m_HighSchoolCapacityFactor;

	public float m_CollegeCapacityFactor;

	public float m_UniversityCapacityFactor;

	public float m_ParkingFacilityRange;

	public float m_MaintenanceVehicleFactor;

	public float m_FireStationEngineFactor;

	public float m_FireStationHelicopterFactor;

	public float m_CrimeProducerCrimeFactor;

	public float m_PoliceStationCarFactor;

	public float m_PoliceStationHelicopterFactor;

	public float m_PoliceStationCapacityFactor;

	public float m_PrisonVehicleFactor;

	public float m_PrisonCapacityFactor;

	public float m_GroundPollutionFactor;

	public float m_AirPollutionFactor;

	public float m_NoisePollutionFactor;

	public float m_GroundPollutionRadius;

	public float m_AirPollutionRadius;

	public float m_NoisePollutionRadius;

	public float m_AttractivenessFactor;
}
