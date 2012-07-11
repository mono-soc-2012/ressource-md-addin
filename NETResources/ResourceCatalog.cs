using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Linq;
using System.Resources;
using System.Reflection;
using System;

namespace MonoDevelop.NETResources {
	public class ResourceCatalog : IEnumerable<ResourceEntry> {
	
		List<ResourceEntry> entriesList = new List<ResourceEntry> ();
		string fileName;

		public bool IsDirty { get; set; }

		public int Count {
			get { 
				return entriesList.Count; 
			}
		}

		public ResourceEntry this[string key] {
			get {
				return entriesList.Where (e=>e.Key == key).FirstOrDefault ();
			}
		}


		public ResourceCatalog () 
		{

		}

		public bool Load (IProgressMonitor monitor, string resxFile)
		{

			fileName = resxFile;
			try {
				using (var reader = new ResXResourceReader (resxFile)) {

					reader.UseResXDataNodes = true;

					string temp;

					foreach (DictionaryEntry de in reader) {

						var node = (ResXDataNode)de.Value;

						if (node.FileRef != null)
							entriesList.Add (new FileRefResourceEntry (node));
							//FIXME: assemblies
						else if (node.GetValueTypeName (new AssemblyName [0]).StartsWith("System.String,"))
							entriesList.Add (new StringResourceEntry (node));
						
						else
							entriesList.Add (new ObjectResourceEntry (node));
					}
				}
				return true;
			} catch (Exception ex) {
				return false;
			}
		}

		#region IEnumerable<ResourceEntry> Members
		public IEnumerator<ResourceEntry> GetEnumerator ()
		{
			return entriesList.GetEnumerator ();
		}
		#endregion

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return entriesList.GetEnumerator ();
		}
		#endregion

    	}
}

