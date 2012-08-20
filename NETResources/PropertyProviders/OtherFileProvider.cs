//
// OtherFileProvider.cs : Class enabling display of OtherFileEntry 
// properties in Property Pad
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

