using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NaturalLanguageProcessor {
	public class RegexEqualityComparer: IEqualityComparer<string> {
		public RegexEqualityComparer () {
		}

		public bool Equals (string x, string y) {
			var match = Regex.Match (y, x, RegexOptions.IgnoreCase);
			return match.Success;
		}

		public int GetHashCode (string obj) {
			return obj.GetHashCode ();
		}
	}
}
