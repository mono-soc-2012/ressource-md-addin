//
// ResXEditorView.cs : AbstractViewContent implementation for editor
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

			try {
				catalog.Save (fileName); //not checking return value as throwing exceptions on errors
				ContentName = fileName;
				IsDirty = false;
			} catch (Exception ex) {
				MessageService.ShowException (ex, ex.Message, "The file could not be saved");
			}
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

