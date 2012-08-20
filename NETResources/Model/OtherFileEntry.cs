//
// OtherFileEntry.cs : Class for resources that are not Image / Icon / Audio
// types and are links to files
//
// Author:
//  	Gary Barnett (gary.barnett.mono@gmail.com)
// 
// Copyright (C) Gary Barnett (2012)
//
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

