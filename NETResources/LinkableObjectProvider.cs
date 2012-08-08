using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.NETResources {
	public class LinkableObjectProvider : EntryProvider {
		internal LinkableObjectProvider (ObjectResourceEntry entry)
		{
			Entry = entry;
		}

		public Persistence Persistence {
			get { return Persistence.Embedded; }
			set {
				if (Persistence == value)
					return;

				object obj = Entry.GetValue ();
				string ext = GetExtension (obj);
				string fileName = Name + "." + ext;
				string newFile;
				if (Entry.Owner.Project == null)
					newFile = GetFileLocation (fileName);
				else
					newFile = GetFileLocationForProject (fileName, Entry.Owner.Project);

				if (newFile == null)
					return;

				WriteOutFile (obj, newFile);

				if (Entry.Owner.Project != null) {
					Entry.Owner.Project.AddFile (newFile);
					Entry.Owner.Project.Save (null);
				}

				var fileEntry = new FileRefResourceEntry (Entry.Owner, Name, newFile, 
				                                          obj.GetType ().AssemblyQualifiedName);
				fileEntry.Comment = Comment;
				Entry.Owner.AddEntry (fileEntry);
				Entry.Owner.RemoveEntry (Entry);
				// object icon widget will need refreshed
			}
		}

		ConvertableType ConvertableType {
			get {
				if (TypeName.StartsWith ("System.Drawing.Bitmap, System.Drawing"))
					return ConvertableType.Bitmap;
				else if (TypeName.StartsWith ("System.Drawing.Icon, System.Drawing"))
					return ConvertableType.Icon;
				else if (TypeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
					return ConvertableType.Audio;
				else
					throw new Exception ("Not convertable");
			}
		}

		void WriteOutFile (object obj, string file)
		{
			using (FileStream fs = new FileStream (file, FileMode.Create)) {
				switch (ConvertableType) {
				case ConvertableType.Icon:
					((Icon) obj).Save (fs);
					return;
				case ConvertableType.Bitmap:
					var bmp = (Bitmap) obj;
					bmp.Save (fs, bmp.RawFormat);
					return;
				case ConvertableType.Audio:
					((MemoryStream) obj).WriteTo  (fs);
					return;
				}
			}
		}

		string GetExtension (object obj)
		{
			if (obj is System.Drawing.Icon)
				return "ico";
			else if (obj is Bitmap) {
				var img = (Bitmap) obj;
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
			} else if (obj is MemoryStream)
				return "wav";
			else
				throw new Exception ("file type could not be detected");
		}
		// return a file location prompting user where necessary. return null to cancel
		string GetFileLocation (string fileName)
		{
			var dialog = new OpenFileDialog (GettextCatalog.GetString ("Select where to save file to"), 
							Gtk.FileChooserAction.Save);
			dialog.InitialFileName = fileName;
			if (dialog.Run ())
				return dialog.SelectedFile.ToString ();
			else
				return null; // cancelled
		}

		string GetFileLocationForProject (string fileName, Project project)
		{
			//project not null, copy file to resources folder, warn on overwrite
			string resFolder = System.IO.Path.Combine (project.BaseDirectory.FullPath, "Resources");
			string newFile = System.IO.Path.Combine (resFolder, fileName);
			
			if (Directory.Exists (resFolder)) {
				if (File.Exists (newFile)) {
					bool overwrite = MessageService.Confirm (GettextCatalog.GetString (
										"Overwrite existing file?"),
										GettextCatalog.GetString (
										"A file named {0} already exists in the Resources folder.", 
										fileName),
										AlertButton.OverwriteFile);
					if (!overwrite)
						return null; // cancelled
				}
			} else {
				Directory.CreateDirectory (resFolder);
				project.AddDirectory ("Resources");
				project.Save (null); //FIXME: ok to save users project here? did this with generate resources wizard too?
			}

			return newFile;
		}
	}

	internal enum ConvertableType {
		Bitmap,
		Icon,
		Audio
	}
}

