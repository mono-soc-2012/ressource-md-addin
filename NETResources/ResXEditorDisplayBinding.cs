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

