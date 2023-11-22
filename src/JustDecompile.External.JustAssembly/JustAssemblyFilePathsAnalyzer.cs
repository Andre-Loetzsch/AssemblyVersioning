using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.JustDecompiler.Languages;
using Mono.Cecil;
using JustDecompile.Tools.MSBuildProjectBuilder;
using Telerik.JustDecompiler.Decompiler;

namespace JustDecompile.External.JustAssembly
{
	class JustAssemblyFilePathsAnalyzer : JustDecompile.Tools.MSBuildProjectBuilder.FilePathsServices.DefaultFilePathsAnalyzer
	{
		public JustAssemblyFilePathsAnalyzer(AssemblyDefinition assembly, ILanguage language, bool decompileDangerousResources)
			: base(Tools.MSBuildProjectBuilder.Utilities.GetUserDefinedTypes(assembly, decompileDangerousResources), Tools.MSBuildProjectBuilder.Utilities.GetResources(assembly), language, decompileDangerousResources)
		{
		}

		public int GetMaximumPossibleTargetPathLength()
		{
			return MSBuildProjectBuilder.MaxPathLength - GetMinimumNeededRelativeFilePathLength("");
		}

		protected override int GetProjFileMaximumLength(string projFileName)
		{
			return 0; // project file is not written in JustAssemblyProjectBuilder
		}
	}
}
