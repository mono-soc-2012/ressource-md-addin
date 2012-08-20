//
// StringEntry.cs : Class for embedded string type resources
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

namespace MonoDevelop.NETResources {
	public class StringEntry : ResourceEntry, IStringResourceDisplay {
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
		public StringEntry (ResourceCatalog _owner, string _name, string _value, string _comment, bool isMeta)
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

			IsMeta = isMeta;
			Owner = _owner;
			node = new ResXDataNode (_name, _value); //FIXME: perhaps shouldn't create node yet
			node.Comment = _comment;
			SetRelativePos ();
		}
		
		public StringEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			if (!_node.GetValueTypeName ((AssemblyName []) null).StartsWith ("System.String, mscorlib"))
				throw new ArgumentException ("node","Should be string resource");
			if (_node.FileRef != null)
				throw new ArgumentException ("node", "FileRef should not be set");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}
		
		public string GetBaseString ()
		{
			object obj;

			if (Owner.BaseCatalog == null)
					return "No base loaded";

			if (Owner.BaseCatalog.ContainsName (Name))
				obj = Owner.BaseCatalog [Name].GetValue ();
			else
				return "(Not present)";

			if (obj is String)
				return (string) obj;
			else
				return String.Format ("Is a {0} type in base", obj.GetType ().Name);
		}
	}
}

