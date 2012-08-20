//
// ResXCodeFileGenerator.cs : Custom Tool creating an internal class
// providing strongly typed access to the resources in a resx file
// which is embedded in the same assembly as current project
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

