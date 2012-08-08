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
using MonoDevelop.Components;

namespace MonoDevelop.NETResources {
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ResXEditorWidget : Gtk.Bin {

		static List<ResXEditorWidget> widgets = new List<ResXEditorWidget> (); 

		StringEditorOptions options;

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
				objectIconWidget.Catalog = catalog;
			}
		}

		TreeIter SelectedIter {
			get {
				TreeIter iter;
				if (entriesTV.Selection.GetSelected (out iter)) 
					return iter;
				return Gtk.TreeIter.Zero;
			}
		}

		IStringResourceDisplay SelectedEntry {
			get {
				TreeIter iter = SelectedIter;
				if (iter.Equals (Gtk.TreeIter.Zero))
					return null;
				if (entriesTV.Selection.IterIsSelected (iter))
					return store.GetValue (iter, 0) as IStringResourceDisplay;
				return null;
			}
		}

		public SearchEntry SearchEntry {
			get { return filterSearchEntry; }
		}

		InserterRow Inserter;



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
			options = new StringEditorOptions (this);
			SetupFilterSearchEntry ();
			SetupEntriesTreeView ();
		}

		void SetupFilterSearchEntry ()
		{
			filterSearchEntry.Entry.Text = String.Empty;
			filterSearchEntry.Entry.Changed += delegate {
				options.Refresh ();
			};

			filterSearchEntry.Ready = true;
			filterSearchEntry.Visible = true;
			filterSearchEntry.ForceFilterButtonVisible = true;
			filterSearchEntry.RequestMenu += delegate {
				filterSearchEntry.Menu = options.CreateOptionsMenu ();
			};
		}

		void SetupEntriesTreeView ()
		{
			entriesScrolledWindow.Remove (entriesTV);
			entriesTV.Destroy ();
			entriesTV = new MonoDevelop.Components.ContextMenuTreeView ();
			entriesTV.ShowAll ();
			entriesScrolledWindow.Add (entriesTV);
			((MonoDevelop.Components.ContextMenuTreeView) entriesTV).DoPopupMenu = ShowPopup;

			store = new ListStore (typeof (IStringResourceDisplay));

			entriesTV.Model = store;
			// setup columns

			entriesTV.AppendColumn (GetEntriesTreeViewColumn ("Name", 0, nameDataFunc, nameEditedHandler));
			entriesTV.AppendColumn (GetEntriesTreeViewColumn ("BaseValue", 1, baseValueDataFunc));
			entriesTV.AppendColumn (GetEntriesTreeViewColumn ("Value", 2, valueDataFunc, valueEditedHandler));
			entriesTV.AppendColumn (GetEntriesTreeViewColumn ("Comment", 3, commentDataFunc, commentEditedHandler));

			entriesTV.Selection.Changed += OnEntrySelected;

			store.SetSortColumnId (0, SortType.Ascending);
		}
		
		void nameEditedHandler (object o, Gtk.EditedArgs args) 
		{
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			if (ValidateName (sre.Name, args.NewText, (sre is InserterRow)))
				sre.Name = args.NewText;
			else {
				lastinvalidPath = new TreePath (args.Path);
				GLib.Idle.Add (KeepFocusOnInvalidName);
			}
		}

		void valueEditedHandler (object o, Gtk.EditedArgs args) 
		{
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			sre.Value = args.NewText;
		}

		void commentEditedHandler (object o, Gtk.EditedArgs args) 
		{
			Gtk.TreeIter iter;
			store.GetIter (out iter, new Gtk.TreePath (args.Path));
		 	
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			sre.Comment = args.NewText;
		}

		void nameDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
			          TreeModel tree_model, TreeIter iter) 
		{
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Name;
		}

		void baseValueDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                        TreeModel tree_model, TreeIter iter) 
		{
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.GetBaseString ();
		}
		
		void valueDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                    TreeModel tree_model, TreeIter iter) 
		{
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Value;
		}
		
		void commentDataFunc (TreeViewColumn tree_column, CellRenderer cell, 
		                      TreeModel tree_model, TreeIter iter) 
		{
			var sre = (IStringResourceDisplay) store.GetValue (iter, 0);
			((CellRendererText) cell).Text = sre.Comment;
		}

		TreeViewColumn GetEntriesTreeViewColumn (string title, int sortColumnId, TreeCellDataFunc treeCellDataFunc)
		{
			TreeViewColumn column = new TreeViewColumn ();
			column.Expand = true;
			column.SortIndicator = true;
			column.SortColumnId = sortColumnId;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.Resizable = true;

			column.Title = GettextCatalog.GetString (title);
			CellRendererText cell = new CellRendererText ();
			//cell.Ellipsize = Pango.EllipsizeMode.End;
			cell.WrapMode = Pango.WrapMode.Word;

			column.AddNotification ("width", columnWidthChanged);
			column.PackStart (cell, true);
			column.SetCellDataFunc (cell, treeCellDataFunc);
			return column;
		}

		TreeViewColumn GetEntriesTreeViewColumn (string title, int sortColumnId, 
		                                         TreeCellDataFunc treeCellDataFunc,
		                                         EditedHandler editHandler)
		{
			TreeViewColumn column = new TreeViewColumn ();
			column.Expand = true;
			column.SortIndicator = true;
			column.SortColumnId = sortColumnId;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.Resizable = true;

			column.Title = GettextCatalog.GetString (title);
			CellRendererText cell = new CellRendererText ();
			//cell.Ellipsize = Pango.EllipsizeMode.End;
			cell.Editable = true;
			cell.WrapMode = Pango.WrapMode.Word;

			column.AddNotification ("width", columnWidthChanged);
			column.PackStart (cell, true);

			column.SetCellDataFunc (cell, treeCellDataFunc);
			cell.Edited += editHandler;
			return column;
		}

		void columnWidthChanged (object sender, GLib.NotifyArgs args)
		{
			//FIXME: assumes 1 cell renderer per column and its a ...Text
			var col = (TreeViewColumn) sender;
			var crText = (CellRendererText) col.Cells [0];
			//FIXME: hacky
			if ((crText.WrapWidth > col.Width -15 && crText.WrapWidth < col.Width - 5) || col.Width < 10)
				return;
			crText.WrapWidth = col.Width - 10;
			entriesTV.Model = null; 
			entriesTV.Model = store; // rows need to be regenerated to have correct heights to display wrapped lines
		}

		void ShowPopup (EventButton evt)
		{
			IStringResourceDisplay entry = SelectedEntry;
			if (entry == null)
				return;
			var resEntry = entry as ResourceEntry;
			if (resEntry == null)
				return; // FIXME: if its InserterRow should clear its values?

			Gtk.Menu contextMenu = CreateContextMenu (RemoveEntry, resEntry);
			if (contextMenu != null)
				GtkWorkarounds.ShowContextMenu (contextMenu, this, evt);

		}

		public delegate void RemoveEntryFunc (ResourceEntry entry);

		public static Gtk.Menu CreateContextMenu (RemoveEntryFunc removeEntryFunc, ResourceEntry entry)
		{
			Gtk.Menu result = new Gtk.Menu ();
			Gtk.MenuItem item = new Gtk.MenuItem (GettextCatalog.GetString ("Delete"));
			item.Sensitive = true;
			item.Activated += delegate {
				removeEntryFunc (entry); 
			};
			item.Show();
			result.Append (item);
			
			return result;
		}

		bool ValidateName (string oldName, string newName, bool IsInserter)
		{
			//oldName null if new record else an existing one is being renamed
			if (String.IsNullOrEmpty (newName)) {
				MessageService.ShowError (GettextCatalog.GetString ("Resource cant have an empty name."));
				return false;
			} else if (!Catalog.IsUniqueName (oldName, newName, IsInserter)) {
				MessageService.ShowError (GettextCatalog.GetString ("A resource called {0} already exists.", newName));
				return false;
			} else
				return true;
		}

		#region Funcs for Idle delegate
		//FIXME: using fields for params as cant change method signiture
		TreePath lastinvalidPath;

		bool KeepFocusOnInvalidName ()
		{
			entriesTV.SetCursor (lastinvalidPath, entriesTV.Columns[0], true);
			entriesTV.Selection.SelectPath (lastinvalidPath);
			return false;
		}

		TreePath rowSelect;

		bool ReturnToRowSelect ()
		{

			entriesTV.Selection.SelectPath (rowSelect);
			entriesTV.ScrollToCell (rowSelect, entriesTV.Columns [0], true, 0, 0) ;
			return false;
		}
		#endregion
		bool inserterWasSelected = false;
		//handle InserterRow FIXME: wont be saved if user moves focus out of editor then saves
		//FIXME: should save with emty string / comment if name changed from default
		void OnEntrySelected (object sender, EventArgs args)
		{			
			IStringResourceDisplay entry = SelectedEntry;
			if (entry is InserterRow) {
				inserterWasSelected = true;
				Inserter.Selected ();
			} else if (inserterWasSelected) {
				inserterWasSelected = false;
				Inserter.Deselected ();
			}

			ResXEditorView.SetPropertyPad (entry);
		}

		void AddEntry (StringEntry newEntry)
		{
			createdAfterSort.Add (newEntry);
			TreePath currentPath = null;
			TreePath [] paths = entriesTV.Selection.GetSelectedRows ();
			if (paths != null)
				currentPath = paths [0];
			//UpdateFromCatalog ();  
			TreeIter tempIter = SelectedIter; //FIXME: by avoiding Update From Catalog, no scroll problem
			store.Remove (ref tempIter);
			store.AppendValues (newEntry);
			store.AppendValues (Inserter);
			rowSelect = currentPath;
			if (currentPath != null)
				GLib.Idle.Add (ReturnToRowSelect); // causes scroll downward revealing next blank entry
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

		string filter = String.Empty;
		Regex  regex = new Regex (String.Empty);
		// called on load and when catalog filtered
		public void UpdateFromCatalog ()
		{
			var newStore = new ListStore (typeof (IStringResourceDisplay));
			int total = 0, found = 0;

			foreach (var re in Catalog) {
				if (re is StringEntry) {
					if (options.ShouldFilter ((StringEntry) re)) {
						total++;
					} else {
						newStore.AppendValues (re);
						found++;
					}
				}
			}

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

			//FIXME: 
			if (Inserter == null)
				Inserter = new InserterRow (catalog, AddEntry);
			else if (Inserter.catalog != catalog)
				throw new Exception ("catalog didnt match"); // (?reinitialise)

			newStore.AppendValues (Inserter);
			store.Dispose ();
			entriesTV.Model = store = newStore;
			// FIXME: ??
			//IdeApp.Workbench.StatusBar.ShowMessage (string.Format (GettextCatalog.GetPluralString ("{0} string resource out of {1} match filter.", "{0} string resources out of {1} match filter.", found, total), found,total));
		}

		#region sorting
		//FIXME: store no longer refreshed / sorted after addentry so no need for createdAfterSort feature?
		void HandleSortColumnChanged (object sender, EventArgs e)
		{
			createdAfterSort.Clear ();
		}

		List <StringEntry> createdAfterSort = new List <StringEntry> (); // FIXME: shouldnt accept InserterRow

		int nameSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2) 
		{
			IStringResourceDisplay entry1 = (IStringResourceDisplay) model.GetValue (iter1, 0);
			IStringResourceDisplay entry2 = (IStringResourceDisplay) model.GetValue (iter2, 0);
			int c;
			SortType sortType;
			((TreeSortable) model).GetSortColumnId (out c, out sortType);

			if (entry2 is InserterRow || createdAfterSort.Contains ((StringEntry) entry2))
				return (sortType == SortType.Ascending) ? -1 : 1;
			else
				return entry1.Name.CompareTo (entry2.Name);

		}

		int baseValueSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2) 
		{
			IStringResourceDisplay entry1 = (IStringResourceDisplay) model.GetValue (iter1, 0);
			IStringResourceDisplay entry2 = (IStringResourceDisplay) model.GetValue (iter2, 0);
			int c;
			SortType sortType;
			((TreeSortable) model).GetSortColumnId (out c, out sortType);

			if (entry2 is InserterRow || createdAfterSort.Contains ((StringEntry) entry2))
				return (sortType == SortType.Ascending) ? -1 : 1;
			else
				return entry1.GetBaseString ().CompareTo (entry2.GetBaseString ());
		}

		int valueSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2) 
		{
			IStringResourceDisplay entry1 = (IStringResourceDisplay) model.GetValue (iter1, 0);
			IStringResourceDisplay entry2 = (IStringResourceDisplay) model.GetValue (iter2, 0);
			int c;
			SortType sortType;
			((TreeSortable) model).GetSortColumnId (out c, out sortType);

			if (entry2 is InserterRow || createdAfterSort.Contains ((StringEntry) entry2))
				return (sortType == SortType.Ascending) ? -1 : 1;
			else
				return entry1.Value.CompareTo (entry2.Value);
		}	

		int commentSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2) 
		{
			IStringResourceDisplay entry1 = (IStringResourceDisplay) model.GetValue (iter1, 0);
			IStringResourceDisplay entry2 = (IStringResourceDisplay) model.GetValue (iter2, 0);
			int c;
			SortType sortType;
			((TreeSortable) model).GetSortColumnId (out c, out sortType);

			if (entry2 is InserterRow || createdAfterSort.Contains ((StringEntry) entry2))
				return (sortType == SortType.Ascending) ? -1 : 1;
			else
				return entry1.Comment.CompareTo (entry2.Comment);
		}

		#endregion

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

				
		protected void OnStringResourcesActivated (object sender, EventArgs e)
		{
			pagesNotebook.CurrentPage = 0;
		}
		protected void OnOtherResourcesActivated (object sender, EventArgs e)
		{
			pagesNotebook.CurrentPage = 1;
			objectIconWidget.Catalog = catalog;

			MonoDevelop.Components.PropertyGrid.PropertyGrid grid = new MonoDevelop.Components.PropertyGrid.PropertyGrid ();

			grid.Refresh();

		}

		internal object GetObjectForPropPad ()
		{
			switch (pagesNotebook.CurrentPage) {
			case 0:
				return SelectedEntry;
			case 1:
				return objectIconWidget.GetObjectForPropPad ();
			default:
				return null; // property pad may call before editor finished loading
			}
		}

		internal void OnPropertyPadChanged ()
		{
			switch (pagesNotebook.CurrentPage) {
			case 0:
				// careful, sometimes property pad doesnt update with treeview selected item
				TreePath treePath = store.GetPath (SelectedIter);
				store.EmitRowChanged (treePath, SelectedIter);
				break;
			case 1:
				//refresh all to account for persistence changes which remove and add objects
				objectIconWidget.Refresh ();
				break;
			}
		}

    }
	interface IStringResourceDisplay {
		string Name {get; set;}
		string Value {get; set;} 
		string Comment {get; set;}
		string GetBaseString ();
	}
	//FIXME: ResXEditorWidget will probably become StringEditorWidget
	public class StringEditorOptions {
		static bool isCaseSensitive;
		static bool isWholeWordOnly;
		static bool regexSearch;
		static SearchIn searchIn;

		ResXEditorWidget editor;
		string filter = "";
		Regex  regex = new Regex ("");

		enum SearchIn {
			Name,
			BaseValue,
			Value,
			Comment,
			All
		}

		static StringEditorOptions ()
		{
			isCaseSensitive = PropertyService.Get ("NETResourcesAddin.Search.IsCaseSensitive", false);
			isWholeWordOnly = PropertyService.Get ("NETResourcesAddin.Search.IsWholeWordOnly", false);
			regexSearch     = PropertyService.Get ("NETResourcesAddin.Search.RegexSearch", false);
			searchIn        = PropertyService.Get ("NETResourcesAddin.Search.SearchIn", SearchIn.All);
		}

		public StringEditorOptions (ResXEditorWidget _editor)
		{
			if (_editor == null)
				throw new ArgumentNullException ("editor");

			editor = _editor;
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

		bool IsMatch (string text)
		{
			if (RegexSearch)
				return regex.IsMatch (text);
		
			if (!IsCaseSensitive)
				text = text.ToUpper ();
			int idx = text.IndexOf (filter);
			if (idx >= 0) {
				if (IsWholeWordOnly) {
					return (idx == 0 || char.IsWhiteSpace (text[idx - 1])) &&
						   (idx + filter.Length == text.Length || char.IsWhiteSpace (text[idx + 1]));
				}
				return true;
			}
			return false;
		}

		public bool ShouldFilter (StringEntry entry)
		{

			if (String.IsNullOrEmpty (filter)) 
				return false;

			if (DoSearchIn == SearchIn.Name || DoSearchIn == SearchIn.All) {
				if (IsMatch (entry.Name))
					return false;
			}
			if (DoSearchIn == SearchIn.BaseValue || DoSearchIn == SearchIn.All) {
				if (IsMatch (entry.GetBaseString ()))
					return false;
			}
			if (DoSearchIn == SearchIn.Value || DoSearchIn == SearchIn.All) {
				if (IsMatch (entry.Value))
					return false;
			}
			if (DoSearchIn == SearchIn.Comment || DoSearchIn == SearchIn.All) {
				if (IsMatch (entry.Comment))
					return false;
			}

			return true;
		}

		internal static readonly Gdk.Color errorColor = new Gdk.Color (210, 32, 32);

		void SetUpFilter ()
		{
			filter = editor.SearchEntry.Entry.Text;

			if (!IsCaseSensitive && filter != null)
				filter = filter.ToUpper ();
			if (RegexSearch) {
				try {
					RegexOptions options = RegexOptions.Compiled;
					if (!IsCaseSensitive)
						options |= RegexOptions.IgnoreCase;
					regex = new Regex (filter, options);
				} catch (Exception e) {
					IdeApp.Workbench.StatusBar.ShowError (e.Message);
					editor.SearchEntry.Entry.ModifyBase (StateType.Normal, errorColor);
					return;
				}
			}
			editor.SearchEntry.Entry.ModifyBase (StateType.Normal, editor.Style.Base (StateType.Normal));
		}

		public void Refresh ()
		{
			SetUpFilter ();
			editor.UpdateFromCatalog ();
		}

		// options menu for filterSearchEntry
		public Menu CreateOptionsMenu ()
		{

			Menu menu = new Menu ();
			
			MenuItem searchInMenu = new MenuItem (GettextCatalog.GetString ("_Search in"));
			Menu sub = new Menu ();
			searchInMenu.Submenu = sub;

			Gtk.RadioMenuItem  name = null, baseValue = null, value =null, comment = null, all = null;
			GLib.SList group = new GLib.SList (IntPtr.Zero);

			name = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Name"));
			group = name.Group;
			name.ButtonPressEvent += delegate { name.Activate (); };
			sub.Append (name);
			
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
			case SearchIn.Name:
				name.Activate ();
				break;
			case SearchIn.Comment:
				comment.Activate ();
				break;
			}
			menu.Append (searchInMenu);
			all.Activated += delegate {
				if (DoSearchIn != SearchIn.All) {
					DoSearchIn = SearchIn.All;
					Refresh ();
					menu.Destroy ();
				}
			};
			baseValue.Activated += delegate {
				if (DoSearchIn != SearchIn.BaseValue) {
					DoSearchIn = SearchIn.BaseValue;
					Refresh ();
					menu.Destroy ();
				}
			};
			value.Activated += delegate {
				if (DoSearchIn != SearchIn.Value) {
					DoSearchIn = SearchIn.Value;
					Refresh ();
					menu.Destroy ();
				}
			};
			name.Activated += delegate {
				if (DoSearchIn != SearchIn.Name) {
					DoSearchIn = SearchIn.Name;
					Refresh ();
					menu.Destroy ();
				}
			};
			comment.Activated += delegate {
				if (DoSearchIn != SearchIn.Comment) {
					DoSearchIn = SearchIn.Comment;
					Refresh ();
					menu.Destroy ();
				}
			};
			
			Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = RegexSearch;
			regexSearch.ButtonPressEvent += delegate { 
				RegexSearch = !RegexSearch;
				Refresh ();
			};
			menu.Append (regexSearch);
			
			Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.ButtonPressEvent += delegate { 
				IsCaseSensitive = !IsCaseSensitive;
				Refresh ();
			};
			menu.Append (caseSensitive);
			
			Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.Sensitive = !RegexSearch;
			wholeWordsOnly.ButtonPressEvent += delegate {
				IsWholeWordOnly = !IsWholeWordOnly;
				Refresh ();
			};
			menu.Append (wholeWordsOnly);
			menu.ShowAll ();
			return menu;
		}
	}

	public delegate void AddEntryFunc (StringEntry newEntry);

	public class InserterRow : IStringResourceDisplay {
		string name;
		string _value;
		string comment;
		public ResourceCatalog catalog;
		bool changed;
		AddEntryFunc addEntryFunc;

		#region IStringResourceDisplay implementation
		public string Name {
			get {
				return name;
			} 
			set {
				if (name != value) {
					name = value;
					Changed = true;
				}
			}
		}
		public string Value  {
			get { return _value; }
			set {
				if (_value != value) {
					_value = value;
					Changed = true;	
				}
			}
		}
		public string Comment  { 
			get { return comment; }
			set {
				if (comment != value) {
					comment = value;
					Changed = true;	
				}
			}
		}
		#endregion
		public bool Changed { 
			get {
				return changed;
			} 
			set {
				changed = value;
				if (changed)
					CreateResource ();
			}
		}
		public InserterRow (ResourceCatalog _catalog, AddEntryFunc _addEntryFunc)
		{
			if (_catalog == null)
				throw new ArgumentNullException ("catalog");
			if (_addEntryFunc == null)
				throw new ArgumentNullException ("addEntryFunc");

			catalog = _catalog;
			addEntryFunc = _addEntryFunc;
			Reset ();

		}
		public string GetBaseString ()
		{
			return "";
		}
		public void Selected ()
		{
			name = catalog.NextFreeName ();
		}
		public void Deselected ()
		{
			Reset ();
		}
		void Reset ()
		{
			name = "";
			_value = ""; 
			comment = "";
			Changed = false;
		}
		void CreateResource ()
		{
			StringEntry newEntry = new StringEntry (catalog, Name, Value, Comment, false);
			catalog.AddEntry (newEntry);
			Reset ();
			addEntryFunc (newEntry);
		}
	}
}

