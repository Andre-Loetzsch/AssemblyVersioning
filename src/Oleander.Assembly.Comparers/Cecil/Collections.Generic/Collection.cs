//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Collections;

namespace Oleander.Assembly.Comparers.Cecil.Collections.Generic {

	public class Collection<T> : IList<T>, IList {

		internal T [] items;
		internal int size;
		int version;

		public int Count {
			get { return this.size; }
		}

		public T this [int index] {
			get {
				if (index >= this.size)
					throw new ArgumentOutOfRangeException ();

				return this.items [index];
			}
			set {
                this.CheckIndex (index);
				if (index == this.size)
					throw new ArgumentOutOfRangeException ();

                this.OnSet (value, index);

                this.items [index] = value;
			}
		}

		public int Capacity {
			get { return this.items.Length; }
			set {
				if (value < 0 || value < this.size)
					throw new ArgumentOutOfRangeException ();

                this.Resize (value);
			}
		}

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		bool IList.IsFixedSize {
			get { return false; }
		}

		bool IList.IsReadOnly {
			get { return false; }
		}

		object IList.this [int index] {
			get { return this [index]; }
			set {
                this.CheckIndex (index);

				try {
					this [index] = (T) value;
					return;
				} catch (InvalidCastException) {
				} catch (NullReferenceException) {
				}

				throw new ArgumentException ();
			}
		}

		int ICollection.Count {
			get { return this.Count; }
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get { return this; }
		}

		public Collection ()
		{
            this.items = Empty<T>.Array;
		}

		public Collection (int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ();

            this.items = new T [capacity];
		}

		public Collection (ICollection<T> items)
		{
			if (items == null)
				throw new ArgumentNullException ("items");

			this.items = new T [items.Count];
			items.CopyTo (this.items, 0);
			this.size = this.items.Length;
		}

		/*Telerik Authorship*/
		public void AddRange(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
                this.Add(item);
			}
		}

		public void Add (T item)
		{
			if (this.size == this.items.Length) this.Grow (1);

            this.OnAdd (item, this.size);

            this.items [this.size++] = item;
            this.version++;
		}

		public bool Contains (T item)
		{
			return this.IndexOf (item) != -1;
		}

		public int IndexOf (T item)
		{
			return Array.IndexOf (this.items, item, 0, this.size);
		}

		public void Insert (int index, T item)
		{
            this.CheckIndex (index);
			if (this.size == this.items.Length) this.Grow (1);

            this.OnInsert (item, index);

            this.Shift (index, 1);
            this.items [index] = item;
            this.version++;
		}

		public void RemoveAt (int index)
		{
			if (index < 0 || index >= this.size)
				throw new ArgumentOutOfRangeException ();

			var item = this.items [index];

            this.OnRemove (item, index);

            this.Shift (index, -1);
			Array.Clear (this.items, this.size, 1);
            this.version++;
		}

		public bool Remove (T item)
		{
			var index = this.IndexOf (item);
			if (index == -1)
				return false;

            this.OnRemove (item, index);

            this.Shift (index, -1);
			Array.Clear (this.items, this.size, 1);
            this.version++;

			return true;
		}

		public void Clear ()
		{
            this.OnClear ();

			Array.Clear (this.items, 0, this.size);
            this.size = 0;
            this.version++;
		}

		public void CopyTo (T [] array, int arrayIndex)
		{
			Array.Copy (this.items, 0, array, arrayIndex, this.size);
		}

		public T [] ToArray ()
		{
			var array = new T [this.size];
			Array.Copy (this.items, 0, array, 0, this.size);
			return array;
		}

		void CheckIndex (int index)
		{
			if (index < 0 || index > this.size)
				throw new ArgumentOutOfRangeException ();
		}

		void Shift (int start, int delta)
		{
			if (delta < 0)
				start -= delta;

			if (start < this.size)
				Array.Copy (this.items, start, this.items, start + delta, this.size - start);

            this.size += delta;

			if (delta < 0)
				Array.Clear (this.items, this.size, -delta);
		}

		protected virtual void OnAdd (T item, int index)
		{
		}

		protected virtual void OnInsert (T item, int index)
		{
		}

		protected virtual void OnSet (T item, int index)
		{
		}

		protected virtual void OnRemove (T item, int index)
		{
		}

		protected virtual void OnClear ()
		{
		}

		internal virtual void Grow (int desired)
		{
			int new_size = this.size + desired;
			if (new_size <= this.items.Length)
				return;

			const int default_capacity = 4;

			new_size = Math.Max (
				Math.Max (this.items.Length * 2, default_capacity),
				new_size);

            this.Resize (new_size);
		}

		protected void Resize (int new_size)
		{
			if (new_size == this.size)
				return;
			if (new_size < this.size)
				throw new ArgumentOutOfRangeException ();

            this.items = this.items.Resize (new_size);
		}

		int IList.Add (object value)
		{
			try {
                this.Add ((T) value);
				return this.size - 1;
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			throw new ArgumentException ();
		}

		void IList.Clear ()
		{
            this.Clear ();
		}

		bool IList.Contains (object value)
		{
			return ((IList) this).IndexOf (value) > -1;
		}

		int IList.IndexOf (object value)
		{
			try {
				return this.IndexOf ((T) value);
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			return -1;
		}

		void IList.Insert (int index, object value)
		{
            this.CheckIndex (index);

			try {
                this.Insert (index, (T) value);
				return;
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			throw new ArgumentException ();
		}

		void IList.Remove (object value)
		{
			try {
                this.Remove ((T) value);
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}
		}

		void IList.RemoveAt (int index)
		{
            this.RemoveAt (index);
		}

		void ICollection.CopyTo (Array array, int index)
		{
			Array.Copy (this.items, 0, array, index, this.size);
		}

		public Enumerator GetEnumerator ()
		{
			return new Enumerator (this);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return new Enumerator (this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return new Enumerator (this);
		}

		public struct Enumerator : IEnumerator<T>, IDisposable {

			Collection<T> collection;
			T current;

			int next;
			readonly int version;

			public T Current {
				get { return this.current; }
			}

			object IEnumerator.Current {
				get {
                    this.CheckState ();

					if (this.next <= 0)
						throw new InvalidOperationException ();

					return this.current;
				}
			}

			internal Enumerator (Collection<T> collection)
				: this ()
			{
				this.collection = collection;
				this.version = collection.version;
			}

			public bool MoveNext ()
			{
                this.CheckState ();

				if (this.next < 0)
					return false;

				if (this.next < this.collection.size) {
                    this.current = this.collection.items [this.next++];
					return true;
				}

                this.next = -1;
				return false;
			}

			public void Reset ()
			{
                this.CheckState ();

                this.next = 0;
			}

			void CheckState ()
			{
				if (this.collection == null)
					throw new ObjectDisposedException (this.GetType ().FullName);

				if (this.version != this.collection.version)
					throw new InvalidOperationException ();
			}

			public void Dispose ()
			{
                this.collection = null;
			}
		}
	}
}
