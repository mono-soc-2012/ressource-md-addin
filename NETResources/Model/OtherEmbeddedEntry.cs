//
// OtherEmbeddedEntry.cs : Class for a resource embedded in a resx file
// not of type String / Bitmap / Icon / Audio
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

