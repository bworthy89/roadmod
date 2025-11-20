using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Student : IBufferElementData, IEmptySerializable, IEquatable<Student>
{
	public Entity m_Student;

	public Student(Entity student)
	{
		m_Student = student;
	}

	public bool Equals(Student other)
	{
		return m_Student.Equals(other.m_Student);
	}

	public static implicit operator Entity(Student student)
	{
		return student.m_Student;
	}
}
