using System;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System.Resources;
using System.Text;
using System.IO;

namespace MonoDevelop.NETResources {
	public class EntryProvider {
		protected ResourceEntry Entry;
		protected EntryProvider ()
		{
		}
		protected EntryProvider (ResourceEntry entry)
		{
			Entry = entry;
		}
		public string Name {
			get { return Entry.Name; }
			set {
				if (ValidateNameUI (Entry.Name, value))
					Entry.Name = value;
			}
		}
		public string Comment {
			get { return Entry.Comment; }
			set { Entry.Comment = value; }
		}
		public string TypeName {
			get { return Entry.TypeName; }
		}

		public static EntryProvider GetProvider (ResourceEntry entry)
		{
			if (entry is BinaryOrStringEntry)
				return new BinaryOrStringProvider ((BinaryOrStringEntry) entry);
			else if (entry is OtherFileEntry)
				return new OtherFileProvider ((OtherFileEntry) entry);
			else if (entry is PersistenceChangingEntry)
				return new PersistenceChangingProvider ((PersistenceChangingEntry) entry);
			else //OtherEmbeddedEntry, StringEntry
				return new EntryProvider (entry);
		}

		bool ValidateNameUI (string oldName, string newName)
		{
			//oldName null if new record else an existing one is being renamed
			if (String.IsNullOrEmpty (newName)) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("Resource name not changed."),
					                          GettextCatalog.GetString ("Resource cant have an empty name."));
				});
				return false;
			} else if (!Entry.Owner.IsUniqueName (oldName, newName, false)) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("Resource name not changed."),
					                          GettextCatalog.GetString ("A resource called {0} already exists.", 
					                          newName));
				});
				return false;
			} else
				return true;
		}
	}
}

