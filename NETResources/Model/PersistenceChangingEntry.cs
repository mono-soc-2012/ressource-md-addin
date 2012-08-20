//
// PersistenceChangingEntry.cs : base class for resources that the editor 
// supports changing between being links to files and being embedded in
// the resx
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
using System.IO;

namespace MonoDevelop.NETResources {
	public abstract class PersistenceChangingEntry :ResourceEntry {
		public Persistence Persistence {
			get {
				if (node.FileRef == null)
					return Persistence.Embedded;
				else
					return Persistence.Linked;
			}
		}

		protected ResXFileRef FileRef {
			get {
				if (Persistence == Persistence.Embedded)
					throw new InvalidOperationException ("Resource is Embedded");
				return node.FileRef;
			}
		}
		public string FileName {
			get {
				if (Persistence == Persistence.Embedded)
					return null;
				return FileRef.FileName;
			}
		}

		protected abstract void SaveFile (string filePath);

		public void ExportToFile (string filePath)
		{
			if (Persistence == Persistence.Linked)
				throw new InvalidOperationException ("Resource is already linked");

			SaveFile (filePath);

			var newFileRef = new ResXFileRef (filePath, TypeName);
			var newNode = new ResXDataNode (node.Name, newFileRef);
			newNode.Comment = node.Comment;
			node = newNode;
		}

		public void EmbedFile ()
		{
			if (Persistence == Persistence.Embedded)
				throw new InvalidOperationException ("Resource is already embedded");

			object obj = GetValue ();

			var newNode = new ResXDataNode (Name,obj);
			newNode.Comment = Comment;
			node = newNode;
		}

		public abstract string GetExtension ();

	}

	public enum Persistence {
		Embedded,
		Linked
	}
}

