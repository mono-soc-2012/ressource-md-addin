using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.NETResources {
	public class BinaryOrStringProvider : EntryProvider, ICustomTypeDescriptor {
		BinaryOrStringEntry BinaryOrStringEntry {
			get { return (BinaryOrStringEntry) Entry; }
		}
		internal BinaryOrStringProvider (BinaryOrStringEntry entry)
		{
			Entry = entry;
		}
		
		public string FileName {
			get { return BinaryOrStringEntry.FileName; }
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

		#region ICustomTypeDescriptor implementation
		#region Uses TypeDescriptor methods
		public AttributeCollection GetAttributes ()
		{
			return TypeDescriptor.GetAttributes (this, true);
		}
		public string GetClassName ()
		{
			return TypeDescriptor.GetClassName (this, true);
		}
		public string GetComponentName ()
		{
			return TypeDescriptor.GetComponentName (this, true);
		}
		public TypeConverter GetConverter ()
		{
			return TypeDescriptor.GetConverter (this, true);
		}
		public EventDescriptor GetDefaultEvent ()
		{
			return TypeDescriptor.GetDefaultEvent (this, true);
		}
		public PropertyDescriptor GetDefaultProperty ()
		{
			return TypeDescriptor.GetDefaultProperty (this, true);
		}
		public object GetEditor (Type editorBaseType)
		{
			return TypeDescriptor.GetEditor (this, editorBaseType, true);
		}
		public EventDescriptorCollection GetEvents ()
		{
			return TypeDescriptor.GetEvents (this, true);
		}
		public EventDescriptorCollection GetEvents (Attribute[] arr)
		{
			return TypeDescriptor.GetEvents (this, arr, true);
		}
		#endregion
		public PropertyDescriptorCollection GetProperties ()
		{
			return GetProperties (null);
		}
		public PropertyDescriptorCollection GetProperties (Attribute [] arr)
		{
			/* Once the selected object is loaded into the property grid it does not support showing
			 * or hiding properties in response to other properties changing, properties with IsBrowsable.No
			 * set after first loaded are set to read only mode, and properties that did have IsBrowsable.No
			 * set on load and now do not remain hidden.
			 */ 
 			var props = TypeDescriptor.GetProperties (this, arr, true);
			var propsToUse = new PropertyDescriptorCollection (new PropertyDescriptor [0]);
			
			var roAtt = new Attribute [] { ReadOnlyAttribute.Yes };
			//var hideAtt = new Attribute [] { BrowsableAttribute.No };
			foreach (PropertyDescriptor prop in props) {
				if (prop.Name == "TextFileEncoding") {
					if (EditEncoding)
						propsToUse.Add (prop);
					else
						propsToUse.Add (new CustomProperty (prop, roAtt));
				} else
					propsToUse.Add (prop);
			}
			return propsToUse;
		}
		public object GetPropertyOwner (PropertyDescriptor pd)
		{
			return this;
		}
		#endregion
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

	//FIXME: copied from MonoDevelop.DesignerSupport as private
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

