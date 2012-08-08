using System;
using System.Resources;
using System.IO;
using System.Reflection;

namespace MonoDevelop.NETResources {
	public class AudioEntry : PersistenceChangingEntry {
		public AudioEntry (ResourceCatalog _owner, ResXDataNode _node, bool isMeta)
		{
			if (_node == null)
				throw new ArgumentNullException ("node");
			
			string nodeTypeName = _node.GetValueTypeName ((AssemblyName []) null);
			if (!nodeTypeName.StartsWith ("System.IO.MemoryStream, mscorlib"))
				throw new ArgumentException ("node","Invalid resource type");
			
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			IsMeta = isMeta;
			Owner = _owner;
			node = _node;
			SetRelativePos ();
		}
		
		public AudioEntry (ResourceCatalog _owner, string _name, 
		                  string _fileName, bool isMeta)
		{
			if (_name == null)
				throw new ArgumentNullException ("name");
			if (_name == String.Empty)
				throw new ArgumentException ("name", "should not be empty");
			if (_fileName == null)
				throw new ArgumentNullException ("fileName");
			if (!(_fileName.EndsWith (".wav")))
				throw new ArgumentException ("fileName", "must point to a .wav file");
			if (_owner == null)
				throw new ArgumentNullException ("owner");
			
			IsMeta = isMeta;
			Owner = _owner;
			
			node = new ResXDataNode (_name, new ResXFileRef (_fileName, typeof (MemoryStream).AssemblyQualifiedName)); // same ver as MD
			SetRelativePos ();
		}
		
		#region implemented abstract members of PersistenceChangingEntry
		protected override void SaveFile (string filePath)
		{
			object obj = GetValue ();
			
			if (!(obj is MemoryStream))
				throw new InvalidOperationException ("Retrieved object was not a memorystream");

			using (FileStream fs = new FileStream (filePath, FileMode.Create))
				((MemoryStream) obj).WriteTo (fs);
		}

		public override string GetExtension ()
		{
			return "wav";
		}
		#endregion
	}
}

