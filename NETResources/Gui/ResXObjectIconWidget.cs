//
// ResXObjectIconWidget.cs : Widget displaying ResourceEntry objects from
// catalog as Icons supporting filtering, adding and deleting. Updates 
// supported by means of property pad.
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
using Gtk;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Resources;
using MonoDevelop.Components.Extensions;

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
		
		private ResourceCatalog Catalog {
			get {
				return catalog;
			} set {
				catalog = value;
				UpdateFromCatalog ();
			}
		}

		internal Type FilterType { get; set; }

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
			entriesIV.SelectionChanged += OnEntrySelected;
			entriesIV.ButtonPressEvent += OnButtonPress;

			entriesIV.PopupMenu += ShowPopupMenu;
		}

		internal void SetCatalog<T> (ResourceCatalog _catalog)
		{
			FilterType = typeof (T);
			Catalog = _catalog;
		}

		//FIXME:  Clicks right of centre on cellrendertext do not select object / GetPathAtPos returns null
		void OnButtonPress (object o, ButtonPressEventArgs args)
		{
			// Ignore double-clicks, triple-clicks, non-right clicks
			if (args.Event.Type != Gdk.EventType.ButtonPress || args.Event.Button != 3)
				return;

			TreePath path = entriesIV.GetPathAtPos ((int) args.Event.X, (int) args.Event.Y);

			// avoid menu being displayed over unselected object.
			if (path != null) {
				entriesIV.SelectPath (path);
				ShowPopup (args.Event);
			}
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			ResourceEntry entry = SelectedEntry;
			if (entry == null)
				return;
			
			Gtk.Menu contextMenu = ResXEditorWidget.CreateContextMenu (RemoveEntry, entry);

			if (contextMenu != null)
				GtkWorkarounds.ShowContextMenu (contextMenu, this.entriesIV, evt);
		}

		void ShowPopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup (null);
			args.RetVal = true;
		}
		void OnEntrySelected (object sender, EventArgs args)
		{
			ResXEditorView.SetPropertyPad (SelectedEntry);
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

			string tip = entry.Name;
			//FIXME: if (entry is FileRefResourceEntry)
			//	tip += "\n" + ((FileRefResourceEntry) entry).FileName;

			args.Tooltip.Text = tip;
			args.RetVal = true;
		}
		/*
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

		void RemoveEntry (ResourceEntry entry)
		{
			bool yes = MessageService.AskQuestion (GettextCatalog.GetString ("Do you really want to remove the resource {0}?", entry.Name),
			                                       AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove;
			if (yes) {
				Catalog.RemoveEntry (entry);
				UpdateFromCatalog ();
			}
		}
		
		void UpdateFromCatalog ()
		{
			var newStore = new ListStore (typeof (ResourceEntry));

			foreach (var re in Catalog) {
				if (FilterType.IsAssignableFrom (re.GetType ()))
					newStore.AppendValues (re);
			}

			newStore.DefaultSortFunc = nameSortFunc;
			newStore.SetSortFunc (0, nameSortFunc);
			newStore.SetSortColumnId (0,SortType.Ascending);

			if (store != null)
				store.Dispose ();
			entriesIV.Model = store = newStore;
		}

		int nameSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2) 
		{
			var entry1 = (ResourceEntry) model.GetValue (iter1, 0);
			var entry2 = (ResourceEntry) model.GetValue (iter2, 0);
			return entry1.Name.CompareTo (entry2.Name);
		}
		
		internal object GetObjectForPropPad ()
		{
			return SelectedEntry;
		}
		// keeps view sorted while tracking selected object during renames, was of greater importance 
		// when persistence change implementation deleted and recreated objects from prop pad
		internal void Refresh ()
		{
			Gtk.TreePath oldPath = SelectedPath;
			ResourceEntry oldEntry = SelectedEntry;
			UpdateFromCatalog ();
			// select object again
			entriesIV.SelectPath (oldPath);
			// if object at this position doesnt match old object try to find and select old object
			if (SelectedEntry.Name != oldEntry.Name) { //oldEntry.Name will have current name
				store.Foreach (delegate (TreeModel model, TreePath path, TreeIter iter) {
					object obj = model.GetValue (iter, 0);
					if (((ResourceEntry) obj).Name == oldEntry.Name) {
						entriesIV.SelectPath (path);
						return true; // stops further iterations
					} else
						return false;
				});
			}
		}

		static string [] imageFilters = new string [] { "*.emf","*.exif","*.wmf","*.bmp","*.gif",
								"*.jpeg","*.jpg","*.png","*.tif","*.tiff" };

		SelectFileDialogFilter GetFileFilters ()
		{
			if (FilterType.IsAssignableFrom (typeof (OtherFileEntry)))
				return new SelectFileDialogFilter ("All Files (*.*)", "*.*");
			else if (FilterType.IsAssignableFrom (typeof (AudioEntry)))
				return new SelectFileDialogFilter ("Audio (*.wav)", "*.wav");
			else if (FilterType.IsAssignableFrom (typeof (IconEntry)))
				return new SelectFileDialogFilter ("Icon (*.ico)", "*.ico");
			else if (FilterType.IsAssignableFrom (typeof (ImageEntry))) {
				string name = "Image (" + String.Join (",", imageFilters) + ")";
				return new SelectFileDialogFilter (name, imageFilters);
			} else
				throw new Exception ("no support for adding this type of resource");
		}

		public void AddResource ()
		{
			OpenFileDialog dialog = new OpenFileDialog (GettextCatalog.GetString ("Choose file to add to resources"), 
			                                            Gtk.FileChooserAction.Open);

			dialog.AddFilter (GetFileFilters ());

			if (dialog.Run ())
				ProcessNewResource (Catalog.Project, dialog.SelectedFile);
		}

		void ProcessNewResource (Project project, string fileToAdd)
		{
			string typeName = GetTypeForFile (fileToAdd);
			string resName = System.IO.Path.GetFileNameWithoutExtension (fileToAdd);
			// ensure unique resName
			int i = 0;
			string temp = resName;
			while (Catalog.ContainsName (temp)) {
				temp = resName + ++i;
			}
			resName = temp;

			if (project == null) { // just link to selected file
				AddEntry (resName, typeName, fileToAdd);
				return;
			}
			//project not null, copy file to resources folder, warn on overwrite
			string fileName = System.IO.Path.GetFileName (fileToAdd);
			string resFolder = System.IO.Path.Combine (project.BaseDirectory.FullPath, "Resources");
			string newFile = System.IO.Path.Combine (resFolder, fileName);

			if (Directory.Exists (resFolder)) {
				if (File.Exists (newFile)) {
					bool overwrite = MessageService.Confirm (GettextCatalog.GetString (
										"Overwrite existing file?"),
					                                         GettextCatalog.GetString (
										"A file named {0} already exists in the Resources folder.", 
										fileName),
					                        		AlertButton.OverwriteFile);
					if (!overwrite)
						return;
				}
			} else {
				Directory.CreateDirectory (resFolder);
				project.AddDirectory ("Resources");
			}

			File.Copy (fileToAdd, newFile, true);
			project.AddFile (newFile);
			project.Save (null); //FIXME: ok to save users project here? did this with generate resources wizard too?
			AddEntry (resName, typeName, newFile);
		}
		//FIXME: tidy up?
		void AddEntry (string name, string typeName, string fileToAdd)
		{
			ResourceEntry entry;

			if (typeName == typeof (string).AssemblyQualifiedName || 
			    typeName == typeof (byte []).AssemblyQualifiedName)
				entry = new BinaryOrStringEntry (Catalog, name, fileToAdd, typeName, null, false);
			else if (typeName == typeof (System.Drawing.Icon).AssemblyQualifiedName)
				entry = new IconEntry (Catalog, name, fileToAdd, false);
			else if (typeName == typeof (Bitmap).AssemblyQualifiedName)
				entry = new ImageEntry (Catalog, name, fileToAdd, false);
			else if (typeName == typeof (MemoryStream).AssemblyQualifiedName)
				entry = new AudioEntry (Catalog, name, fileToAdd, false);
			else 
				entry = new OtherFileEntry (Catalog, name, fileToAdd, typeName, false);

			Catalog.AddEntry (entry);
			UpdateFromCatalog ();
		}

		public void DeleteBtnClick ()
		{
			if (SelectedEntry != null)
				RemoveEntry (SelectedEntry);
		}

		//FIXME: will always reference 4.0 assemblies
		string GetTypeForFile (string file)
		{
			string ext = System.IO.Path.GetExtension (file);

			if (!(ext == String.Empty))
				ext = ext.Remove (0,1); //preceeding .

			switch (ext.ToLower ()) {
			case "ico":
				return typeof (System.Drawing.Icon).AssemblyQualifiedName;
			case "emf":
			case "exif":
			case "wmf":
			case "bmp":
			case "gif":
			case "jpeg":
			case "jpg":
			case "png":
			case "tif":
			case "tiff":
				return typeof (System.Drawing.Bitmap).AssemblyQualifiedName;
			case "wav":
				return typeof (MemoryStream).AssemblyQualifiedName;
			case "txt":
				return typeof (string).AssemblyQualifiedName;
			default:
				return typeof (byte []).AssemblyQualifiedName;
			}
		}

	    }
}

