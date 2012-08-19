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
using System.Runtime.Serialization;

namespace MonoDevelop.NETResources {
	public class ResourceCatalog : IEnumerable<ResourceEntry> {
		private bool isDirty;

		public event EventHandler DirtyChanged;

		List<ResourceEntry> entriesList = new List<ResourceEntry> ();
		string fileName;

		public Project Project { get; private set;}

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
				if (entriesList.Count == 0)
					return 0;
				return entriesList.Max (e=> e.RelativePos.X);
			}
		}

		public ProjectFile ProjectFile {
			get {
				if (Project == null)
					return null;
				return Project.GetProjectFile (fileName);
			}
		}

		public ResourceCatalog BaseCatalog { get; private set; }

		public ResourceCatalog (Project project) 
		{
			Project = project;
		}

		public bool Contains (ResourceEntry entry)
		{
			return entriesList.Contains (entry);
		}

		public bool ContainsName (string name)
		{
			return entriesList.Any (e => e.Name.ToLower () == name.ToLower ());
		}

		public bool IsUniqueName (string oldName, string newName, bool IsNew)
		{
			if (IsNew || (!IsNew && (oldName != newName)))
				return !entriesList.Any (e=> e.Name.ToLower () == newName.ToLower ());
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
			} while (ContainsName (tempName));

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
				ResourceCatalog tempCat = new ResourceCatalog (Project);
				tempCat.Load (null, baseFilePath);
				BaseCatalog = tempCat;
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error loading base catalog {0} for resx file {1} : {2}", 
				                         baseFilePath, fileName ,ex);
				BaseCatalog = null;
			}

		}

		public bool Load (IProgressMonitor monitor, string resxFile)
		{
			Clear ();
			fileName = resxFile;
			GetBaseCatalog ();

			try {
				/* we need to get the resources as ResXDataNodes to access comment and file info
				 * this returns both data and metadata elements without any distinction between them
				 * thus have to run GetMetadataEnumerator (keeping UseResXDataNodes false as default) to
				 * get a list of meta data keys first
				 */ 
				// FIXME: GetMetadataEnumerator throws exceptions if referenced file is missing, and probably 
				// other occasions as it tries to instantiate everything in the file
				var meta_keys = new List<string> ();
				try { 
				using (var reader = new ResXResourceReader (resxFile)) {
					reader.BasePath = Path.GetDirectoryName (resxFile);
					var enumerator = reader.GetMetadataEnumerator ();
					while (enumerator.MoveNext ())
						meta_keys.Add (((DictionaryEntry) enumerator.Current).Key as string);
				}
				} catch {
					//carry on for now, risking some metadata resources being saved back as data resources
				}

				using (var reader = new ResXResourceReader (resxFile)) {
					reader.UseResXDataNodes = true;
					reader.BasePath = Path.GetDirectoryName (resxFile);
					foreach (DictionaryEntry de in reader) {
						var node = (ResXDataNode) de.Value;
						bool isMeta = meta_keys.Contains (node.Name);
						string typeName = node.GetValueTypeName (new AssemblyName [0]);
						// ignore assembly versions
						if (typeName.StartsWith ("System.Drawing.Icon, System.Drawing"))
							AddEntry (new IconEntry (this, node, isMeta));
						else if (typeName.StartsWith ("System.Drawing.Bitmap, System.Drawing"))
							AddEntry (new ImageEntry (this, node, isMeta));
						else if (typeName.StartsWith ("System.IO.MemoryStream, mscorlib")) 
							AddEntry (new AudioEntry (this, node, isMeta));
						else if (typeName.StartsWith ("System.String, mscorlib")) {
							if (node.FileRef == null)
								AddEntry (new StringEntry (this, node, isMeta));
							else
								AddEntry (new BinaryOrStringEntry (this, node, isMeta));
						} else if (typeName.StartsWith ("System.Byte[], mscorlib")) {
							if (node.FileRef == null)
								AddEntry (new OtherEmbeddedEntry (this, node, isMeta));
							else
								AddEntry (new BinaryOrStringEntry (this, node, isMeta));
						} else if (node.FileRef == null)
							AddEntry (new OtherEmbeddedEntry (this, node, isMeta));
						else 
							AddEntry (new OtherFileEntry (this, node, isMeta));
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

		// tried a tidier version of this using a stringwriter but limited the encoding of xml output to utf-16
		public bool Save (string newFileName)
		{
			// get a temp file in same directory as newFileName for safe write
			int fileCounter = 0;
			string tempFileName = "";
			StreamWriter sw;
			try {
				do  {
					fileCounter++;
					tempFileName = newFileName + fileCounter.ToString();
				} while (File.Exists (tempFileName));
				sw = new StreamWriter (tempFileName);
			}
			catch (Exception ex) {
				LoggingService.LogError ("Unhandled error creating temp file '{0}' while saving resx " +
							"file : {1}", tempFileName, ex);
				throw new Exception ("Could not create a temporary file in same directory as target " +
							"file for save", ex);
			}
			// write out resources
			using (var rw = new ResXResourceWriter (sw)) {
				rw.BasePath = Path.GetDirectoryName (newFileName); // make fileRefs relative to new resx dir
				foreach (var res in entriesList.OrderBy (e=> e.RelativePos.X).ThenBy (e=> e.RelativePos.Y)) {
					if (res.IsMeta) {
						/* since AddMetadata does not have an overload accepting a ResXDataNode 
						 * the resource needs to be instantiated. This will fail for unresolvable 
						 * types with TypeLoadException or ArgumentException
						 */ 
						try {
							rw.AddMetadata (res.Node.Name, res.GetValue ()); // no comment stored
						} catch (Exception ex) {
							LoggingService.LogError ("Unhandled error adding metadata " +
										"resource while saving resx file '{0}'" +
										": {1}", newFileName, ex);
							File.Delete (tempFileName);
							throw new Exception ("Could not add metadata resource named '" +
							                     res.Name + "'\nNote: only types resolvable " + 
							                     "by monodevelop are currently supported as " +
							                     "metadata resources", ex);
						}
					} else {
						try { 
							rw.AddResource (res.Node); 
						} catch (Exception ex) { //unlikely to get error here
							LoggingService.LogError ("Unhandled error adding data " +
							                         "resource while saving resx file '{0}'" +
							                         ": {1}", newFileName, ex);
							File.Delete (tempFileName); 
							throw ex;
						}
					}
				}
			}
			//try to replace original file
			try {
				File.Copy (tempFileName, newFileName, true);
			}
			catch (Exception ex){
				LoggingService.LogError ("Unhandled error saving ResX File to '{0}': {1}", newFileName, ex);
				File.Delete(tempFileName);
				throw new Exception ("Could not copy from temp file to the target save file", ex);
			}	

			File.Delete(tempFileName);
			fileName = newFileName;
			IsDirty = false;
			return true;
		}

		protected virtual void OnDirtyChanged (EventArgs e)
		{
			if (DirtyChanged != null)
				DirtyChanged (this, e);
		}

		#region IEnumerable<ResourceEntry2> Members
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

