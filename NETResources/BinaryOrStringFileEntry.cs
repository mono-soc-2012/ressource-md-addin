using System;
using System.Resources;
using System.Text;
using System.Reflection;

namespace MonoDevelop.NETResources {
	// FIXME: share ctor logic with base
	public class BinaryOrStringEntry : OtherFileEntry {

		// hides inherited member to allow typename to be changed between string and byte[] (remembering encoding)
		public string TypeName {
			get {
				return base.TypeName;
			}
			set {
				if (value == TypeName)
					return;
				
				ResXFileRef fileRef;
				
				if (value == typeof (string).AssemblyQualifiedName) { //expects same assembly ver as MD runs under
					if (OldEncoding == null) {
						fileRef = new ResXFileRef (FileRef.FileName, value); 
					} else {
						fileRef = new ResXFileRef (FileRef.FileName, value, OldEncoding);
						OldEncoding = null;
					}
				} else if (value == typeof (byte []).AssemblyQualifiedName) { //expects same assembly ver as MD runs under
					if (TextFileEncoding != null)
						OldEncoding = TextFileEncoding;
					fileRef = new ResXFileRef (FileRef.FileName, value);
				}
				else
					throw new ArgumentException ("TypeName", "Should be String or Byte[] type");
				
				var newNode = new ResXDataNode (node.Name, fileRef);
				newNode.Comment = node.Comment;
				node = newNode;
				MarkOwnerDirty ();
			}
		}
		// hides inherited member to enable writes
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

		public BinaryOrStringEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (_node.FileRef == null)
				throw new ArgumentNullException ("node","FileRef should be set");
			
			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (!nodeTypeName.StartsWith ("System.String, mscorlib") &&
			    !nodeTypeName.StartsWith ("System.Byte[], mscorlib"))
				throw new ArgumentException ("node","Only string or byte[] TypeName allowed");
			
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}
		
		public BinaryOrStringEntry (ResourceCatalog _owner, string _name, 
		                             string _fileName, string _typeName, 
		                             Encoding _encoding, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "Name should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (_fileName == String.Empty)
				throw new ArgumentException ("fileName", "should not be empty");
			if (_typeName == null)
				throw new ArgumentNullException ("typeName");
			if (_typeName == String.Empty)
				throw new ArgumentException ("typename", "should not be empty");
			if (!_typeName.StartsWith ("System.String, mscorlib") &&
			    !_typeName.StartsWith ("System.Byte[], mscorlib"))
				throw new ArgumentException ("typeName","Only string or byte[] type valid");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
						
			IsMeta = isMeta;
			Owner = _owner;
			if (_encoding == null)
				node = new ResXDataNode (_name, new ResXFileRef (_fileName, _typeName));
			else
				node = new ResXDataNode (_name, new ResXFileRef (_fileName, _typeName, _encoding));
			SetRelativePos ();
		}
	}
}

