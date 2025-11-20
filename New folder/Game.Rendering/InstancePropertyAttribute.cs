using System;

namespace Game.Rendering;

[AttributeUsage(AttributeTargets.Field)]
public class InstancePropertyAttribute : Attribute
{
	public string ShaderPropertyName { get; protected set; }

	public Type DataType { get; protected set; }

	public BatchFlags RequiredFlags { get; protected set; }

	public int DataIndex { get; protected set; }

	public bool IsBuiltin { get; protected set; }

	public InstancePropertyAttribute(string shaderPropertyName, Type dataType, BatchFlags requiredFlags = (BatchFlags)0, int dataIndex = 0, bool isBuiltin = false)
	{
		ShaderPropertyName = shaderPropertyName;
		DataType = dataType;
		RequiredFlags = requiredFlags;
		DataIndex = dataIndex;
		IsBuiltin = isBuiltin;
	}
}
