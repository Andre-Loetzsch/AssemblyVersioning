using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Oleander.Assembly.Comparers.Cecil
{
	/*Telerik Authorship*/
	public class ConstantValue
	{
		public object Value { get; set; }
		public ElementType Type { get; set; }

		public ConstantValue(object value, ElementType type)
		{
			this.Value = value;
			this.Type = type;
		}

		internal ConstantValue()
		{
		}
	}
}
