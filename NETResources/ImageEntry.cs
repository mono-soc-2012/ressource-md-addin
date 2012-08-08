using System;
using System.Resources;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;

namespace MonoDevelop.NETResources {
	public class ImageEntry : PersistenceChangingEntry {
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

