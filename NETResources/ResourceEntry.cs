using System;
using System.Resources;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MonoDevelop.Components.PropertyGrid;
using System.ComponentModel.Design;
using System.Collections;

namespace MonoDevelop.NETResources {
	public abstract class ResourceEntry {
		protected ResXDataNode node;

		internal ResXDataNode Node {
			get {	return node;	}
		}
		// to preserve order in resx
		internal Point RelativePos;

		public string Name { 
			get {
				return node.Name;
			} set {
				// if (value != Name)
				// 	if (Parent.BaseCatalog.HasName (Name))
				// 		throw new ArgumentException ("Name In Use")
				// 	else
				if (Name != value) {		
					node.Name = value;
					MarkOwnerDirty ();
				}
			}
		}
		public object GetValue ()
		{ 
			return node.GetValue ((AssemblyName []) null);
		}
		public string Comment { 
			get {
				return node.Comment;
			} set {
				if (Comment != value) {
					node.Comment = value;
					MarkOwnerDirty ();
				}
			}
		}
		public string TypeName {
			get {
				//FIXME: loads assemblies into the MD process
				return node.GetValueTypeName ((AssemblyName []) null);
			}
		}
		/*
		public string TypeFullName {
			get {
				string name = TypeName;
				int pos = name.IndexOf (",");
				return name.Substring (0,pos);
			}
		}
		*/
		internal ResourceCatalog Owner { get; set; }

		// FIXME: should this be a constructor? Note subclasses have different validation checks for node
		protected void SetRelativePos ()
		{
			Point p = node.GetNodePosition (); // X is line, Y is column in resx file
			if (p == Point.Empty) 
				// ie new resource, these only added after existing resources loaded
				RelativePos.X = Owner.MaxRelativeXPos + 1; 
			else
				RelativePos = p;
		}

		public virtual object GetBaseValue ()
		{
			if (Owner.BaseCatalog == null)
				return "No base loaded";
			else if (Owner.BaseCatalog.ContainsName (Name))
			 	return Owner.BaseCatalog[Name].GetValue ();
			else
				return "(Not present)";
		}

		protected void MarkOwnerDirty ()
		{
			if (Owner != null)
				Owner.IsDirty = true;
		}
	}

	public class StringResourceEntry : ResourceEntry, IStringResourceDisplay {
		public string Value {
			get {
				return (string) base.GetValue();
			} set {
				if (Value != value) {
					ResXDataNode newNode = new ResXDataNode (Name, value);
					newNode.Comment = node.Comment;
					node = newNode;
					MarkOwnerDirty ();
				}
			}
		}
		public StringResourceEntry (ResourceCatalog _owner, string _name, string _value, string _comment)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name","Should not be empty");
			if (_value == null)
				throw new ArgumentNullException ("value");
			if (!(_value is String))
				throw new ArgumentException ("value", "Should be string resource");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = new ResXDataNode (_name, _value); //FIXME: perhaps shouldn't create node yet
			node.Comment = _comment;
			SetRelativePos ();
		}

		public StringResourceEntry (ResourceCatalog _owner, ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (!_node.GetValueTypeName (new AssemblyName [0]).StartsWith("System.String,")) //FIXME: assembly list
				throw new ArgumentException ("node","Should be string resource");
			if (_node.FileRef != null)
				throw new ArgumentException ("node", "FileRef should not be set");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		public string GetBaseString ()
		{
			// if (Parent.BaseCatalog.HasName (Name)) {
			object obj = GetBaseValue ();
			if (obj is String)
				return (string) obj;
			else
				return String.Format ("Is a {0} type in base", obj.GetType ().ToString ());
		}
	}

	public class ObjectResourceEntry : ResourceEntry {
		public ObjectResourceEntry (ResourceCatalog _owner, ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (_node.GetValueTypeName ((AssemblyName []) null) == typeof (string).AssemblyQualifiedName)
				throw new ArgumentException ("node", "Should not be string or file resource");
			if (_node.FileRef != null)
				throw new ArgumentException ("node","FileRef should not be set");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		public ObjectResourceEntry (ResourceCatalog _owner, string _name, object _value)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name","Name should not be empty");
			if (_value == null)
				throw new ArgumentNullException ("value");
			if (_value is string)
				throw new ArgumentException ("value","Should not be string resource type");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = new ResXDataNode (_name,_value);
			SetRelativePos ();
		}
	}

	public class FileRefResourceEntry : ResourceEntry, ICustomTypeDescriptor {

		ResXFileRef FileRef {
			get {
				return node.FileRef;
			}
		}
		public string FileName {
			get {
				return FileRef.FileName;
			}
		}

		bool ShowEncoding {
			get {	return FileType == "Text"; }
		}
		bool ShowFileType {
			get {	return FileType == "Text" || FileType == "Binary"; }
		}

		#region ICustomTypeDescriptor implementation
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
		public PropertyDescriptorCollection GetProperties ()
		{
			return GetProperties (null);
		}
		public PropertyDescriptorCollection GetProperties (Attribute [] arr)
		{
			var props = TypeDescriptor.GetProperties (this, arr, true);
			var propsToUse = new PropertyDescriptorCollection (new PropertyDescriptor [0]);
			foreach (PropertyDescriptor prop in props) {
				switch (prop.Name) {
				case "TextFileEncoding":
					if (ShowEncoding)
						propsToUse.Add (prop);
					break;
				case "FileType":
					if (ShowFileType)
						propsToUse.Add (prop);
					break;
				default:
					propsToUse.Add (prop);
					break;
				}
			}
			return propsToUse;
		}
		public object GetPropertyOwner (PropertyDescriptor pd)
		{
			return this;
		}
		#endregion
		[System.ComponentModel.TypeConverter (typeof (EncodingConverter))]
		public Encoding TextFileEncoding {
			get {
				return FileRef.TextFileEncoding;
			} set {
				var newNode = new ResXDataNode (node.Name, new ResXFileRef (node.FileRef.FileName, 
					                                                     node.FileRef.TypeName,
					                                                     value));
				newNode.Comment = node.Comment;
				node = newNode;
				MarkOwnerDirty ();
			}
		}
		[System.ComponentModel.TypeConverter (typeof (FileTypeConverter))]
		public string FileType {
			get { 
				// try to avoid loading assemblies
				if (TypeName.StartsWith ("System.String, mscorlib"))
					return "Text";
				else if (TypeName.StartsWith ("System.Byte[], mscorlib"))
					return "Binary";
				else
					return "n/a";
			}
			set {
				if (FileType == "n/a" || value == FileType)
					return;
				string newType;
				if (value == "Text")
					newType = typeof (string).AssemblyQualifiedName;
				else if (value == "Binary")
					newType = typeof (byte []).AssemblyQualifiedName;
				else
					throw new ArgumentException ("FileType", "Should be Text, Binary or n/a");

				ResXFileRef fileRef;
				if (FileRef.TextFileEncoding == null) 
					fileRef = new ResXFileRef (FileRef.FileName, newType);
				else
					fileRef = new ResXFileRef (FileRef.FileName, newType, FileRef.TextFileEncoding);
				
				var newNode = new ResXDataNode (node.Name, fileRef);
				newNode.Comment = node.Comment;
				node = newNode;
				MarkOwnerDirty ();
			}
		}

		public FileRefResourceEntry (ResourceCatalog _owner, ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (_node.FileRef == null)
				throw new ArgumentNullException ("node","FileRef should be set");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		public FileRefResourceEntry (ResourceCatalog _owner, string _name, string _fileName, string _typeName)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name","Name should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (_typeName == null)
				throw new ArgumentNullException ("typeName");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, _typeName));
			SetRelativePos ();
		}

		public FileRefResourceEntry (ResourceCatalog _owner, string _name, string _fileName, string _typeName, Encoding _encoding)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "Name should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (_typeName == null)
				throw new ArgumentNullException ("typeName");
			if (_encoding == null)
				throw new ArgumentNullException ("encoding");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			Owner = _owner;
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, _typeName, _encoding));
			SetRelativePos ();
		}
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

	[MonoDevelop.Components.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
	class FileTypeConverter : TypeConverter
	{
		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return true;
		}
		
		public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
		{
			var entry = (FileRefResourceEntry) context.Instance;
			if (entry.FileType == "Text" || entry.FileType == "Binary")
				return new StandardValuesCollection (new List <string> { "Text", "Binary" });
			else
				return new StandardValuesCollection (new List <string> ());
		}

		public override bool CanConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType)
		{
			return destinationType == typeof (string);
		}
		
		public override object ConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, 
		                                  object value, System.Type destinationType)
		{
			if (!(value is string))
				base.ConvertTo (context, culture, value, destinationType);
			
			if (destinationType != typeof (string))
				base.ConvertTo (context, culture, value, destinationType);
			
			if (value == null)
				return null;
			
			return value;
		}
		
		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof (string);
		}
		
		public override object ConvertFrom (ITypeDescriptorContext context,
		                                    System.Globalization.CultureInfo culture, object value)
		{
			if (!IsValid (context, value))
				throw new FormatException ("Invalid");

			return value;
		}
		
		public override bool IsValid (ITypeDescriptorContext context, object value)
		{
			string str = value as String;

			if (str == null)
				return false;		

			if (str != "Text" && str != "Binary" && str != "n/a")
				return false;
			else
				return true;
		}

		public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
		{
			return true;
		}
	}
}



		