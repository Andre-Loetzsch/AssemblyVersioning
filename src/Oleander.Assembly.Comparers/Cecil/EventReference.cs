//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Oleander.Assembly.Comparers.Cecil {

	public abstract class EventReference : MemberReference {

		TypeReference event_type;

		public TypeReference EventType {
			get { return this.event_type; }
			set { this.event_type = value; }
		}

		public override string FullName {
			get { return this.event_type.FullName + " " + this.MemberFullName (); }
		}

		protected EventReference (string name, TypeReference eventType)
			: base (name)
		{
            this.event_type = eventType ?? throw new ArgumentNullException (nameof(eventType));
		}

		public abstract EventDefinition Resolve ();
	}
}
