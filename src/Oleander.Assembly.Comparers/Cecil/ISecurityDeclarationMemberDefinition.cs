//Telerik Authorship

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Mono.Cecil
{
	public interface ISecurityDeclarationMemberDefinition : IMemberDefinition
	{
		Collection<SecurityDeclaration> SecurityDeclarations { get; }
	}
}
