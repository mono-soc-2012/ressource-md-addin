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

