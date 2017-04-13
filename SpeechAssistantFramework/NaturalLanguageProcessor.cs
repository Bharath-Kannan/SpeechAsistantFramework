using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace NaturalLanguageProcessor {
	public class NaturalLanguageProcessor {
		public NaturalLanguageProcessor () {
		}

		private readonly List<Tuple<string, int>> numbersInString = new List<Tuple<string, int>> {
			new Tuple<string, int>("one", 1),
			new Tuple<string, int>("two", 2),
			new Tuple<string, int>("three", 3),
			new Tuple<string, int>("four", 4),
			new Tuple<string, int>("five", 5),
			new Tuple<string, int>("six", 6),
			new Tuple<string, int>("seven", 7),
			new Tuple<string, int>("eight", 8),
			new Tuple<string, int>("nine", 9)};

		public List<IntentConfiguration> IntentConfigurations { get; private set; }
		public List<ContextConfiguration> ContextConfigurations { get; private set; }

		public void SetConfiguration (List<IntentConfiguration> intentConfigurations, List<ContextConfiguration> contextConfigurations) {
			this.IntentConfigurations = intentConfigurations;
			this.ContextConfigurations = contextConfigurations;
		}

		public void SetConfiguration (string intentConfigurations, string contextConfigurations) {
			this.IntentConfigurations = JsonConvert.DeserializeObject<List<IntentConfiguration>> (intentConfigurations);
			this.ContextConfigurations = JsonConvert.DeserializeObject<List<ContextConfiguration>> (contextConfigurations);
		}

		public IntentResult GetMatchingIntent (string speechText) {

			List<string> words = speechText.Split (' ').ToList ();

			//For each config
			var matchingIntents = from i in this.IntentConfigurations
								  where ProcessForMatch (i, words)
								  select i;

			if (matchingIntents.Any ()) {
				var matchingEntity = matchingIntents.FirstOrDefault ();
				Dictionary<string, List<string>> parameters = null;

				if (matchingEntity.Entities != null && matchingEntity.Entities.Any ())
					parameters = ProcessForEntities (matchingEntity.Entities, speechText);

				if (matchingEntity.ConsiderInputTextAsParameter) {
					if (parameters == null)
						parameters = new Dictionary<string, List<string>> ();
					parameters.Add ("input", new List<string> () { speechText });
				}

				return new IntentResult () {
					Action = matchingEntity.Action,
					Parameters = parameters
				};
			} else
				return null;
		}

		public List<string> GetSuggestions (string contextName) {
			var contextConfigurations = from cc in this.ContextConfigurations
										where cc.Name == contextName
										select cc;

			if (contextConfigurations.Any ()) {
				var contextConfiguration = contextConfigurations.FirstOrDefault ();
				var intentConfigurations = from ic in this.IntentConfigurations
										   where contextConfiguration.Intents.Contains (ic.IntentName)
										   select ic;

				if (intentConfigurations.Any ()) {
					var suggestions = new List<String> ();
					intentConfigurations.ToList ().ForEach (ic => {
						suggestions.AddRange (ic.Suggestions);
					});

					return suggestions;
				} else
					return null;
			} else
				return null;
		}

		private bool ProcessForMatch (IntentConfiguration intentConfiguration, List<string> words) {
			bool result = false;

			if (ProcessForKeywords (intentConfiguration.Keywords, words))
				if (ProcessForKeywords (intentConfiguration.Verbs, words) || intentConfiguration.Verbs == null)
					result = true;


			return result;
		}

		private bool ProcessForKeywords (List<string> keywords, List<string> words) {
			bool result = false;

			if (keywords == null || !keywords.Any ())
				return result;

			var matchingWords = from w in words
								where keywords.Contains (w, new RegexEqualityComparer ())
								select w;

			if (matchingWords.Any ())
				result = true;

			return result;
		}

		private Dictionary<string, List<string>> ProcessForEntities (List<Entity> entities, string speechText) {
			Dictionary<string, List<string>> parameters = new Dictionary<string, List<string>> ();
			var words = speechText.Split (' ').ToList ();
			entities.ForEach (entity => {
				if (entity.RegexParametersMetaData == null) {
					var matches = from w in words
								  where entity.Values.Contains (w, StringComparer.OrdinalIgnoreCase)
								  select w;

					if (matches.Any ()) {
						parameters.Add (entity.Name, matches.ToList ());
					}
				} else {
					entity.Values.ForEach (val => {
						var match = Regex.Match (speechText, val);
						if (match.Success) {
							var matchedSubstring = match.Value;

							entity.RegexParametersMetaData.ForEach (parameterMetaData => {
								if (parameterMetaData.Type == "number") {
									var values = Regex.Split (matchedSubstring, @"\D+")
													  .Where (s => !string.IsNullOrWhiteSpace (s)).ToList ();

									if (values != null && values.Any ()) {
										if (!parameters.ContainsKey (parameterMetaData.Name))
											parameters.Add (parameterMetaData.Name, new List<string> () { values [entity.RegexParametersMetaData.IndexOf (parameterMetaData)] });
									} else {
										var numberMatches = from w in matchedSubstring.Split (' ')
															where this.numbersInString.Select (number => number.Item1).Contains (w, StringComparer.OrdinalIgnoreCase)
															select w;

										if (numberMatches != null && numberMatches.Any ()) {
											if (!parameters.ContainsKey (parameterMetaData.Name))
												parameters.Add (parameterMetaData.Name, new List<string> () { this.numbersInString.Where (n => n.Item1 == numberMatches.FirstOrDefault ()).FirstOrDefault ().Item2.ToString () });
										}
									}
								}
							});
						}
					});
				}
			});

			if (parameters.Count () > 0)
				return parameters;
			else
				return null;
		}
	}
}
