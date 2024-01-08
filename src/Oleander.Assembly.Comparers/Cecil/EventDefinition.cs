//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Mono.Cecil {

	public sealed class EventDefinition : EventReference, IMemberDefinition {

		ushort attributes;

		Collection<CustomAttribute> custom_attributes;

		internal MethodDefinition add_method;
		internal MethodDefinition invoke_method;
		internal MethodDefinition remove_method;
		internal Collection<MethodDefinition> other_methods;

        /*Telerik Authorship*/
        public bool IsUnsafe
        {
            get
            {
                return false;
            }
        }
		
		public EventAttributes Attributes {
			get { return (EventAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public MethodDefinition AddMethod {
			get {
				if (this.add_method != null)
					return this.add_method;

                this.InitializeMethods ();
				return this.add_method;
			}
			set { this.add_method = value; }
		}

		public MethodDefinition InvokeMethod {
			get {
				if (this.invoke_method != null)
					return this.invoke_method;

                this.InitializeMethods ();
				return this.invoke_method;
			}
			set { this.invoke_method = value; }
		}

		public MethodDefinition RemoveMethod {
			get {
				if (this.remove_method != null)
					return this.remove_method;

                this.InitializeMethods ();
				return this.remove_method;
			}
			set { this.remove_method = value; }
		}

		public bool HasOtherMethods {
			get {
				if (this.other_methods != null)
					return this.other_methods.Count > 0;

                this.InitializeMethods ();
				return !this.other_methods.IsNullOrEmpty ();
			}
		}

		public Collection<MethodDefinition> OtherMethods {
			get {
				if (this.other_methods != null)
					return this.other_methods;

                this.InitializeMethods ();

				if (this.other_methods != null)
					return this.other_methods;

				return this.other_methods = new Collection<MethodDefinition> ();
			}
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
				return this.GetHasCustomAttributes(ref this.hasCustomAttributes, this.Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this.Module)); }
		}

		#region EventAttributes

		public bool IsSpecialName {
			get { return this.attributes.GetAttributes ((ushort) EventAttributes.SpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) EventAttributes.SpecialName, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return this.attributes.GetAttributes ((ushort) EventAttributes.RTSpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) EventAttributes.RTSpecialName, value); }
		}

		#endregion

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public EventDefinition (string name, EventAttributes attributes, TypeReference eventType)
			: base (name, eventType)
		{
			this.attributes = (ushort) attributes;
			this.token = new MetadataToken (TokenType.Event);
		}

		void InitializeMethods ()
		{
			var module = this.Module;
			if (module == null)
				return;

			lock (module.SyncRoot) {
				if (this.add_method != null
					|| this.invoke_method != null
					|| this.remove_method != null)
					return;

				if (!module.HasImage ())
					return;

				module.Read (this, (@event, reader) => reader.ReadMethods (@event));
			}
		}

		public override EventDefinition Resolve ()
		{
			return this;
		}
	}
}
