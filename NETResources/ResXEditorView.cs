using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Projects;

namespace MonoDevelop.NETResources {
	internal class ResXEditorView : AbstractViewContent, IPropertyPadProvider { //, IUndoHandler
		public ResXEditorView ()
		{
		}
		
		ResourceCatalog catalog;
		ResXEditorWidget resXEditorWidget;
		//FIXME: why is file loaded separately from constructor?
		public ResXEditorView (string resxFile, Project project)
		{
			catalog = new ResourceCatalog (project);
			resXEditorWidget = new ResXEditorWidget ();
			catalog.DirtyChanged += delegate (object sender, EventArgs args) {
				IsDirty = catalog.IsDirty;
			};
		}
		
		public override void Load (string fileName)
		{
			catalog.Load (null, fileName);
			resXEditorWidget.Catalog = catalog;

			//poEditorWidget.POFileName = fileName;
			//poEditorWidget.UpdateRules (System.IO.Path.GetFileNameWithoutExtension (fileName));

			this.ContentName = fileName;
			this.IsDirty = false;
		}
		
		public override void Save (string fileName)
		{
			OnBeforeSave (EventArgs.Empty);
			catalog.Save (fileName);
			ContentName = fileName;
			IsDirty = false;
		}
		
		public override void Save ()
		{
			Save (this.ContentName);
		}

		#region IPropertyPadProvider implementation

		public object GetActiveComponent ()
		{
			return resXEditorWidget.GetObjectForPropPad ();
		}

		public object GetProvider ()
		{
			object obj = GetActiveComponent ();

			if (obj is ResourceEntry)
				return EntryProvider.GetProvider ((ResourceEntry) obj);
			else
				return null; // nothing will be shown
		}

		public void OnEndEditing (object obj)
		{
		}

		public void OnChanged (object obj)
		{
			resXEditorWidget.OnPropertyPadChanged ();
		}

		#endregion

		//FIXME: very hacky solution to keeping pad up to date!!!!!!
		internal static void SetPropertyPad (object obj)
		{
			object [] providers;
			if (obj is ResourceEntry)
			providers = new object [] { EntryProvider.GetProvider ((ResourceEntry) obj) };
			else
				providers = new object [0]; // prop pad will show nothing

			try {
				var pad = IdeApp.Workbench.GetPad <MonoDevelop.DesignerSupport.PropertyPad> ();
				var propPad = (MonoDevelop.DesignerSupport.PropertyPad) pad.Content;
				var en = ((InvisibleFrame) propPad.Control).AllChildren.GetEnumerator ();
				en.MoveNext ();
				var grid = (MonoDevelop.Components.PropertyGrid.PropertyGrid) en.Current;
				grid.SetCurrentObject (obj, providers);
			} catch (Exception ex) {
				LoggingService.LogError (GettextCatalog.GetString ("Error occurred in hack to update property pad {0}"), ex);
				//FIXME: for debugging
				throw ex;
			}
		}

		/*
		#region IUndoHandler implementation
		void IUndoHandler.Undo ()
		{
			//poEditorWidget.Undo ();
		}
		
		void IUndoHandler.Redo ()
		{
			//poEditorWidget.Redo ();
		}
		
		IDisposable IUndoHandler.OpenUndoGroup ()
		{
			//return poEditorWidget.OpenUndoGroup ();
			throw new NotImplementedException ();
		}
		
		bool IUndoHandler.EnableUndo {
			get {
				// return poEditorWidget.EnableUndo;
				throw new NotImplementedException ();
			}
		}
		
		bool IUndoHandler.EnableRedo {
			get {
				//return poEditorWidget.EnableRedo;
				throw new NotImplementedException ();
			}
		}
		#endregion
		*/
		public override Widget Control
		{
			get { return resXEditorWidget; }
		}
				
		public override bool IsReadOnly
		{
			get { return false; }
		}
		
		public override string TabPageLabel 
		{
			get { return GettextCatalog.GetString ("ResX Editor"); }
		}
	
    }
}

