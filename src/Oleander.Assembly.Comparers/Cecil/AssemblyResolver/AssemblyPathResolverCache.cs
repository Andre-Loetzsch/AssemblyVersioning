using AssemblyPathName = System.Collections.Generic.KeyValuePair<Oleander.Assembly.Comparers.Cecil.AssemblyResolver.AssemblyStrongNameExtended, string>;

namespace Oleander.Assembly.Comparers.Cecil.AssemblyResolver
{
    public class AssemblyPathResolverCache
    {
        protected List<AssemblyPathName> assemblyPathName;
        protected IDictionary<string, TargetPlatform> assemblyParts;
        protected IDictionary<string, AssemblyName> assemblyNameDefinition;
        protected IDictionary<string, TargetArchitecture> assemblyPathArchitecture;
        protected IClonableCollection<AssemblyStrongNameExtended> assemblyFaildedResolver;

        public AssemblyPathResolverCache()
        {
            this.assemblyPathName = new List<AssemblyPathName>();
            this.assemblyParts = new Dictionary<string, TargetPlatform>();
            this.assemblyNameDefinition = new Dictionary<string, AssemblyName>();
            this.assemblyPathArchitecture = new Dictionary<string, TargetArchitecture>();
            this.assemblyFaildedResolver = new UnresolvedAssembliesCollection();
        }

        public IClonableCollection<AssemblyStrongNameExtended> AssemblyFaildedResolverCache
        {
            get { return this.assemblyFaildedResolver; }
        }

        public IDictionary<string, TargetArchitecture> AssemblyPathArchitecture
        {
            get { return this.assemblyPathArchitecture; }
        }

        public IDictionary<string, AssemblyName> AssemblyNameDefinition
        {
            get { return this.assemblyNameDefinition; }
        }

        public IDictionary<string, TargetPlatform> AssemblyParts
        {
            get { return this.assemblyParts; }
        }

        public List<AssemblyPathName> AssemblyPathName
        {
            get { return this.assemblyPathName; }
        }

        internal void Clear()
        {
            this.assemblyPathName.Clear();
            this.assemblyParts.Clear();
            this.assemblyNameDefinition.Clear();
            this.assemblyPathArchitecture.Clear();
        }
    }
}
