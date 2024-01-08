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

	public sealed class PInvokeInfo {

		ushort attributes;
		string entry_point;
		ModuleReference module;

		public PInvokeAttributes Attributes {
			get { return (PInvokeAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public string EntryPoint {
			get { return this.entry_point; }
			set { this.entry_point = value; }
		}

		public ModuleReference Module {
			get { return this.module; }
			set { this.module = value; }
		}

		#region PInvokeAttributes

		public bool IsNoMangle {
			get { return this.attributes.GetAttributes ((ushort) PInvokeAttributes.NoMangle); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) PInvokeAttributes.NoMangle, value); }
		}

		public bool IsCharSetNotSpec {
			get { return this.attributes.GetMaskedAttributes((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetNotSpec); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetNotSpec, value); }
		}

		public bool IsCharSetAnsi {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetAnsi); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetAnsi, value); }
		}

		public bool IsCharSetUnicode {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetUnicode); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetUnicode, value); }
		}

		public bool IsCharSetAuto {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetAuto); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CharSetMask, (ushort) PInvokeAttributes.CharSetAuto, value); }
		}

		public bool SupportsLastError {
			get { return this.attributes.GetAttributes ((ushort) PInvokeAttributes.SupportsLastError); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) PInvokeAttributes.SupportsLastError, value); }
		}

		public bool IsCallConvWinapi {
			get { return this.attributes.GetMaskedAttributes((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvWinapi); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvWinapi, value); }
		}

		public bool IsCallConvCdecl {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvCdecl); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvCdecl, value); }
		}

		public bool IsCallConvStdCall {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvStdCall); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvStdCall, value); }
		}

		public bool IsCallConvThiscall {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvThiscall); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvThiscall, value); }
		}

		public bool IsCallConvFastcall {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvFastcall); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.CallConvMask, (ushort) PInvokeAttributes.CallConvFastcall, value); }
		}

		public bool IsBestFitEnabled {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.BestFitMask, (ushort) PInvokeAttributes.BestFitEnabled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.BestFitMask, (ushort) PInvokeAttributes.BestFitEnabled, value); }
		}

		public bool IsBestFitDisabled {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.BestFitMask, (ushort) PInvokeAttributes.BestFitDisabled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.BestFitMask, (ushort) PInvokeAttributes.BestFitDisabled, value); }
		}

		public bool IsThrowOnUnmappableCharEnabled {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.ThrowOnUnmappableCharMask, (ushort) PInvokeAttributes.ThrowOnUnmappableCharEnabled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.ThrowOnUnmappableCharMask, (ushort) PInvokeAttributes.ThrowOnUnmappableCharEnabled, value); }
		}

		public bool IsThrowOnUnmappableCharDisabled {
			get { return this.attributes.GetMaskedAttributes ((ushort) PInvokeAttributes.ThrowOnUnmappableCharMask, (ushort) PInvokeAttributes.ThrowOnUnmappableCharDisabled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) PInvokeAttributes.ThrowOnUnmappableCharMask, (ushort) PInvokeAttributes.ThrowOnUnmappableCharDisabled, value); }
		}

		#endregion

		public PInvokeInfo (PInvokeAttributes attributes, string entryPoint, ModuleReference module)
		{
			this.attributes = (ushort) attributes;
			this.entry_point = entryPoint;
			this.module = module;
		}
	}
}
