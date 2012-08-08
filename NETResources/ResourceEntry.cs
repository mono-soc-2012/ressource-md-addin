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
using MonoDevelop.Ide;
using MonoDevelop.Core;

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
				if (Name != value) {
					if (Owner.IsUniqueName (Name, value, false)) {
						node.Name = value;
						MarkOwnerDirty ();
					} else
						throw new ArgumentException ("Name","Not unique");
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

	public class FileRefResourceEntry : ResourceEntry {

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
		// hides inhertied member to allow typename to be changed between string and byte[] (remembering encoding)
		//FIXME: messy as some instances shouldnt be allowed their type to be changed, as checked for in provider
		public string TypeName {
			get {
				return base.TypeName;
			}
			set {
				if (value == TypeName) // relying on provider to not change typename when inelligible
					return;

				ResXFileRef fileRef;
				
				if (value == typeof (string).AssemblyQualifiedName) {
					if (OldEncoding == null) {
						fileRef = new ResXFileRef (FileRef.FileName, value); 
					} else {
						fileRef = new ResXFileRef (FileRef.FileName, value, OldEncoding);
						OldEncoding = null;
					}
				} else if (value == typeof (byte []).AssemblyQualifiedName) {
					if (TextFileEncoding != null)
						OldEncoding = TextFileEncoding;
					fileRef = new ResXFileRef (FileRef.FileName, value);
				}
				else
					throw new ArgumentException ("TypeName", "Should be Text or Byte");
				
				var newNode = new ResXDataNode (node.Name, fileRef);
				newNode.Comment = node.Comment;
				node = newNode;
				MarkOwnerDirty ();
			}
		}

		public Encoding TextFileEncoding {
			get {
				return FileRef.TextFileEncoding;
			} set {
				if (FileRef.TextFileEncoding == value)
					return;

				var newNode = new ResXDataNode (node.Name, new ResXFileRef (node.FileRef.FileName, 
				                                                            node.FileRef.TypeName,
				                                                            value));
				newNode.Comment = node.Comment;
				node = newNode;
				MarkOwnerDirty ();
			}
		}

		Encoding OldEncoding { get; set; }

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

}



		