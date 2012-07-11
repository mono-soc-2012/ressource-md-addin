using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.NETResources {
	internal class ResXEditorView : AbstractViewContent { //, IUndoHandler
		public ResXEditorView ()
		{
		}
		
		ResourceCatalog catalog;
		ResXEditorWidget resXEditorWidget;
		
		public ResXEditorView (string resxFile)
		{
			catalog = new ResourceCatalog ();
			resXEditorWidget = new ResXEditorWidget ();
			//catalog.DirtyChanged += delegate (object sender, EventArgs args) {
			//	IsDirty = catalog.IsDirty;
			//};

			//Load (resxFile);
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
			//OnBeforeSave (EventArgs.Empty);
			//catalog.Save (fileName);
			//ContentName = fileName;
			//IsDirty = false;
		}
		
		public override void Save ()
		{
			//Save (this.ContentName);
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

