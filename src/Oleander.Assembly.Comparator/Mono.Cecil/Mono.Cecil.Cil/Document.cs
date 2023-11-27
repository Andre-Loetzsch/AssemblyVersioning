//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Mono.Cecil.Cil {

	public enum DocumentType {
		Other,
		Text,
	}

	public enum DocumentHashAlgorithm {
		None,
		MD5,
		SHA1,
	}

	public enum DocumentLanguage {
		Other,
		C,
		Cpp,
		CSharp,
		Basic,
		Java,
		Cobol,
		Pascal,
		Cil,
		JScript,
		Smc,
		MCpp,
		FSharp,
	}

	public enum DocumentLanguageVendor {
		Other,
		Microsoft,
	}

	public sealed class Document {

		string url;

		byte type;
		byte hash_algorithm;
		byte language;
		byte language_vendor;

		byte [] hash;

		public string Url {
			get { return this.url; }
			set { this.url = value; }
		}

		public DocumentType Type {
			get { return (DocumentType)this.type; }
			set { this.type = (byte) value; }
		}

		public DocumentHashAlgorithm HashAlgorithm {
			get { return (DocumentHashAlgorithm)this.hash_algorithm; }
			set { this.hash_algorithm = (byte) value; }
		}

		public DocumentLanguage Language {
			get { return (DocumentLanguage)this.language; }
			set { this.language = (byte) value; }
		}

		public DocumentLanguageVendor LanguageVendor {
			get { return (DocumentLanguageVendor)this.language_vendor; }
			set { this.language_vendor = (byte) value; }
		}

		public byte [] Hash {
			get { return this.hash; }
			set { this.hash = value; }
		}

		public Document (string url)
		{
			this.url = url;
			this.hash = Empty<byte>.Array;
		}
	}
}
