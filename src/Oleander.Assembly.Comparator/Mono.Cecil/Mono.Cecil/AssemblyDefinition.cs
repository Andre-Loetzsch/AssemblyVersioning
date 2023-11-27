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
        private Collection<ModuleDefinition> _modules;
        private Collection<CustomAttribute> _customAttributes;
        private Collection<SecurityDeclaration> _securityDeclarations;

        public AssemblyNameDefinition Name { get; set; }

        public string FullName => this.Name != null ? this.Name.FullName : string.Empty;

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

                if (this.MainModule.HasImage)
                {
                    /*Telerik Authorship*/
                    this.MainModule.Read(ref this._modules, this, (_, reader) => reader.ReadModules(this.MainModule.AssemblyResolver));

                    /*Telerik Authorship*/
                    foreach (ModuleDefinition module in this._modules)
                    {
                        if (module != this.MainModule)
                        {
                            module.Assembly = this;
                        }
                    }

                    /*Telerik Authorship*/
                    return this._modules;
                }

                return this._modules = new Collection<ModuleDefinition>(1) { this.MainModule };
            }
        }

        public ModuleDefinition MainModule { get; internal set; }

        public MethodDefinition EntryPoint
        {
            get => this.MainModule.EntryPoint;
            set => this.MainModule.EntryPoint = value;
        }

        /*Telerik Authorship*/
        private bool? _hasCustomAttributes;
        public bool HasCustomAttributes
        {
            get
            {
                if (this._customAttributes != null)
                    return this._customAttributes.Count > 0;

                /*Telerik Authorship*/
                if (this._hasCustomAttributes != null)
                    return this._hasCustomAttributes == true;

                /*Telerik Authorship*/
                return this.GetHasCustomAttributes(ref this._hasCustomAttributes, this.MainModule);
            }
        }

        /*Telerik Authorship*/
        private string _targetFrameworkAttributeValue;
        /*Telerik Authorship*/
        public string TargetFrameworkAttributeValue
        {
            get
            {
                if (this._targetFrameworkAttributeValue == null)
                {
                    this._targetFrameworkAttributeValue = this.GetTargetFrameworkAttributeValue();
                }

                if (this._targetFrameworkAttributeValue == string.Empty)
                {
                    return null;
                }

                return this._targetFrameworkAttributeValue;
            }
        }

        /*Telerik Authorship*/
        /// <summary>
        /// Get the value of assembly's target framework attribute.
        /// </summary>
        /// <returns>Returns string.Empty if the attribute is not present or with invalid value. Otherwise returns it's value.</returns>
        private string GetTargetFrameworkAttributeValue()
        {
            foreach (var customAttr in this.CustomAttributes)
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
            get { return this._customAttributes ?? (this.GetCustomAttributes(ref this._customAttributes, this.MainModule)); }
        }

        /*Telerik Authorship*/
        private bool? _hasSecurityDeclarations;
        public bool HasSecurityDeclarations
        {
            get
            {
                if (this._securityDeclarations != null)
                    return this._securityDeclarations.Count > 0;

                /*Telerik Authorship*/
                if (this._hasSecurityDeclarations != null)
                    return this._hasSecurityDeclarations == true;

                /*Telerik Authorship*/
                return this.GetHasSecurityDeclarations(ref this._hasSecurityDeclarations, this.MainModule);
            }
        }

        public Collection<SecurityDeclaration> SecurityDeclarations
        {
            get { return this._securityDeclarations ?? (this.GetSecurityDeclarations(ref this._securityDeclarations, this.MainModule)); }
        }

        internal AssemblyDefinition()
        {
            /*Telerik Authorship*/
            this._targetFrameworkAttributeValue = null;
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
