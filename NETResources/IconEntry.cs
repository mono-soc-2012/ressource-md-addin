using System;
using System.Resources;
using System.Drawing;
using System.Reflection;
using System.IO;

namespace MonoDevelop.NETResources {
	public class IconEntry : PersistenceChangingEntry {
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

