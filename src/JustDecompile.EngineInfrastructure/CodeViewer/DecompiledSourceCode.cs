using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.JustDecompiler.Languages;

namespace JustDecompile.EngineInfrastructure
{
	public class DecompiledSourceCode : ICodeViewerResults
	{
		private readonly string code;

		private readonly IList<Tuple<int, IMemberDefinition>> lineToMemberMapList;

		public DecompiledSourceCode(string code, IList<Tuple<int, IMemberDefinition>> lineToMemberMap)
		{
			this.code = code;

			this.lineToMemberMapList = lineToMemberMap;
		}

		public string NewLine
		{
			get { return "\n"; }
		}

		public IList<Tuple<int, IMemberDefinition>> LineToMemberMapList
		{
			get
			{
				return this.lineToMemberMapList;
			}
		}

		public override string ToString()
		{
			return this.code;
		}

		public string GetSourceCode()
		{
			return this.code;
		}

		public IMemberDefinition GetMemberDefinitionFromLine(int lineNumber)
		{
			var lineToMemberMap = lineToMemberMapList.FirstOrDefault(t => t.Item1 == lineNumber);

			if (lineToMemberMap != null)
			{
				return lineToMemberMap.Item2;
			}
			return null;
		}

		public int GetLineFromMemberDefinition(IMemberDefinition memberDefinition)
		{
			var lineToMemberMap = lineToMemberMapList.FirstOrDefault(t => t.Item2 == memberDefinition);

			if (lineToMemberMap != null)
			{
				return lineToMemberMap.Item1;
			}
			return 0;
		}
	}
}
