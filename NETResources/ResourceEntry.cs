using System;
using System.Resources;
using System.Reflection;

namespace MonoDevelop.NETResources {
	public abstract class ResourceEntry {

		protected ResXDataNode node;

		public string Key { 
			get {
				return node.Name;
			} set {
				// if (value != Key)
				// 	if (Parent.BaseCatalog.HasKey (Key))
				// 		throw new ArgumentException ("Key In Use")
				// 	else
				if (Key != value) {		
					node.Name = value;
					MarkOwnerDirty ();
				}
			}
		}
		public object GetValue ()
		{ 
			return node.GetValue (new AssemblyName[0]); //FIXME: assemblies referenced by current project
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

		public ResourceCatalog owner { get; set; }

		public virtual object GetBaseValue ()
		{
			// if (Parent.BaseCatalog.HasKey (Key))
			// 	return Parent.BaseCatalog[Key].Value;
			// else
				return null; //FIXME: not implemented
		}

		protected void MarkOwnerDirty ()
		{
			if (owner != null)
				owner.IsDirty = true;
		}
	}

	public class StringResourceEntry : ResourceEntry {

		public StringResourceEntry (string _key)
		{
			node = new ResXDataNode (_key, String.Empty);
		}

		public string Value {
			get {
				return (string) base.GetValue();
			} set {
				if (Value != value) {
					node = new ResXDataNode (Key, value);
					MarkOwnerDirty ();
				}
			}
		}

		public StringResourceEntry (ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node cant be null");
			if (!_node.GetValueTypeName (new AssemblyName [0]).StartsWith("System.String,")) //FIXME: assembly list
				throw new ArgumentException ("string expected");
			if (_node.FileRef != null)
				throw new ArgumentException ("fileref not expected");

			node = _node;
		}

		public string GetBaseString ()
		{
			// if (Parent.BaseCatalog.HasKey (Key)) {
			object obj = GetBaseValue ();
			if (obj is String)
				return (string) obj;
			else
				return "Not A String in Base Resource File";// + obj.GetType ().ToString ();
		}
	}

	public class ObjectResourceEntry : ResourceEntry {

		public ObjectResourceEntry (ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node cant be null");
			if (_node.GetValueTypeName (new AssemblyName [0]).StartsWith ("System.String,")) //FIXME: assembly list
				throw new ArgumentException ("should be instantiating a String or FileRef object");
			if (_node.FileRef != null)
				throw new ArgumentException ("fileref not expected");

			node = _node;
		}

		public ObjectResourceEntry (string _key, object _value)
		{
			if (_key == null)
				throw new ArgumentNullException ("key cant be null");
			if (_value == null)
				throw new ArgumentNullException ("value cant be null");
			if (_value is string)
				throw new ArgumentException ("should be a String.... type");

			node = new ResXDataNode (_key,_value);
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

		public string TypeName {
			get {
				return FileRef.TypeName;
			} 
		}

		public FileRefResourceEntry (ResXDataNode _node)
		{
			if (_node == null)
				throw new ArgumentNullException ("node cant be null");
			if (_node.FileRef == null)
				throw new ArgumentException ("fileref expected");

			node = _node;
		}

		public FileRefResourceEntry (string _key, string _fileName, string _typeName)
		{
			if (_key == null)
				throw new ArgumentNullException ("key cant be null");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName cant be null");
			if (_typeName == null)
				throw new ArgumentNullException ("typeName cant be null");

			node = new ResXDataNode (_key, new ResXFileRef (_fileName, _typeName));
		}
	}
}

		