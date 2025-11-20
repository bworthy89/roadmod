using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct HospitalData : IComponentData, IQueryTypeParameter, ICombineData<HospitalData>, ISerializable
{
	public int m_AmbulanceCapacity;

	public int m_MedicalHelicopterCapacity;

	public int m_PatientCapacity;

	public int m_TreatmentBonus;

	public int2 m_HealthRange;

	public bool m_TreatDiseases;

	public bool m_TreatInjuries;

	public void Combine(HospitalData otherData)
	{
		m_AmbulanceCapacity += otherData.m_AmbulanceCapacity;
		m_MedicalHelicopterCapacity += otherData.m_MedicalHelicopterCapacity;
		m_PatientCapacity += otherData.m_PatientCapacity;
		m_TreatmentBonus += otherData.m_TreatmentBonus;
		m_HealthRange.x = math.min(m_HealthRange.x, otherData.m_HealthRange.x);
		m_HealthRange.y = math.max(m_HealthRange.y, otherData.m_HealthRange.y);
		m_TreatDiseases |= otherData.m_TreatDiseases;
		m_TreatInjuries |= otherData.m_TreatInjuries;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int ambulanceCapacity = m_AmbulanceCapacity;
		writer.Write(ambulanceCapacity);
		int medicalHelicopterCapacity = m_MedicalHelicopterCapacity;
		writer.Write(medicalHelicopterCapacity);
		int patientCapacity = m_PatientCapacity;
		writer.Write(patientCapacity);
		int treatmentBonus = m_TreatmentBonus;
		writer.Write(treatmentBonus);
		int2 healthRange = m_HealthRange;
		writer.Write(healthRange);
		bool treatDiseases = m_TreatDiseases;
		writer.Write(treatDiseases);
		bool treatInjuries = m_TreatInjuries;
		writer.Write(treatInjuries);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int ambulanceCapacity = ref m_AmbulanceCapacity;
		reader.Read(out ambulanceCapacity);
		ref int medicalHelicopterCapacity = ref m_MedicalHelicopterCapacity;
		reader.Read(out medicalHelicopterCapacity);
		ref int patientCapacity = ref m_PatientCapacity;
		reader.Read(out patientCapacity);
		ref int treatmentBonus = ref m_TreatmentBonus;
		reader.Read(out treatmentBonus);
		ref int2 healthRange = ref m_HealthRange;
		reader.Read(out healthRange);
		ref bool treatDiseases = ref m_TreatDiseases;
		reader.Read(out treatDiseases);
		ref bool treatInjuries = ref m_TreatInjuries;
		reader.Read(out treatInjuries);
	}
}
