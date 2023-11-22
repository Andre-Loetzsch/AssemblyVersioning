using JustDecompile.EngineInfrastructure;
using Mono.Cecil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Languages;


namespace JustDecompile.EngineInfrastructure
{
	public interface ICodeViewerResults
	{

		string NewLine { get; }

		string GetSourceCode();

	}
}
