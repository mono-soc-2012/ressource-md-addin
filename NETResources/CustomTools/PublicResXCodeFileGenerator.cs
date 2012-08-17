using System;
using MonoDevelop.Ide.CustomTools;
using System.Resources.Tools;
using MonoDevelop.Projects;
using System.IO;
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using System.Threading;
using System.CodeDom;

namespace MonoDevelop.NETResources.CustomTools {
	public class PublicResXCodeFileGenerator : ISingleFileCustomTool {
		public PublicResXCodeFileGenerator ()
		{

		}

		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				ResXCodeFileGenerator.Generate (monitor, file, result, false);
			}, result);
		}
	}
}

