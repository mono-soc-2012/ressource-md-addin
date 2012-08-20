//
// EntryProvider.cs : Class enabling display and updates of ResourceEntry 
// properties in Property Pad
//
// Author:
//  	Gary Barnett (gary.barnett.mono@gmail.com)
// 
// Copyright (C) Gary Barnett (2012)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System.Resources;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace MonoDevelop.NETResources {
	public class EntryProvider: ICustomTypeDescriptor {
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
			get { 
				if (Entry.IsMeta)
					return "Metadata Resource";
				else
					return Entry.Comment; 
			}
			set { Entry.Comment = value; }
		}
		public string TypeName {
			get { return Entry.TypeName; }
		}

		#region ICustomTypeDescriptor implementation
		#region Uses TypeDescriptor methods
		public AttributeCollection GetAttributes ()
		{
			return TypeDescriptor.GetAttributes (this, true);
		}
		public string GetClassName ()
		{
			return TypeDescriptor.GetClassName (this, true);
		}
		public string GetComponentName ()
		{
			return TypeDescriptor.GetComponentName (this, true);
		}
		public TypeConverter GetConverter ()
		{
			return TypeDescriptor.GetConverter (this, true);
		}
		public EventDescriptor GetDefaultEvent ()
		{
			return TypeDescriptor.GetDefaultEvent (this, true);
		}
		public PropertyDescriptor GetDefaultProperty ()
		{
			return TypeDescriptor.GetDefaultProperty (this, true);
		}
		public object GetEditor (Type editorBaseType)
		{
			return TypeDescriptor.GetEditor (this, editorBaseType, true);
		}
		public EventDescriptorCollection GetEvents ()
		{
			return TypeDescriptor.GetEvents (this, true);
		}
		public EventDescriptorCollection GetEvents (Attribute[] arr)
		{
			return TypeDescriptor.GetEvents (this, arr, true);
		}
#endregion
		public PropertyDescriptorCollection GetProperties ()
		{
			return GetProperties (null);
		}
		virtual public PropertyDescriptorCollection GetProperties (Attribute [] arr)
		{
			/* Once the selected object is loaded into the property grid it does not support showing
			 * or hiding properties in response to other properties changing, properties with IsBrowsable.No
			 * set after first loaded are set to read only mode, and properties that did have IsBrowsable.No
			 * set on load and now do not remain hidden.
			 */ 
			var props = TypeDescriptor.GetProperties (this, arr, true);
			var propsToUse = new PropertyDescriptorCollection (new PropertyDescriptor [0]);
			
			var roAtt = new Attribute [] { ReadOnlyAttribute.Yes };
			//var hideAtt = new Attribute [] { BrowsableAttribute.No };
			foreach (PropertyDescriptor prop in props) {
				if (prop.Name == "Comment") {
					if (Entry.IsMeta)
						propsToUse.Add (new CustomProperty (prop, roAtt));
					else
						propsToUse.Add (prop);
				} else
					propsToUse.Add (prop);
			}
			return propsToUse;
		}
		public object GetPropertyOwner (PropertyDescriptor pd)
		{
			return this;
		}
#endregion
	

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

