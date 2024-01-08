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

	public struct CustomAttributeArgument {

		readonly TypeReference type;
		readonly object value;

		public TypeReference Type {
			get { return this.type; }
		}

		public object Value {
			get { return this.value; }
		}

		public CustomAttributeArgument (TypeReference type, object value)
		{
			Mixin.CheckType (type);
			this.type = type;
			this.value = value;
		}
	}

	public struct CustomAttributeNamedArgument {

		readonly string name;
		readonly CustomAttributeArgument argument;

		public string Name {
			get { return this.name; }
		}

		public CustomAttributeArgument Argument {
			get { return this.argument; }
		}

		public CustomAttributeNamedArgument (string name, CustomAttributeArgument argument)
		{
			Mixin.CheckName (name);
			this.name = name;
			this.argument = argument;
		}
	}

	public interface ICustomAttribute {

		TypeReference AttributeType { get; }

		bool HasFields { get; }
		bool HasProperties { get; }
		Collection<CustomAttributeNamedArgument> Fields { get; }
		Collection<CustomAttributeNamedArgument> Properties { get; }
	}

	public sealed class CustomAttribute : ICustomAttribute {

		readonly internal uint signature;
		internal bool resolved;
		MethodReference constructor;
		byte [] blob;
		internal Collection<CustomAttributeArgument> arguments;
		internal Collection<CustomAttributeNamedArgument> fields;
		internal Collection<CustomAttributeNamedArgument> properties;

		public MethodReference Constructor {
			get { return this.constructor; }
			set { this.constructor = value; }
		}

		public TypeReference AttributeType {
			get { return this.constructor.DeclaringType; }
		}

		public bool IsResolved {
			get { return this.resolved; }
		}

		public bool HasConstructorArguments {
			get {
				/*Telerik Authorship*/
				//Resolve();
				return !this.arguments.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeArgument> ConstructorArguments {
			get {
				/*Telerik Authorship*/
				//Resolve ();
				return this.arguments ?? (this.arguments = new Collection<CustomAttributeArgument> ());
			}
		}

		public bool HasFields {
			get {
				/*Telerik Authorship*/
				//Resolve ();
				return !this.fields.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeNamedArgument> Fields {
			get {
				/*Telerik Authorship*/
				//Resolve ();
				return this.fields ?? (this.fields = new Collection<CustomAttributeNamedArgument> ());
			}
		}

		public bool HasProperties {
			get {
				/*Telerik Authorship*/
				//Resolve ();
				return !this.properties.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeNamedArgument> Properties {
			get {
				/*Telerik Authorship*/
				//Resolve ();
				return this.properties ?? (this.properties = new Collection<CustomAttributeNamedArgument> ());
			}
		}

		internal bool HasImage {
			get { return this.constructor != null && this.constructor.HasImage; }
		}

		internal ModuleDefinition Module {
			get { return this.constructor.Module; }
		}

		internal CustomAttribute (uint signature, MethodReference constructor)
		{
			this.signature = signature;
			this.constructor = constructor;
			this.resolved = false;
		}

		public CustomAttribute (MethodReference constructor)
		{
			this.constructor = constructor;
			this.resolved = true;
		}

		public CustomAttribute (MethodReference constructor, byte [] blob)
		{
			this.constructor = constructor;
			this.resolved = false;
			this.blob = blob;
		}

		public byte [] GetBlob ()
		{
			if (this.blob != null)
				return this.blob;

			if (!this.HasImage)
				throw new NotSupportedException ();

			return this.Module.Read (ref this.blob, this, (attribute, reader) => reader.ReadCustomAttributeBlob (attribute.signature));
		}

		/*Telerik Authorship*/
		public void Resolve ()
		{
			if (this.resolved || !this.HasImage)
				return;

			lock (this.Module.SyncRoot)
			{
				if (this.resolved)
				{
					return;
				}

                this.Module.Read (this, (attribute, reader) => {
					try {
						reader.ReadCustomAttributeSignature (attribute);
                        this.resolved = true;
					} catch (ResolutionException) {
						if (this.arguments != null) this.arguments.Clear ();
						if (this.fields != null) this.fields.Clear ();
						if (this.properties != null) this.properties.Clear ();

                        this.resolved = false;
					}
					return this;
				});
			}
		}
	}

	static partial class Mixin {

		public static void CheckName (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (name.Length == 0)
				throw new ArgumentException ("Empty name");
		}
	}
}
