using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Oleander.Assembly.Comparers.Cecil
{
	public interface IGenericDefinition
	{
		bool HasGenericParameters { get; }

		Collection<GenericParameter> GenericParameters { get; }

		string Name { get; }
	}
}