//
// IconEntry.cs : Class for a resource of type System.Drawing.Icon whether
// it is a link to a file or embedded in the resx file
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
using System.IO;

namespace MonoDevelop.NETResources {
	public class IconEntry : PersistenceChangingEntry, IThumbnailProvider {
		public IconEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");

			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (!nodeTypeName.StartsWith ("System.Drawing.Icon, System.Drawing"))
				throw new ArgumentException ("node","Invalid resource type");

			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}

		public IconEntry (ResourceCatalog _owner, string _name, 
		                       string _fileName, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (!(_fileName.EndsWith (".ico")))
				throw new ArgumentException ("fileName", "must point to a .ico file");
			if (_owner == null)
				throw new ArgumentNullException ("owner");

			IsMeta = isMeta;
			Owner = _owner;

			node = new ResXDataNode (_name, new ResXFileRef (_fileName, typeof (Icon).AssemblyQualifiedName)); // same ver as MD
			SetRelativePos ();
		}
		#region IThumbnailProvider implementation
		Bitmap thumbnail = null;
		
		// Note: width and height ignored after first call to method
		public Bitmap GetThumbnail (int width, int height)
		{
			if (thumbnail != null)
				return thumbnail;
			try {
				var ico = GetValue () as System.Drawing.Icon;
				if (ico != null) {
					var sizedIco = new System.Drawing.Icon (ico, width, height);
					thumbnail = sizedIco.ToBitmap ();
				}
			} catch {
			}
			return thumbnail;
		}
		#endregion
		#region implemented abstract members of PersistenceChangingEntry
		protected override void SaveFile (string filePath)
		{
			object obj = GetValue ();

			if (!(obj is Icon))
				throw new InvalidOperationException ("Retrieved object was not an icon");

			using (FileStream fs = new FileStream (filePath, FileMode.Create))
					((Icon) obj).Save (fs);
		}

		public override string GetExtension ()
		{
			return "ico";
		}
		#endregion
	}
}

