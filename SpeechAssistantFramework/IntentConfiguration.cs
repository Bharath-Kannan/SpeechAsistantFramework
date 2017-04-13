using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NaturalLanguageProcessor {
	public class IntentConfiguration {
		public IntentConfiguration () {
		}

		[JsonProperty (PropertyName = "intentName")]
		public string IntentName { get; set; }

		[JsonProperty (PropertyName = "verbs")]
		public List<string> Verbs { get; set; }

		[JsonProperty (PropertyName = "keywords")]
		public List<string> Keywords { get; set; }

		[JsonProperty (PropertyName = "considerInputTextAsParameter")]
		public bool ConsiderInputTextAsParameter { get; set; }

		[JsonProperty (PropertyName = "entities")]
		public List<Entity> Entities { get; set; }

		[JsonProperty (PropertyName = "action")]
		public string Action { get; set; }

		[JsonProperty (PropertyName = "suggestions")]
		public List<string> Suggestions { get; set; }
	}
}