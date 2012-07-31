using System;
using Gtk;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MonoDevelop.NETResources {
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ResXObjectIconWidget : Gtk.Bin {
		public ResXObjectIconWidget ()
		{
			this.Build ();
			SetupIconView ();
		}

		ResourceCatalog catalog;
		ListStore store;
		Dictionary <string, Gdk.Pixbuf> Icons = new Dictionary <string, Gdk.Pixbuf> ();
		
		internal ResourceCatalog Catalog {
			get {
				return catalog;
			} set {
				catalog = value;
				UpdateFromCatalog ();
			}
		}

		TreePath SelectedPath {
			get {
				if (entriesIV.SelectedItems.Length == 1) 
					return entriesIV.SelectedItems [0];
				return null;
			}
		}
		
		ResourceEntry SelectedEntry {
			get {
				TreePath path = SelectedPath;
				if (path == null)
					return null;

				if (entriesIV.PathIsSelected (path)) {
					TreeIter iter;
					store.GetIter (out iter, path);
					return store.GetValue (iter, 0) as ResourceEntry;
				}
				return null;
			}
		}

		void SetupIconView ()
		{
			//FIXME: what happens if one not available, do the associated res not display?
			//Icons.Add ("File", GetIcon ("gnome-fs-regular"));
			Icons.Add ("Image", GetIcon ("gnome-mime-image"));
			Icons.Add ("Audio", GetIcon ("gnome-mime-audio"));
			Icons.Add ("Text", GetIcon ("gnome-mime-text"));
			Icons.Add ("Icon", GetIcon ("gnome-mime-image-vnd.microsoft.icon"));
			Icons.Add ("Other", GetIcon ("gnome-other"));
			Icons.Add ("Unknown", GetIcon ("gnome-unknown"));

			CellRendererPixbuf pixBuf = new CellRendererPixbuf ();
			CellRendererText text = new CellRendererText ();
			text.Alignment = Pango.Alignment.Center;
			text.Width = entriesIV.ItemWidth;
			text.Ellipsize = Pango.EllipsizeMode.End;

			entriesIV.PackEnd (pixBuf, true);
			entriesIV.PackEnd (text, true);
			entriesIV.SetCellDataFunc (text, nameDataFunc);
			entriesIV.SetCellDataFunc (pixBuf, pixbufDataFunc);
			entriesIV.HasTooltip = true;
			entriesIV.QueryTooltip += tooltipHandler;
		}

		void nameDataFunc (CellLayout cell_layout, CellRenderer cell, 
			                     TreeModel tree_model, TreeIter iter)
		{
			var re = (ResourceEntry) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = re.Name;
		}

		void pixbufDataFunc (CellLayout cell_layout, CellRenderer cell, 
			                     TreeModel tree_model, TreeIter iter)
		{
			var re = (ResourceEntry) store.GetValue (iter, 0);
			((CellRendererPixbuf) cell).Pixbuf = GetIconForType (Type.GetType (re.TypeName));
		}

		void tooltipHandler (object o, QueryTooltipArgs args)
		{
			CellRenderer cell = null;
			TreePath path = null;

			bool isItem = entriesIV.GetItemAtPos (args.X, args.Y, out path, out cell);

			if (!isItem) {
				args.RetVal = false;
				return;
			}

			TreeIter iter = TreeIter.Zero;
			store.GetIter (out iter, path);

			ResourceEntry entry = (ResourceEntry) store.GetValue (iter, 0);

			args.Tooltip.Text = entry.Name;
			args.RetVal = true;
		}

		void tooltipDataFunc (CellLayout cell_layout, CellRenderer cell, 
		                      TreeModel tree_model, TreeIter iter)
		{
			var re = (ResourceEntry) store.GetValue (iter, 0);

			if (re is FileRefResourceEntry) {
				var fre = (FileRefResourceEntry) re;
				((CellRendererText) cell).Text = String.Format ("Type: {0}\nFile Linked at Compile Time\n{1}\n{2}{3}", 
							                         fre.TypeName, 
							                         fre.FileName,
							                         (fre.TextFileEncoding == null) ? "" : "Encoding: " + fre.TextFileEncoding.ToString () + "\n",
							                         (String.IsNullOrEmpty(fre.Comment))? "" : "\nComment: " + fre.Comment);
			} else if (re is ObjectResourceEntry) {
				((CellRendererText) cell).Text = String.Format ("Type: {0}\nObject Embedded in ResX\n{1}", 
							                         re.TypeName,
							                         (String.IsNullOrEmpty(re.Comment))? "" : "\nComment: " + re.Comment);
			}
		}

		Gdk.Pixbuf GetIcon (string name)
		{
			return Gtk.IconTheme.Default.LoadIcon (name, 48, (IconLookupFlags) 0);
		}
		
		Gdk.Pixbuf GetIconForType (Type type)
		{
			if (type == null)
				return Icons ["Unknown"];
			if (typeof (System.Drawing.Icon).IsAssignableFrom (type))
				return Icons ["Icon"];
			if (typeof (Bitmap).IsAssignableFrom (type))
				return Icons ["Image"];
			if (typeof (MemoryStream).IsAssignableFrom (type))
				return Icons ["Audio"];
			if (type == typeof (string))
				return Icons ["Text"];
			else
				return Icons ["Other"];
		}
		
		void UpdateFromCatalog ()
		{
			var newStore = new ListStore (typeof (ResourceEntry));
			//var newStore = new ListStore (typeof (ResourceEntry));
			Gdk.Pixbuf ico;
			string tooltip = null;
			foreach (var re in Catalog) {
				if (re is FileRefResourceEntry || re is ObjectResourceEntry) {
					newStore.AppendValues (re);
					//newStore.AppendValues (re);
				}
			}
			/*
			newStore.DefaultSortFunc = nameSortFunc;
			newStore.SetSortFunc (0, nameSortFunc);
			newStore.SetSortFunc (1, baseValueSortFunc);
			newStore.SetSortFunc (2, valueSortFunc);
			newStore.SetSortFunc (3, commentSortFunc);

			int sortCol;
			SortType sortType;
			store.GetSortColumnId (out sortCol,out sortType);
			newStore.SetSortColumnId (sortCol, sortType);
			newStore.SortColumnChanged += HandleSortColumnChanged;

			*/
			if (store != null)
				store.Dispose ();
			entriesIV.Model = store = newStore;
		}
		
		internal object GetObjectForPropPad ()
		{
			return SelectedEntry;
		}

		internal void RefreshSelected ()
		{
			TreeIter treeIter = TreeIter.Zero;
			store.GetIter (out treeIter, SelectedPath);
			store.EmitRowChanged (SelectedPath, treeIter);
		}
	    }
}

