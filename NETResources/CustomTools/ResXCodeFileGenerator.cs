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
	public class ResXCodeFileGenerator : ISingleFileCustomTool {
		public ResXCodeFileGenerator ()
		{

		}

		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				Generate (monitor, file, result, true);
			}, result);
		}

		internal static void Generate (IProgressMonitor monitor, ProjectFile file, 
		                               SingleFileCustomToolResult result, bool internalClass)
		{
			var outputFile = file.FilePath.ChangeExtension (".Designer.cs");
			
			DotNetProject dnProject = file.Project as DotNetProject;
			if (dnProject == null)
				return; //don't do anything if file not in project
			
			CodeDomProvider provider = dnProject.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				return; //don't do anything if no provider available
			
			// this returns the namespace in accordance with DotNetNamingPolicy in use
			string resourcesNamespace = dnProject.GetDefaultNamespace (outputFile.ToString ());
			string baseName = file.FilePath.FileNameWithoutExtension;

			string genCodeNamespace = String.IsNullOrWhiteSpace (file.CustomToolNamespace) ? 
							resourcesNamespace : file.CustomToolNamespace;

			string [] unmatchables;
			CodeCompileUnit ccu;
			
			ccu = StronglyTypedResourceBuilder.Create (file.FilePath.ToString (),
			                                           baseName,
			                                           genCodeNamespace,
			                                           resourcesNamespace,
			                                           provider,
			                                           internalClass,
			                                           out unmatchables);
			// generate code overwriting existing file
			using (var writer = new StreamWriter (outputFile, false)) {
				provider.GenerateCodeFromCompileUnit (ccu,
				                                      writer,
				                                      new CodeGeneratorOptions());
			}
			
			result.GeneratedFilePath = outputFile;
			
			foreach (var u in unmatchables) //should report these better?
				monitor.Log.WriteLine ("Unable to create property for resource named: {0}", u);
		}
	}
}

