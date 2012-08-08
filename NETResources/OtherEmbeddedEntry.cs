using System;
using System.Resources;
using System.Reflection;
using System.Drawing;
using System.IO;

namespace MonoDevelop.NETResources {
	//FIXME: MemoryStream objects currently not permitted as presumed to hold wav data
	public class OtherEmbeddedEntry : ResourceEntry {
		public OtherEmbeddedEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");

			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);

			if (nodeTypeName.StartsWith ("System.String, mscorlib") ||
			    nodeTypeName.StartsWith ("System.Drawing.Bitmap, System.Drawing") ||
			    nodeTypeName.StartsWith ("System.Drawing.Icon, System.Drawing") ||
			    nodeTypeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				throw new ArgumentException ("node", "Invalid resource type");

			if (_node.FileRef != null)
				throw new ArgumentException ("node","FileRef should not be set");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		public OtherEmbeddedEntry (ResourceCatalog _owner, string _name, object _value, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "Name should not be empty");
			if (_value == null)
				throw new ArgumentNullException ("value");
			if (_value is string || _value is Icon || _value is Bitmap || _value is MemoryStream)
				throw new ArgumentException ("value", "Invalid type");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = new ResXDataNode (_name,_value);
			SetRelativePos ();
		}
	}
}

