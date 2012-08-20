//
// ResXResourceEntry.cs : base class for a resx resource
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
using System.Drawing;
using System.Reflection;

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
				//FIXME: possibly loads assemblies into the MD process
				return node.GetValueTypeName ((AssemblyName []) null);
			}
		}
		public bool IsMeta { get; protected set; }
		
		internal ResourceCatalog Owner { get; set; }
		
		// cant be ctor as ctors in subclasses have to create node on occasion
		protected void SetRelativePos ()
		{
			Point p = node.GetNodePosition (); // X is line, Y is column in resx file
			if (p == Point.Empty) 
				// ie new resource, these only added after existing resources loaded
				RelativePos.X = Owner.MaxRelativeXPos + 1; 
			else
				RelativePos = p;
		}

		protected void MarkOwnerDirty ()
		{
			if (Owner != null)
				Owner.IsDirty = true;
		}
    }
}

