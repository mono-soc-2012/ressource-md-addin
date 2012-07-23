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
using System.Text.RegularExpressions;
using System.Globalization;

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

		public ResourceCatalog BaseCatalog { get; private set; }

		public ResourceCatalog () 
		{

		}

		public bool ContainsName (string name)
		{
			return entriesList.Any (e => e.Name == name);
		}

		public bool IsUniqueName (string oldName, string newName, bool IsNew)
		{
			if (IsNew || (!IsNew && (oldName != newName)))
				return !entriesList.Any (e=> e.Name == newName);
			else
				return true;
		}

		public string NextFreeName ()
		{
			string tempName;
			int i = 0;
			do {
				i++;
				tempName = "String" + i;
			} while (entriesList.Any (e=> e.Name == tempName));

			return tempName;
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

		void GetBaseCatalog ()
		{

			Regex cultureRegex = new Regex (@"(?<PreCulture>.*)\.(?<Culture>[^\.]*)\.resx$", RegexOptions.IgnoreCase);
			Match cultureMatch = cultureRegex.Match (fileName);

			if (!cultureMatch.Success) { //this is a base catalog
				BaseCatalog = null;
				return;
			}

			string cultureString = cultureMatch.Groups ["Culture"].Value;

			if (cultureString == "aspx") { //this is a base catalog
				BaseCatalog = null;
				return;
			}

			try {
				new CultureInfo (cultureString); // raises error if not valid
				TryLoadBaseCatalog (cultureMatch.Groups ["PreCulture"].Value + ".resx");
				return;
			} catch {
				//FIXME: gets here when no valid culture found, but ignores custom / not installed cultures, maybe log too?
				BaseCatalog = null;
				return;
			}

			/*
			foreach (var name in  CultureInfo.GetCultures (CultureTypes.AllCultures).Select (c=> c.Name)) {
				if (name.ToUpper () == cultureString.ToUpper ()) {
					TryLoadBaseCatalog (cultureMatch.Groups ["PreCulture"].Value + ".resx");
					return;
				}
			}
			BaseCatalog = null;
			return;
			*/
		}

		void TryLoadBaseCatalog (string baseFilePath)
		{
			if (!File.Exists (baseFilePath)) {
				//FIXME: log base catalog not found
				BaseCatalog = null;
				return;
			}

			try {
				ResourceCatalog tempCat = new ResourceCatalog ();
				tempCat.Load (null, baseFilePath);
				BaseCatalog = tempCat;
			} catch (Exception ex) {
				//FIXME: log
				BaseCatalog = null;
			}

		}

		public bool Load (IProgressMonitor monitor, string resxFile)
		{
			Clear ();
			fileName = resxFile;
			GetBaseCatalog ();

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

