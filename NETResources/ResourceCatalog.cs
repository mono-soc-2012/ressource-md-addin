using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Linq;
using System.Resources;
using System.Reflection;
using System;
using System.IO;

namespace MonoDevelop.NETResources {
	public class ResourceCatalog : IEnumerable<ResourceEntry> {
		private bool isDirty;

		public event EventHandler DirtyChanged;

		List<ResourceEntry> entriesList = new List<ResourceEntry> ();
		string fileName;

		public bool IsDirty {
			get { return isDirty; }
			set {
				isDirty = value;
				OnDirtyChanged (EventArgs.Empty);
			}
		}

		public int Count {
			get { 
				return entriesList.Count; 
			}
		}

		public ResourceEntry this[string name] {
			get {
				return entriesList.Where (e=>e.Name == name).FirstOrDefault ();
			}
		}

		public int MaxRelativeXPos {
			get {
				return entriesList.Max (e=> e.RelativePos.X);
			}
		}


		public ResourceCatalog () 
		{

		}

		public void AddEntry (ResourceEntry entry)
		{
			entriesList.Add (entry);
			IsDirty = true;
		}

		public void RemoveEntry (ResourceEntry entry)
		{
			entriesList.Remove (entry);
			IsDirty = true;
		}

		void Clear ()
		{
			entriesList.Clear ();
		}

		public bool Load (IProgressMonitor monitor, string resxFile)
		{
			Clear ();
			fileName = resxFile;
			try {
				using (var reader = new ResXResourceReader (resxFile)) {
					reader.UseResXDataNodes = true;

					foreach (DictionaryEntry de in reader) {
						var node = (ResXDataNode) de.Value;

						if (node.FileRef != null)
							entriesList.Add (new FileRefResourceEntry (this, node));
						else if (node.GetValueTypeName (new AssemblyName [0]) == 
						         typeof (string).AssemblyQualifiedName)
							entriesList.Add (new StringResourceEntry (this, node));
						else
							entriesList.Add (new ObjectResourceEntry (this, node));
					}
				}
				return true;
			} catch (Exception ex) {
				string msg = "Error loading file '" + fileName + "'.";
				LoggingService.LogFatalError (msg, ex);
				if (monitor != null)
					monitor.ReportError (msg, ex);
				return false;
			}
		}

		public bool Save (string newFileName)
		{
			StreamWriter sw;
			int fileCounter = 0;
			string tempFileName = "";
			
			// get a temp file in same directory as newFileName for safe write
			try {
				do  {
					fileCounter++;
					tempFileName = newFileName + fileCounter.ToString();
				} while (File.Exists (tempFileName));
				sw = new StreamWriter (tempFileName);
			}
			catch (Exception ex) {
				LoggingService.LogError ("Unhandled error creating temp file while saving Gettext catalog '{0}': {1}", tempFileName, ex);
				return false;
			}
			// write out resources
			// FIXME: will write metadata resources as normal resources
			using (sw) {
				using (var rw = new ResXResourceWriter (sw)) {
					foreach (var res in entriesList.OrderBy (e=> e.RelativePos.X).ThenBy (e=> e.RelativePos.Y)) {
						rw.AddResource (res.Node);
					}
				}
			}
			//try to replace original file
			bool saved = false;
			try {
				File.Copy (tempFileName, newFileName, true);
				saved = true;
			}
			catch (Exception ex){
				LoggingService.LogError ("Unhandled error saving ResX File to '{0}': {1}", newFileName, ex);
				saved = false;
			}	
			finally {
				File.Delete(tempFileName);
			}
			
			if (!saved)
				return false;
						
			fileName = newFileName;
			IsDirty = false;
			return true;
		}

		protected virtual void OnDirtyChanged (EventArgs e)
		{
			if (DirtyChanged != null)
				DirtyChanged (this, e);
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

