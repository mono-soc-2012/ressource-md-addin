using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.NETResources {
	public class OtherFileProvider : EntryProvider {
		OtherFileEntry OtherFileEntry {
			get { return (OtherFileEntry) Entry; }
		}
		internal OtherFileProvider (OtherFileEntry entry)
		{
			Entry = entry;
		}
		
		public string FileName {
			get { return OtherFileEntry.FileName; }
		}

		public Encoding TextFileEncoding {
			get { return OtherFileEntry.TextFileEncoding; }
		}

		public Persistence Persistence { 
			get { return Persistence.Linked; }
		}
	}
}

