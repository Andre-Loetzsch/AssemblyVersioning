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

namespace Mono.Cecil
{

    public sealed class AssemblyDefinition : ICustomAttributeProvider, ISecurityDeclarationProvider
    {

        AssemblyNameDefinition _name;

        internal ModuleDefinition main_module;
        private Collection<ModuleDefinition> _modules;
        private Collection<CustomAttribute> _customAttributes;
        private Collection<SecurityDeclaration> _securityDeclarations;

        public AssemblyNameDefinition Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        public string FullName
        {
            get { return this._name != null ? this._name.FullName : string.Empty; }
        }

        public MetadataToken MetadataToken
        {
            get { return new MetadataToken(TokenType.Assembly, 1); }
            set { }
        }

        public Collection<ModuleDefinition> Modules
        {
            get
            {
                if (this._modules != null)
                    return this._modules;

                if (this.main_module.HasImage)
                {
                    /*Telerik Authorship*/
                    this.main_module.Read(ref this._modules, this, (_, reader) => reader.ReadModules(this.main_module.AssemblyResolver));

                    /*Telerik Authorship*/
                    foreach (ModuleDefinition module in this._modules)
                    {
                        if (module != this.main_module)
                        {
                            module.Assembly = this;
                        }
                    }

                    /*Telerik Authorship*/
                    return this._modules;
                }

                return this._modules = new Collection<ModuleDefinition>(1) { this.main_module };
            }
        }

        public ModuleDefinition MainModule => this.main_module;

        public MethodDefinition EntryPoint
        {
            get { return this.main_module.EntryPoint; }
            set { this.main_module.EntryPoint = value; }
        }

        /*Telerik Authorship*/
        private bool? hasCustomAttributes;
        public bool HasCustomAttributes
        {
            get
            {
                if (this._customAttributes != null)
                    return this._customAttributes.Count > 0;

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

        public Collection<CustomAttribute> CustomAttributes
        {
            get { return this._customAttributes ?? (this.GetCustomAttributes(ref this._customAttributes, this.main_module)); }
        }

        /*Telerik Authorship*/
        private bool? hasSecurityDeclarations;
        public bool HasSecurityDeclarations
        {
            get
            {
                if (this._securityDeclarations != null)
                    return this._securityDeclarations.Count > 0;

                /*Telerik Authorship*/
                if (this.hasSecurityDeclarations != null)
                    return this.hasSecurityDeclarations == true;

                /*Telerik Authorship*/
                return this.GetHasSecurityDeclarations(ref this.hasSecurityDeclarations, this.main_module);
            }
        }

        public Collection<SecurityDeclaration> SecurityDeclarations
        {
            get { return this._securityDeclarations ?? (this.GetSecurityDeclarations(ref this._securityDeclarations, this.main_module)); }
        }

        internal AssemblyDefinition()
        {
            /*Telerik Authorship*/
            this.targetFrameworkAttributeValue = null;
        }

        public static AssemblyDefinition ReadAssembly(string fileName, ReaderParameters parameters)
        {
            return ReadAssembly(ModuleDefinition.ReadModule(fileName, parameters));
        }

        public static AssemblyDefinition ReadAssembly(Stream stream, ReaderParameters parameters)
        {
            return ReadAssembly(ModuleDefinition.ReadModule(stream, parameters));
        }

        static AssemblyDefinition ReadAssembly(ModuleDefinition module)
        {
            var assembly = module.Assembly;
            if (assembly == null)
                throw new ArgumentException();

            return assembly;
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
