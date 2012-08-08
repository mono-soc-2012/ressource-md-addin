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
			if (entry is StringResourceEntry)
				return new EntryProvider (entry);
			else if (entry is FileRefResourceEntry) {
				var file = (FileRefResourceEntry) entry;
				if (IsConvertable (file.TypeName))
					return new EmbeddableFileProvider (file);
				else if (file.TypeName.StartsWith ("System.String, mscorlib") || 
				         file.TypeName.StartsWith ("System.Byte[], mscorlib"))
					return new OtherFileProvider (file);
				// else??? what about other types stored in files???
			} else if (entry is ObjectResourceEntry) {
				var objEntry = (ObjectResourceEntry) entry;
				if (IsConvertable (objEntry.TypeName))
					return new LinkableObjectProvider (objEntry);
				// else OtherEmbeddedProvider
			} 
			//fallback
			return new EntryProvider (entry);
		}
		
		static bool IsConvertable (string typeName)
		{
			if (typeName.StartsWith ("System.Drawing.Icon, System.Drawing") ||
			         typeName.StartsWith ("System.Drawing.Bitmap, System.Drawing") || 
			         typeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				return true;
			else
				return false;
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

