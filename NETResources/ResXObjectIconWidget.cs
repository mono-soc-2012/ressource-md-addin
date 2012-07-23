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
	
	public ResourceCatalog Catalog {
			get {
				return catalog;
			} set {
				catalog = value;
				UpdateFromCatalog ();
			}
		}

	void SetupIconView ()
	{
		
		iconview2.TextColumn = 0;
		iconview2.PixbufColumn = 1;
		iconview2.TooltipColumn = 2;
		//FIXME: what happens if one not available, do the associated res not display?
		//Icons.Add ("File", GetIcon ("gnome-fs-regular"));
		Icons.Add ("Image", GetIcon ("gnome-mime-image"));
		Icons.Add ("Audio", GetIcon ("gnome-mime-audio"));
		Icons.Add ("Text", GetIcon ("gnome-mime-text"));
		Icons.Add ("Icon", GetIcon ("gnome-mime-image-vnd.microsoft.icon"));
		Icons.Add ("Other", GetIcon ("gnome-other"));
		Icons.Add ("Unknown", GetIcon ("gnome-unknown"));

		//iconview2.SetCellDataFunc (new CellRendererPixbuf (), pixbufDataFunc);
		//iconview2.SetCellDataFunc (new CellRendererText (), nameDataFunc);
		
	}
	/*
	void nameDataFunc (CellLayout cell_layout, CellRenderer cell, 
		                     TreeModel tree_model, TreeIter iter)
	{
		var sre = (ResourceEntry) store.GetValue (iter, 0);
		((CellRendererText) cell).Text = sre.Name;
	}

	void pixbufDataFunc (CellLayout cell_layout, CellRenderer cell, 
		                     TreeModel tree_model, TreeIter iter)
	{
		//var sre = (ResourceEntry) store.GetValue (iter, 0);
		((CellRendererPixbuf) cell).Pixbuf = GetIcon ("gnome-fs-regular");
	}
	*/
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
		var newStore = new ListStore (typeof (string), typeof (Gdk.Pixbuf), typeof (string));
		//var newStore = new ListStore (typeof (ResourceEntry));
		Gdk.Pixbuf ico;
		string tooltip;
		foreach (var re in Catalog) {
			if (re is FileRefResourceEntry || re is ObjectResourceEntry) {
				ico = GetIconForType (Type.GetType (re.TypeName));
				
				if (re is FileRefResourceEntry) {
					var fre = (FileRefResourceEntry) re;
					tooltip = String.Format ("Type: {0}\nFile Linked at Compile Time\n{1}\n{2}{3}", 
				                         fre.TypeName, 
				                         fre.FileName,
						         (fre.TextFileEncoding == null) ? "" : "Encoding: " + fre.TextFileEncoding.ToString () + "\n",
				                         (String.IsNullOrEmpty(fre.Comment))? "" : "\nComment: " + fre.Comment);
				} else if (re is ObjectResourceEntry) {
					tooltip = String.Format ("Type: {0}\nObject Embedded in ResX\n{1}", 
				                         re.TypeName,
				                         (String.IsNullOrEmpty(re.Comment))? "" : "\nComment: " + re.Comment);
				}
				newStore.AppendValues (re.Name, ico, tooltip);
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
		iconview2.Model = store = newStore;
	}
    }
}

