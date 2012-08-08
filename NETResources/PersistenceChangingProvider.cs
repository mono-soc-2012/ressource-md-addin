using System;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using System.Drawing;

namespace MonoDevelop.NETResources {
	public class PersistenceChangingProvider : EntryProvider {
		PersistenceChangingEntry PersistenceChangingEntry {
			get { return (PersistenceChangingEntry) Entry; }
		}
		internal PersistenceChangingProvider (PersistenceChangingEntry entry)
		{
			Entry = entry;
		}
		
		public string FileName {
			get { return PersistenceChangingEntry.FileName; }
		}
		
		public Persistence Persistence {
			get { return PersistenceChangingEntry.Persistence; }
			set {
				if (Persistence == value)
					return;
				if (value == Persistence.Embedded)
					Embed ();
				else
					Export ();
			}
		}

		void Embed ()
		{
			try {
				PersistenceChangingEntry.EmbedFile ();
			} catch (FileNotFoundException ex) { 
				Gtk.Application.Invoke ( delegate {
					MessageService.ShowError (GettextCatalog.GetString ("Could not find file"),
					                          ex.FileName);
				});
				return;
				//cant easily get path for DirectoryNotFoundException, showing default message like with other ex
			} catch (Exception ex) {
				Gtk.Application.Invoke ( delegate {
					MessageService.ShowException (ex);
				});
				return;
			}
		}

		void Export ()
		{
			string ext = PersistenceChangingEntry.GetExtension ();
			string fileName = Name + "." + ext;
			string newFile;
			if (Entry.Owner.Project == null)
				newFile = GetFileLocation (fileName);
			else
				newFile = GetFileLocationForProject (fileName, Entry.Owner.Project);
			
			if (newFile == null)
				return;

			PersistenceChangingEntry.ExportToFile (newFile);

			if (Entry.Owner.Project != null) {
				Entry.Owner.Project.AddFile (newFile);
				Entry.Owner.Project.Save (null);
			}
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
}

