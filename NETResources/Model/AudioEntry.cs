//
// AudioEntry.cs : Class for audio resources whether they are links to files
// or embedded in the resx
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
using System.Reflection;

namespace MonoDevelop.NETResources {
	public class AudioEntry : PersistenceChangingEntry {
		public AudioEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			
			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (!nodeTypeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				throw new ArgumentException ("node","Invalid resource type");
			
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}
		
		public AudioEntry (ResourceCatalog _owner, string _name, 
		                  string _fileName, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (!(_fileName.EndsWith (".wav")))
				throw new ArgumentException ("fileName", "must point to a .wav file");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			
			IsMeta = isMeta;
			Owner = _owner;
			
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, typeof (MemoryStream).AssemblyQualifiedName)); // same ver as MD
			SetRelativePos ();
		}
		
		#region implemented abstract members of PersistenceChangingEntry
		protected override void SaveFile (string filePath)
		{
			object obj = GetValue ();
			
			if (!(obj is MemoryStream))
				throw new InvalidOperationException ("Retrieved object was not a memorystream");

			using (FileStream fs = new FileStream (filePath, FileMode.Create))
				((MemoryStream) obj).WriteTo (fs);
		}

		public override string GetExtension ()
		{
			return "wav";
		}
		#endregion
	}
}

