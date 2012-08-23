//
// BinaryOrStringProvider.cs : Class enabling display of BinaryOrStringEntry
// properties in Property Pad
//
// Author:
// 	Lluis Sanchez Gual <lluis@novell.com>
//  	Gary Barnett (gary.barnett.mono@gmail.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (C) Gary Barnett (2012)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MonoDevelop.NETResources {
	public class BinaryOrStringProvider : EntryProvider {
		BinaryOrStringEntry BinaryOrStringEntry {
			get { return (BinaryOrStringEntry) Entry; }
		}
		internal BinaryOrStringProvider (BinaryOrStringEntry entry)
		{
			Entry = entry;
		}
		
		public string FileName {
			get { return Path.GetFullPath (BinaryOrStringEntry.FileName); }
		}
		
		[System.ComponentModel.TypeConverter (typeof (EncodingConverter))]
		public Encoding TextFileEncoding {
			get {
				return BinaryOrStringEntry.TextFileEncoding;
			} set {
				BinaryOrStringEntry.TextFileEncoding = value;
			}
		}

		public FileRefFileType FileType {
			get { 
				// try to avoid loading assemblies
				if (TypeName.StartsWith ("System.String, mscorlib"))
					return FileRefFileType.Text;
				else if (TypeName.StartsWith ("System.Byte[], mscorlib"))
					return FileRefFileType.Binary;
				else
					throw new Exception ("Invalid type");
			}
			set {
				if (value == FileType)
					return;
				if (value == FileRefFileType.Text)
					BinaryOrStringEntry.TypeName = typeof (string).AssemblyQualifiedName;
				else
					BinaryOrStringEntry.TypeName = typeof (byte []).AssemblyQualifiedName;
			}
		}

		public Persistence Persistence { 
			get { return Persistence.Linked; }
		}
		
		bool EditEncoding {
			get { return (FileType == FileRefFileType.Text); }
		}

		public override PropertyDescriptorCollection GetProperties (Attribute [] arr)
		{
 			var props = TypeDescriptor.GetProperties (this, arr, true);
			var propsToUse = new PropertyDescriptorCollection (new PropertyDescriptor [0]);
			
			var roAtt = new Attribute [] { ReadOnlyAttribute.Yes };
			// FIXME: duplicates code from the base class implementation
			foreach (PropertyDescriptor prop in props) {
				if (prop.Name == "Comment") {
					if (Entry.IsMeta)
						propsToUse.Add (new CustomProperty (prop, roAtt));
					else
						propsToUse.Add (prop);
				} else if (prop.Name == "TextFileEncoding") {
					if (EditEncoding)
						propsToUse.Add (prop);
					else
						propsToUse.Add (new CustomProperty (prop, roAtt));
				} else
					propsToUse.Add (prop);
			}
			return propsToUse;
		}

	}

	public enum FileRefFileType {
		Binary,
		Text
	}
	
	//FIXME: inefficient - awful lot of encoding instantiations
	[MonoDevelop.Components.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
	class EncodingConverter : TypeConverter
	{
		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return true;
		}
		
		public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
		{
			List <Encoding> list = new List<Encoding> () { null };
			list.AddRange (Encoding.GetEncodings ().Select (ei => ei.GetEncoding ()).OrderBy (e=> e.EncodingName).ToList ());
			
			return new StandardValuesCollection (list);
		}
		
		public override bool CanConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType)
		{
			return destinationType == typeof (string);
		}
		
		public override object ConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, 
		                                  object value, System.Type destinationType)
		{
			if (!(value is Encoding))
				base.ConvertTo (context, culture, value, destinationType);
			
			if (destinationType != typeof (string))
				base.ConvertTo (context, culture, value, destinationType);
			
			if (value == null)
				return "";
			
			return ((Encoding) value).EncodingName;
		}
		
		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof (string);
		}
		
		public override object ConvertFrom (ITypeDescriptorContext context,
		                                    System.Globalization.CultureInfo culture, object value)
		{
			if (!IsValid (context, value))
				throw new FormatException ("Invalid encoding");
			
			if (value == null)
				return null;
			
			if ((string) value == String.Empty)
				return null;
			
			return GetEncodingFromName ((string) value);
		}
		
		public override bool IsValid (ITypeDescriptorContext context, object value)
		{
			if (value == null)
				return true;
			
			if (!(value is String))
				return false;
			
			if ((string) value == String.Empty)
				return true;
			
			foreach (EncodingInfo ei in Encoding.GetEncodings() ) {
				if ((string) value == ei.GetEncoding ().EncodingName) //ei.DisplayName not the same
					return true;
			}
			return false;
		}
		
		Encoding GetEncodingFromName (string name)
		{
			foreach (EncodingInfo ei in Encoding.GetEncodings() ) {
				if (name == ei.GetEncoding ().EncodingName) //ei.DisplayName not the same
					return ei.GetEncoding ();
			}
			throw new FormatException ("shouldnt see me");
		}
		
		public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
		{
			return true;
		}
	}

	class CustomProperty: PropertyDescriptor
	{
		PropertyDescriptor prop;
		Attribute[] customAtts;
		
		public CustomProperty (PropertyDescriptor prop, Attribute[] customAtts): base (prop)
		{
			this.prop = prop;
			this.customAtts = customAtts;
		}
		
		public override Type ComponentType {
			get { return prop.ComponentType; }
		}
		
		public override TypeConverter Converter {
			get { return prop.Converter; }
		}
		
		public override bool IsLocalizable {
			get { return prop.IsLocalizable; }
		}
		
		public override bool IsReadOnly {
			get { return true; }
		}
		
		public override Type PropertyType {
			get { return prop.PropertyType; }
		}
		
		public override void AddValueChanged (object component, EventHandler handler)
		{
			prop.AddValueChanged (component, handler);
		}
		
		public override void RemoveValueChanged (object component, EventHandler handler)
		{
			RemoveValueChanged (component, handler);
		}
		
		public override object GetValue (object component)
		{
			return prop.GetValue (component);
		}
		
		public override void SetValue (object component, object value)
		{
			prop.SetValue (component, value);
		}
		
		public override void ResetValue (object component)
		{
			prop.ResetValue (component);
		}
		
		public override bool CanResetValue (object component)
		{
			return prop.CanResetValue (component);
		}
		
		public override bool ShouldSerializeValue (object component)
		{
			return prop.ShouldSerializeValue (component);
		}
		
		public override bool Equals (object o)
		{
			return prop.Equals (o);
		}
		
		public override int GetHashCode ()
		{
			return prop.GetHashCode ();
		}
		
		public override PropertyDescriptorCollection GetChildProperties (object instance, Attribute[] filter)
		{
			return prop.GetChildProperties (instance, filter);
		}
		
		public override object GetEditor (Type editorBaseType)
		{
			return prop.GetEditor (editorBaseType);
		}
		
		protected override Attribute [] AttributeArray {
			get {
				List<Attribute> atts = new List<Attribute> ();
				foreach (Attribute at in prop.Attributes)
					atts.Add (at);
				atts.AddRange (customAtts);
				return atts.ToArray ();
			}
		}
	}
}

