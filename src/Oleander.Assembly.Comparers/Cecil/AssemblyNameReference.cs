//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Oleander.Assembly.Comparers.Cecil.Metadata;

/*Telerik Authorship*/

namespace Oleander.Assembly.Comparers.Cecil {

	public class AssemblyNameReference : IMetadataScope {

		string name;
		string culture;
		Version version;
		uint attributes;
		byte [] public_key;
		byte [] public_key_token;
		AssemblyHashAlgorithm hash_algorithm;
		byte [] hash;

		internal MetadataToken token;

		string full_name;

		/*Telerik Authorship*/
		private static AssemblyNameReference fakeCorlibReference;

		/*Telerik Authorship*/
		public static AssemblyNameReference FakeCorlibReference 
		{
			get 
			{
				if (fakeCorlibReference == null)
				{
					Interlocked.CompareExchange(ref fakeCorlibReference, new AssemblyNameReference() { Name = "mscorlib", Version = new Version(0, 0, 0, 0) }, null);
				}

				return fakeCorlibReference;
			}
		}

		public string Name {
			get { return this.name; }
			set {
                this.name = value;
                this.full_name = null;
			}
		}

		public string Culture {
			get { return this.culture; }
			set {
                this.culture = value;
                this.full_name = null;
			}
		}

		public Version Version {
			get { return this.version; }
			set {
                this.version = value;
                this.full_name = null;
			}
		}

		public AssemblyAttributes Attributes {
			get { return (AssemblyAttributes)this.attributes; }
			set { this.attributes = (uint) value; }
		}

		public bool HasPublicKey {
			get { return this.attributes.GetAttributes ((uint) AssemblyAttributes.PublicKey); }
			set { this.attributes = this.attributes.SetAttributes ((uint) AssemblyAttributes.PublicKey, value); }
		}

		public bool IsSideBySideCompatible {
			get { return this.attributes.GetAttributes ((uint) AssemblyAttributes.SideBySideCompatible); }
			set { this.attributes = this.attributes.SetAttributes ((uint) AssemblyAttributes.SideBySideCompatible, value); }
		}

		public bool IsRetargetable {
			get { return this.attributes.GetAttributes ((uint) AssemblyAttributes.Retargetable); }
			set { this.attributes = this.attributes.SetAttributes ((uint) AssemblyAttributes.Retargetable, value); }
		}

		public bool IsWindowsRuntime {
			get { return this.attributes.GetAttributes ((uint) AssemblyAttributes.WindowsRuntime); }
			set { this.attributes = this.attributes.SetAttributes ((uint) AssemblyAttributes.WindowsRuntime, value); }
		}

		public byte [] PublicKey {
			get { return this.public_key ?? Empty<byte>.Array; }
			set {
                this.public_key = value;
                this.HasPublicKey = !this.public_key.IsNullOrEmpty ();
                this.public_key_token = Empty<byte>.Array;
                this.full_name = null;
			}
		}

		public byte [] PublicKeyToken {
			get {
				if (this.public_key_token.IsNullOrEmpty () && !this.public_key.IsNullOrEmpty ()) {
					var hash = this.HashPublicKey ();
					// we need the last 8 bytes in reverse order
					var local_public_key_token = new byte [8];
					Array.Copy (hash, (hash.Length - 8), local_public_key_token, 0, 8);
					Array.Reverse (local_public_key_token, 0, 8);
                    this.public_key_token = local_public_key_token; // publish only once finished (required for thread-safety)
				}
				return this.public_key_token ?? Empty<byte>.Array;
			}
			set {
                this.public_key_token = value;
                this.full_name = null;
			}
		}

        private byte [] HashPublicKey ()
		{
			HashAlgorithm algorithm;

			switch (this.hash_algorithm) {
			case AssemblyHashAlgorithm.Reserved:

				algorithm = MD5.Create ();
				break;

			default:
				// None default to SHA1

				algorithm = SHA1.Create ();
				break;
			}

			using (algorithm)
				return algorithm.ComputeHash (this.public_key);
		}

		/*Telerik Authorship*/
		public string PublicKeyTokenAsString
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				var pk_token = this.PublicKeyToken;
				if (!pk_token.IsNullOrEmpty() && pk_token.Length > 0)
				{
					for (int i = 0; i < pk_token.Length; i++)
					{
						builder.Append(pk_token[i].ToString("x2"));
					}
				}
				else
					builder.Append("null");

				return builder.ToString();
			}
		}

		public virtual MetadataScopeType MetadataScopeType {
			get { return MetadataScopeType.AssemblyNameReference; }
		}

		public string FullName {
			get {
				if (this.full_name != null)
					return this.full_name;

				const string sep = ", ";

				var builder = new StringBuilder ();
				builder.Append (this.name);
				if (this.version != null) {
					builder.Append (sep);
					builder.Append ("Version=");
					builder.Append (this.version.ToString ());
				}
				builder.Append (sep);
				builder.Append ("Culture=");
				builder.Append (string.IsNullOrEmpty (this.culture) ? "neutral" : this.culture);
				builder.Append (sep);
				builder.Append ("PublicKeyToken=");
				/*Telerik Authorship*/
				builder.Append(this.PublicKeyTokenAsString);

				return this.full_name = builder.ToString ();
			}
		}

		public static AssemblyNameReference Parse (string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fullName.Length == 0)
				throw new ArgumentException ("Name can not be empty");

			var name = new AssemblyNameReference ();
			var tokens = fullName.Split (',');
			for (int i = 0; i < tokens.Length; i++) {
				var token = tokens [i].Trim ();

				if (i == 0) {
					name.Name = token;
					continue;
				}

				var parts = token.Split ('=');
				if (parts.Length != 2)
					throw new ArgumentException ("Malformed name");

				switch (parts [0].ToLowerInvariant ()) {
				case "version":
					name.Version = new Version (parts [1]);
					break;
				case "culture":
					name.Culture = parts [1];
					break;
				case "publickeytoken":
					var pk_token = parts [1];
					if (pk_token == "null")
						break;

					name.PublicKeyToken = new byte [pk_token.Length / 2];
					for (int j = 0; j < name.PublicKeyToken.Length; j++)
						name.PublicKeyToken [j] = byte.Parse (pk_token.Substring (j * 2, 2), NumberStyles.HexNumber);

					break;
				}
			}

			return name;
		}

		public AssemblyHashAlgorithm HashAlgorithm {
			get { return this.hash_algorithm; }
			set { this.hash_algorithm = value; }
		}

		public virtual byte [] Hash {
			get { return this.hash; }
			set { this.hash = value; }
		}

		public MetadataToken MetadataToken {
			get { return this.token; }
			set { this.token = value; }
		}

		internal AssemblyNameReference ()
		{
		}

		public AssemblyNameReference (string name, Version version)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			this.name = name;
			this.version = version;
			this.hash_algorithm = AssemblyHashAlgorithm.None;
			this.token = new MetadataToken (TokenType.AssemblyRef);
		}

		public override string ToString ()
		{
			return this.FullName;
		}
	}
}
