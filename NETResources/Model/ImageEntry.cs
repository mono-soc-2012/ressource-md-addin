//
// ImageEntry.cs : Class for a Bitmap resource, whether it is a link to
// a file or embedded in the resx
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
using System.Drawing.Imaging;
using System.IO;

namespace MonoDevelop.NETResources {
	public class ImageEntry : PersistenceChangingEntry, IThumbnailProvider {
		public ImageEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			
			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (!nodeTypeName.StartsWith ("System.Drawing.Bitmap, System.Drawing"))
				throw new ArgumentException ("node","Invalid resource type");
			
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}
		
		public ImageEntry (ResourceCatalog _owner, string _name, 
		                  string _fileName, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			//if (_fileName.EndsWith (".ico"))
			//	throw new ArgumentException ("fileName", "must point to a .ico file");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			
			IsMeta = isMeta;
			Owner = _owner;
			
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, typeof (Bitmap).AssemblyQualifiedName)); // same ver as MD
			SetRelativePos ();
		}

		#region IThumbnailProvider implementation
		Bitmap thumbnail = null;

		//Note: width and height ignored after first call to method
		public Bitmap GetThumbnail (int width, int height)
		{
			if (thumbnail != null)
				return thumbnail;

			var bmp = GetValue () as System.Drawing.Bitmap; //FIXME: error handling?
			if (bmp != null) {
				thumbnail = bmp.GetThumbnailImage (width, height, delegate () {
					return false;}, IntPtr.Zero) as Bitmap; // delegate never called
			}
			return thumbnail;
		}
		#endregion
		
		#region implemented abstract members of PersistenceChangingEntry
		protected override void SaveFile (string filePath)
		{
			object obj = GetValue ();

			if (!(obj is Bitmap))
				throw new InvalidOperationException ("Retrieved object was not a bitmap");
			
			((Bitmap) obj).Save (filePath);
		}

		public override string GetExtension ()
		{
			var img = (Bitmap) GetValue ();
			if (img.RawFormat.Guid == ImageFormat.Bmp.Guid)
				return "bmp";
			else if (img.RawFormat.Guid == ImageFormat.Emf.Guid)
				return "png"; //ms .net saves as png according to docs
			else if (img.RawFormat.Guid == ImageFormat.Exif.Guid)
				return "exif";
			else if (img.RawFormat.Guid == ImageFormat.Gif.Guid)
				return "gif";
			else if (img.RawFormat.Guid == ImageFormat.Jpeg.Guid)
				return "jpeg";
			else if (img.RawFormat.Guid == ImageFormat.MemoryBmp.Guid)
				return "bmp";
			else if (img.RawFormat.Guid == ImageFormat.Png.Guid)
				return "png";
			else if (img.RawFormat.Guid == ImageFormat.Tiff.Guid)
				return "tiff";
			else if (img.RawFormat.Guid == ImageFormat.Wmf.Guid)
				return "png"; //ms .net saves as png according to docs
			else if (img.RawFormat.Guid == ImageFormat.Icon.Guid)
				throw new Exception ("icon should be stored as Icon type");
			else
				throw new Exception ("unknown image format");
		}
		#endregion
	}
}
