using System;
using System.Resources;
using System.Drawing;
using System.Reflection;

namespace MonoDevelop.NETResources {
	public abstract class ResourceEntry {
		protected ResXDataNode node;
		
		internal ResXDataNode Node {
			get {	return node;	}
		}
		// to preserve order in resx
		internal Point RelativePos;
		
		public string Name {
			get {
				return node.Name;
			} set {
				if (Name != value) {
					if (Owner.IsUniqueName (Name, value, false)) {
						node.Name = value;
						MarkOwnerDirty ();
					} else
						throw new ArgumentException ("Name","Not unique");
				} 
			}
		}
		public object GetValue ()
		{ 
			return node.GetValue ((AssemblyName []) null);
		}
		public string Comment { 
			get {
				return node.Comment;
			} set {
				if (Comment != value) {
					node.Comment = value;
					MarkOwnerDirty ();
				}
			}
		}
		public string TypeName {
			get {
				//FIXME: possibly loads assemblies into the MD process
				return node.GetValueTypeName ((AssemblyName []) null);
			}
		}
		public bool IsMeta { get; protected set; }
		
		internal ResourceCatalog Owner { get; set; }
		
		// cant be ctor as ctors in subclasses have to create node on occasion
		protected void SetRelativePos ()
		{
			Point p = node.GetNodePosition (); // X is line, Y is column in resx file
			if (p == Point.Empty) 
				// ie new resource, these only added after existing resources loaded
				RelativePos.X = Owner.MaxRelativeXPos + 1; 
			else
				RelativePos = p;
		}

		protected void MarkOwnerDirty ()
		{
			if (Owner != null)
				Owner.IsDirty = true;
		}
    }
}

