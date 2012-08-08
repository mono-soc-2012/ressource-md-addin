using System;
using System.Resources;
using System.Text;
using System.Reflection;

namespace MonoDevelop.NETResources {
	// FIXME: fileref with MemoryStream type currently not permitted as presumed to hold wav data
	// FIXME: extract common ctor logic
	public class OtherFileEntry : ResourceEntry {
		
		protected ResXFileRef FileRef {
			get {
				return node.FileRef;
			}
		}
		public string FileName {
			get {
				return FileRef.FileName;
			}
		}
		// theoretically a non string file could have have encoding set, so showing it	
		public Encoding TextFileEncoding {
			get {
				return FileRef.TextFileEncoding;
			} 
		}

		public OtherFileEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (_node.FileRef == null)
				throw new ArgumentNullException ("node","FileRef should be set");

			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (nodeTypeName.StartsWith ("System.String, mscorlib") ||
			    nodeTypeName.StartsWith ("System.Drawing.Bitmap, System.Drawing") ||
			    nodeTypeName.StartsWith ("System.Drawing.Icon, System.Drawing") ||
			    nodeTypeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				throw new ArgumentException ("node","Invalid resource type");

			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		protected OtherFileEntry ()
		{
		}

		public OtherFileEntry (ResourceCatalog _owner, string _name, 
		                             string _fileName, string _typeName, 
		                             bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (_fileName == String.Empty)
				throw new ArgumentException ("fileName", "should not be empty");
			if (_typeName == null)
				throw new ArgumentNullException ("typeName");
			if (_typeName == String.Empty)
				throw new ArgumentException ("typename", "should not be empty");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			if (_typeName.StartsWith ("System.String, mscorlib") ||
			    _typeName.StartsWith ("System.Drawing.Bitmap, System.Drawing") ||
			    _typeName.StartsWith ("System.Drawing.Icon, System.Drawing") ||
			    _typeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				throw new ArgumentException ("TypeName","Invalid resource type");

			IsMeta = isMeta;
			Owner = _owner;
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, _typeName));
			SetRelativePos ();
		}
	}
}

