//
// ResXEditorDisplayBinding.cs : IViewDisplayBinding implementation for 
// editor
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;

namespace MonoDevelop.NETResources {
	public class ResXEditorDisplayBinding : IViewDisplayBinding {
		// FIXME: use GetText
		public  string Name {
			get { return GettextCatalog.GetString ("ResX Editor"); }
		}

		public bool CanHandle (FilePath filePath, string mimeType, Project project)
		{
			return filePath.IsNotNull && filePath.HasExtension (".resx");
		}

		public IViewContent CreateContent (FilePath filePath, string mimeType, Project project)
		{
			/*
			foreach (TranslationProject tp in IdeApp.Workspace.GetAllSolutionItems<TranslationProject>  ())
				if (tp.BaseDirectory == Path.GetDirectoryName (filePath))
					return new Editor.CatalogEditorView (tp, filePath);
			
			return new Editor.CatalogEditorView (null, filePath);
			*/

			return new ResXEditorView (filePath, project);
		}

		// Whether the display binding can be used as the default handler for the content types
		// that it handles. If this is false, the binding is only used when the user explicitly picks it.
		public bool CanUseAsDefault {
			get { return true; }
		}
    }
}

