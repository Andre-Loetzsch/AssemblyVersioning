//Telerik Authorship

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Oleander.Assembly.Comparers.Cecil
{
	public interface ISecurityDeclarationMemberDefinition : IMemberDefinition
	{
		Collection<SecurityDeclaration> SecurityDeclarations { get; }
	}
}
