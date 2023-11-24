//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public sealed class AssemblyDefinition : ICustomAttributeProvider, ISecurityDeclarationProvider {

		AssemblyNameDefinition name;

		internal ModuleDefinition main_module;
		Collection<ModuleDefinition> modules;
		Collection<CustomAttribute> custom_attributes;
		Collection<SecurityDeclaration> security_declarations;

		public AssemblyNameDefinition Name {
			get { return this.name; }
			set { this.name = value; }
		}

		public string FullName {
			get { return this.name != null ? this.name.FullName : string.Empty; }
		}

		public MetadataToken MetadataToken {
			get { return new MetadataToken (TokenType.Assembly, 1); }
			set { }
		}

		public Collection<ModuleDefinition> Modules {
			get {
				if (this.modules != null)
					return this.modules;

				if (this.main_module.HasImage)
				{
					/*Telerik Authorship*/
                    this.main_module.Read(ref this.modules, this, (_, reader) => reader.ReadModules(this.main_module.AssemblyResolver));

					/*Telerik Authorship*/
					foreach (ModuleDefinition module in this.modules)
					{
						if (module != this.main_module)
						{
							module.Assembly = this;
						}
					}
					
					/*Telerik Authorship*/
					return this.modules;
				}

				return this.modules = new Collection<ModuleDefinition> (1) { this.main_module };
			}
		}

		public ModuleDefinition MainModule {
			get { return this.main_module; }
		}

		public MethodDefinition EntryPoint {
			get { return this.main_module.EntryPoint; }
			set { this.main_module.EntryPoint = value; }
		}

		/*Telerik Authorship*/
		private bool? hasCustomAttributes;
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
					return this.custom_attributes.Count > 0;

				/*Telerik Authorship*/
				if (this.hasCustomAttributes != null)
					return this.hasCustomAttributes == true;

				/*Telerik Authorship*/
				return this.GetHasCustomAttributes(ref this.hasCustomAttributes, this.main_module);
			}
		}

        /*Telerik Authorship*/
        private string targetFrameworkAttributeValue;
        /*Telerik Authorship*/
        public string TargetFrameworkAttributeValue
        {
            get
            {
                if (this.targetFrameworkAttributeValue == null)
                {
                    this.targetFrameworkAttributeValue = this.GetTargetFrameworkAttributeValue();
                }

                if (this.targetFrameworkAttributeValue == string.Empty)
                {
                    return null;
                }

                return this.targetFrameworkAttributeValue;
            }
        }

        /*Telerik Authorship*/
        /// <summary>
        /// Get the value of assembly's target framework attribute.
        /// </summary>
        /// <returns>Returns string.Empty if the attribute is not present or with invalid value. Otherwise returns it's value.</returns>
        private string GetTargetFrameworkAttributeValue()
        {
            foreach (CustomAttribute customAttr in this.CustomAttributes)
            {
                if (customAttr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    if (!customAttr.IsResolved)
                    {
                        customAttr.Resolve();
                    }

                    if (customAttr.ConstructorArguments.Count == 0)
                    {
                        // sanity check.
                        return string.Empty;
                    }

                    string versionString = customAttr.ConstructorArguments[0].Value as string;
                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        return string.Empty;
                    }

                    return versionString;
                }
            }

            return string.Empty;
        }

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this.main_module)); }
		}

		/*Telerik Authorship*/
		private bool? hasSecurityDeclarations;
		public bool HasSecurityDeclarations {
			get {
				if (this.security_declarations != null)
					return this.security_declarations.Count > 0;

				/*Telerik Authorship*/
				if (this.hasSecurityDeclarations != null)
					return this.hasSecurityDeclarations == true;

				/*Telerik Authorship*/
				return this.GetHasSecurityDeclarations (ref this.hasSecurityDeclarations, this.main_module);
			}
		}

		public Collection<SecurityDeclaration> SecurityDeclarations {
			get { return this.security_declarations ?? (this.GetSecurityDeclarations (ref this.security_declarations, this.main_module)); }
		}

		internal AssemblyDefinition ()
		{
			/*Telerik Authorship*/
			this.targetFrameworkAttributeValue = null;
		}

#if !READ_ONLY
		public static AssemblyDefinition CreateAssembly (AssemblyNameDefinition assemblyName, string moduleName, ModuleKind kind)
		{
			return CreateAssembly (assemblyName, moduleName, new ModuleParameters { Kind = kind });
		}

		public static AssemblyDefinition CreateAssembly (AssemblyNameDefinition assemblyName, string moduleName, ModuleParameters parameters)
		{
			if (assemblyName == null)
				throw new ArgumentNullException ("assemblyName");
			if (moduleName == null)
				throw new ArgumentNullException ("moduleName");
			Mixin.CheckParameters (parameters);
			if (parameters.Kind == ModuleKind.NetModule)
				throw new ArgumentException ("kind");

			var assembly = ModuleDefinition.CreateModule (moduleName, parameters).Assembly;
			assembly.Name = assemblyName;

			return assembly;
		}
#endif

		/*Telerik Authorship*/
		//public static AssemblyDefinition ReadAssembly (string fileName)
		//{
		//	return ReadAssembly (ModuleDefinition.ReadModule (fileName));
		//}

		public static AssemblyDefinition ReadAssembly (string fileName, ReaderParameters parameters)
		{
			return ReadAssembly (ModuleDefinition.ReadModule (fileName, parameters));
		}

		/*Telerik Authorship*/
		//public static AssemblyDefinition ReadAssembly (Stream stream)
		//{
		//	return ReadAssembly (ModuleDefinition.ReadModule (stream));
		//}

		public static AssemblyDefinition ReadAssembly (Stream stream, ReaderParameters parameters)
		{
			return ReadAssembly (ModuleDefinition.ReadModule (stream, parameters));
		}

		static AssemblyDefinition ReadAssembly (ModuleDefinition module)
		{
			var assembly = module.Assembly;
			if (assembly == null)
				throw new ArgumentException ();

			return assembly;
		}

#if !READ_ONLY
		public void Write (string fileName)
		{
            this.Write (fileName, new WriterParameters ());
		}

		public void Write (Stream stream)
		{
            this.Write (stream, new WriterParameters ());
		}

		public void Write (string fileName, WriterParameters parameters)
		{
            this.main_module.Write (fileName, parameters);
		}

		public void Write (Stream stream, WriterParameters parameters)
		{
            this.main_module.Write (stream, parameters);
		}
#endif

		public override string ToString ()
		{
			return this.FullName;
		}
	}
}
