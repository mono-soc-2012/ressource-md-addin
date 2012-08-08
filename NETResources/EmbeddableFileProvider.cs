using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.NETResources {
	public class EmbeddableFileProvider : EntryProvider {
		FileRefResourceEntry FileRefEntry {
			get { return (FileRefResourceEntry) Entry; }
		}
		internal EmbeddableFileProvider (FileRefResourceEntry entry)
		{
			Entry = entry;
		}

		public string FileName {
			get { return FileRefEntry.FileName; }
		}

		public Persistence Persistence {
			get { return Persistence.Linked; }
			set {
				if (Persistence == value)
					return;

				object obj;
				try {
					obj = FileRefEntry.GetValue ();
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

				var embedEntry = new ObjectResourceEntry (Entry.Owner, Name, obj);
				embedEntry.Comment = Comment;
				Entry.Owner.AddEntry (embedEntry);
				Entry.Owner.RemoveEntry (Entry);
				// object icon widget will need refreshed
			}
		}
	}

	public enum Persistence {
		Embedded,
		Linked
	}
}

