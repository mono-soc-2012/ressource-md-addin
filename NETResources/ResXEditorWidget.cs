using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
//using MonoDevelop.Ide.Tasks;
using MonoDevelop.NETResources;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.ComponentModel;
//using System.Threading;

namespace MonoDevelop.NETResources {
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ResXEditorWidget : Gtk.Bin {

		static List<ResXEditorWidget> widgets = new List<ResXEditorWidget> (); 

		ListStore store;
		ResourceCatalog catalog;

		public ResourceCatalog Catalog {
			get {
				return catalog;
			}
			set {
				catalog = value;
				UpdateFromCatalog ();
				UpdateProgressBar ();
			}
		}

		public ResXEditorWidget ()
		{
			this.Build (); // in gui designer's partial class

			// Setup AccessorCombo
			// if webapp local = enabled, default no code gen (vs doesnt seem to generate any code in the file, also outputs no code to the file when code gen selected)
			// if webapp global = enabled, default internal (vs doesnt give the no code gen option)
			// if website = disabled, default no code gen
			// if winforms local = enabled, default no code gen (vs warns you if you try to edit resx directly, also outputs no code to the file when code gen selected)
			// if winforms global = enabled, default internal (vs doesnt give the no code gen option)

			// AccessorCombo Onchange - persist option?

			SetupFilterSearchEntry ();
			SetupEntriesTreeView ();
		}

		void SetupFilterSearchEntry ()
		{
			filterSearchEntry.Entry.Text = String.Empty;
			filterSearchEntry.Entry.Changed += delegate {
				UpdateFromCatalog ();
			};

			filterSearchEntry.Ready = true;
			filterSearchEntry.Visible = true;
			filterSearchEntry.ForceFilterButtonVisible = true;
			filterSearchEntry.RequestMenu += delegate {
				filterSearchEntry.Menu = CreateOptionsMenu ();
			};
		}

		void SetupEntriesTreeView ()
		{
			// FIXME: makes use of ContextMenuTreeView subclass to add popup
			entriesScrolledWindow.Remove (entriesTreeView);
			entriesTreeView.Destroy ();
			entriesTreeView = new MonoDevelop.Components.ContextMenuTreeView ();
			entriesTreeView.ShowAll ();
			entriesScrolledWindow.Add (entriesTreeView);
			((MonoDevelop.Components.ContextMenuTreeView)entriesTreeView).DoPopupMenu = ShowPopup;

			store = new ListStore (typeof (StringResourceEntry));

			entriesTreeView.Model = store;
			// setup columns

			entriesTreeView.AppendColumn (GetEntriesTreeViewColumn ("Key", 0, keyDataFunc, keyEditHandler));
			entriesTreeView.AppendColumn (GetEntriesTreeViewColumn ("BaseValue", 1, baseValueDataFunc));
			entriesTreeView.AppendColumn (GetEntriesTreeViewColumn ("Value", 2, valueDataFunc, valueEditHandler));
			entriesTreeView.AppendColumn (GetEntriesTreeViewColumn ("Comment", 3, commentDataFunc, commentEditHandler));
		}
		
		void keyEditHandler (object o, Gtk.EditedArgs args) 
		{
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			sre.Key = args.NewText;
		}

		void valueEditHandler (object o, Gtk.EditedArgs args) 
		{
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			sre.Value = args.NewText;
		}

		void commentEditHandler (object o, Gtk.EditedArgs args) {
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			sre.Comment = args.NewText;
		}

		void keyDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
			          TreeModel tree_model, TreeIter iter) 
		{
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Key;
		}

		void baseValueDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                        TreeModel tree_model, TreeIter iter) 
		{
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.GetBaseString ();
		}
		
		void valueDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                    TreeModel tree_model, TreeIter iter) 
		{
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Value;
		}
		
		void commentDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                      TreeModel tree_model, TreeIter iter) 
		{
			var sre = (StringResourceEntry) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Comment;
		}

		// FIXME: will need to take anon func instead of model Pos
		TreeViewColumn GetEntriesTreeViewColumn (string title, int sortColumnId, TreeCellDataFunc treeCellDataFunc)
		{
			TreeViewColumn column = new TreeViewColumn ();
			column.Expand = true;
			column.SortIndicator = true;
			column.SortColumnId = sortColumnId;
			column.Resizable = true;

			column.Title = GettextCatalog.GetString (title);
			CellRendererText cell = new CellRendererText ();
			cell.Ellipsize = Pango.EllipsizeMode.End;
			column.PackStart (cell, true);
			column.SetCellDataFunc (cell, treeCellDataFunc);
			// column.AddAttribute (cell, "text", modelPos);
			return column;
		}

		// FIXME: will need to take anon func instead of model Pos
		TreeViewColumn GetEntriesTreeViewColumn (string title, int sortColumnId, 
		                                         TreeCellDataFunc treeCellDataFunc,
		                                         EditedHandler editHandler)
		{
			TreeViewColumn column = new TreeViewColumn ();
			column.Expand = true;
			column.SortIndicator = true;
			column.SortColumnId = sortColumnId;
			column.Resizable = true;

			column.Title = GettextCatalog.GetString (title);
			CellRendererText cell = new CellRendererText ();
			cell.Ellipsize = Pango.EllipsizeMode.End;
			cell.Editable = true;
			column.PackStart (cell, true);

			column.SetCellDataFunc (cell, treeCellDataFunc);
			cell.Edited += editHandler;
			return column;
		}

		void ShowPopup (EventButton evt)
		{
			Gtk.Menu contextMenu = CreateContextMenu ();
			if (contextMenu != null)
				GtkWorkarounds.ShowContextMenu (contextMenu, this, evt);
		}

		Gtk.Menu CreateContextMenu ()
		{
			//CatalogEntry entry = SelectedEntry;
			//if (entry == null)
			//	return null;

			Gtk.Menu result = new Gtk.Menu ();
			
			Gtk.MenuItem item = new Gtk.MenuItem (GettextCatalog.GetString ("Delete"));
			//item.Sensitive = entry.References.Length == 0;
			item.Activated += delegate {
				//RemoveEntry (entry);
			};
			item.Show();
			result.Append (item);
			
			return result;
		}

		// called when catalog filtered
		string filter = String.Empty;
		Regex  regex = new Regex (String.Empty);
		void UpdateFromCatalog ()
		{
			var newStore = new ListStore (typeof (StringResourceEntry));

			foreach (var re in Catalog) {
				if (re is StringResourceEntry)
					newStore.AppendValues (re);
			}

			entriesTreeView.Model = store = newStore;
		}

		#region Options
		enum SearchIn {
			Key,
			BaseValue,
			Value,
			Comment,
			All
		}
		
		static bool isCaseSensitive;
		static bool isWholeWordOnly;
		static bool regexSearch;
		static SearchIn searchIn;
		
		static ResXEditorWidget ()
		{
			isCaseSensitive = PropertyService.Get ("NETResourcesAddin.Search.IsCaseSensitive", false);
			isWholeWordOnly = PropertyService.Get ("NETResourcesAddin.Search.IsWholeWordOnly", false);
			regexSearch     = PropertyService.Get ("NETResourcesAddin.Search.RegexSearch", false);
			searchIn        = PropertyService.Get ("NETResourcesAddin.Search.SearchIn", SearchIn.All);
		}
		
		static bool IsCaseSensitive {
			get {
				return isCaseSensitive;
			}
			set {
				PropertyService.Set ("NETResourcesAddin.Search.IsCaseSensitive", value);
				isCaseSensitive = value;
			}
		}
		
		static bool IsWholeWordOnly {
			get {
				return isWholeWordOnly;
			}
			set {
				PropertyService.Set ("NETResourcesAddin.Search.IsWholeWordOnly", value);
				isWholeWordOnly = value;
			}
		}
		
		static bool RegexSearch {
			get {
				return regexSearch;
			}
			set {
				PropertyService.Set ("NETResourcesAddin.Search.RegexSearch", value);
				regexSearch = value;
			}
		}
		
		static SearchIn DoSearchIn {
			get {
				return searchIn;
			}
			set {
				PropertyService.Set ("NETResourcesAddin.Search.SearchIn", value);
				searchIn = value;
			}
		}
		#endregion

		// options menu for filterSearchEntry
		public Menu CreateOptionsMenu ()
		{

			Menu menu = new Menu ();
			
			MenuItem searchInMenu = new MenuItem (GettextCatalog.GetString ("_Search in"));
			Menu sub = new Menu ();
			searchInMenu.Submenu = sub;

			Gtk.RadioMenuItem  key = null, baseValue = null, value =null, comment = null, all = null;
			GLib.SList group = new GLib.SList (IntPtr.Zero);

			key = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Key"));
			group = key.Group;
			key.ButtonPressEvent += delegate { key.Activate (); };
			sub.Append (key);
			
			baseValue = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Base Value"));
			baseValue.ButtonPressEvent += delegate { baseValue.Activate (); };
			group = baseValue.Group;
			sub.Append (baseValue);

			value = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Value"));
			value.ButtonPressEvent += delegate { value.Activate (); };
			group = value.Group;
			sub.Append (value);

			comment = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Comment"));
			comment.ButtonPressEvent += delegate { comment.Activate (); };
			group = comment.Group;
			sub.Append (comment);
			
			all = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_All"));
			all.ButtonPressEvent += delegate { all.Activate (); };
			sub.Append (all);

			switch (DoSearchIn) {
			case SearchIn.All:
				all.Activate ();
				break;
			case SearchIn.BaseValue:
				baseValue.Activate ();
				break;
			case SearchIn.Value:
				value.Activate ();
				break;
			case SearchIn.Key:
				key.Activate ();
				break;
			case SearchIn.Comment:
				comment.Activate ();
				break;
			}
			menu.Append (searchInMenu);
			all.Activated += delegate {
				if (DoSearchIn != SearchIn.All) {
					DoSearchIn = SearchIn.All;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			baseValue.Activated += delegate {
				if (DoSearchIn != SearchIn.BaseValue) {
					DoSearchIn = SearchIn.BaseValue;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			value.Activated += delegate {
				if (DoSearchIn != SearchIn.Value) {
					DoSearchIn = SearchIn.Value;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			key.Activated += delegate {
				if (DoSearchIn != SearchIn.Key) {
					DoSearchIn = SearchIn.Key;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			comment.Activated += delegate {
				if (DoSearchIn != SearchIn.Comment) {
					DoSearchIn = SearchIn.Comment;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			
			Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = RegexSearch;
			regexSearch.ButtonPressEvent += delegate { 
				RegexSearch = !RegexSearch;
				UpdateFromCatalog ();
			};
			menu.Append (regexSearch);
			
			Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.ButtonPressEvent += delegate { 
				IsCaseSensitive = !IsCaseSensitive;
				UpdateFromCatalog ();
			};
			menu.Append (caseSensitive);
			
			Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.Sensitive = !RegexSearch;
			wholeWordsOnly.ButtonPressEvent += delegate {
				IsWholeWordOnly = !IsWholeWordOnly;
				UpdateFromCatalog ();
			};
			menu.Append (wholeWordsOnly);
			menu.ShowAll ();
			return menu;
		}

		void UpdateProgressBar ()
		{
			/*
			int all, untrans, fuzzy, missing, bad;
			catalog.GetStatistics (out all, out fuzzy, out missing, out bad, out untrans);
			double percentage = all > 0 ? ((double)(all - untrans) / all) * 100 : 0.0;
			string barText = String.Format (GettextCatalog.GetString ("{0:#00.00}% Translated"), percentage);
			if (untrans > 0 || fuzzy > 0)
				barText += " (";

			if (untrans > 0) {
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Missing Message", "{0} Missing Messages", untrans), untrans);
			}

			if (fuzzy > 0) {
				if (untrans > 0) {
					barText += ", ";
				}
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Fuzzy Message", "{0} Fuzzy Messages", fuzzy), fuzzy);
			}

			if (untrans > 0 || fuzzy > 0)
				barText += ")";
			
			this.progressbar1.Text = barText;
			percentage = percentage / 100;
			this.progressbar1.Fraction = percentage;
			*/
		}

    }
}

