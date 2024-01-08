using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Methods
{
    class ParameterDiffItem : BaseDiffItem
    {
        private readonly ParameterDefinition oldParameter;
        private readonly ParameterDefinition newParameter;

        public ParameterDiffItem(ParameterDefinition oldParameter, ParameterDefinition newParameter)
            : base(DiffType.Modified)
        {
            this.oldParameter = oldParameter;
            this.newParameter = newParameter;
        }

        protected override string GetXmlInfoString()
        {
            return string.Format("Parameter name changed from {0} to {1}.", this.oldParameter.Name, this.newParameter.Name);
        }

        public override bool IsBreakingChange
        {
            get { return true; }
        }
    }
}
