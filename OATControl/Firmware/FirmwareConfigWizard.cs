using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace OATControl.Firmware
{
	/// <summary>
	/// Runs the firmware configuration wizard defined in Resources/FirmwareConfig/steps.json, which is exported from
	/// the config.openastrotech.com website (tools/firmware-config/export-steps.mjs). The logic mirrors the website's
	/// WizardStep.jsx so that both produce identical Configuration_local.hpp files for the same answers. JS semantics
	/// that affect the output (truthiness, first-match String.replace, "undefined" for missing values) are kept on purpose.
	/// </summary>
	public class FirmwareConfigWizard
	{
		private readonly JObject _schema;
		private List<ConfigEntry> _configuration = new List<ConfigEntry>();
		private readonly Stack<HistoryEntry> _history = new Stack<HistoryEntry>();

		private class ConfigEntry
		{
			public string Variable;
			public JToken Value; // a string/number for choice steps, a JArray of {key, value} for text input steps
		}

		private class HistoryEntry
		{
			public int StepIndex;
			public List<ConfigEntry> ConfigurationBefore;
			public JObject Answer; // {step, key} for a choice or {step, text} for a text input step, see ExportAnswers
		}

		private class EvalResult
		{
			public bool Skip;
			public bool? Bool;
		}

		public FirmwareConfigWizard(JObject schema)
		{
			_schema = schema;
			StepIndex = -1;
			Advance();
		}

		public static JObject LoadEmbeddedSchema()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var name = assembly.GetManifestResourceNames().First(n => n.EndsWith("FirmwareConfig.steps.json", StringComparison.OrdinalIgnoreCase));
			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				return JObject.Parse(reader.ReadToEnd());
			}
		}

		public string SourceVersion => (string)_schema["sourceVersion"] ?? string.Empty;

		public int StepIndex { get; private set; }

		public bool IsComplete { get; private set; }

		public bool CanGoBack => _history.Count > 0;

		public IList<JObject> Steps
		{
			get
			{
				var tracker = FindConfig("tracker")?.Value?.ToString() ?? "OAT";
				var steps = _schema["trackers"][tracker] as JArray;
				return steps == null ? new List<JObject>() : steps.Cast<JObject>().ToList();
			}
		}

		public JObject CurrentStep => IsComplete || StepIndex < 0 || StepIndex >= Steps.Count ? null : Steps[StepIndex];

		/// <summary>Returns the value chosen for a variable (choice key), or null.</summary>
		public string GetValue(string variable)
		{
			var entry = FindConfig(variable);
			return entry?.Value is JValue ? JsString(entry.Value) : null;
		}

		/// <summary>Returns the define value of the chosen option of the step that owns the variable (e.g. BOARD_AVR_RAMPS).</summary>
		public string GetChosenDefineValue(string variable)
		{
			var value = GetValue(variable);
			if (value == null)
			{
				return null;
			}

			foreach (var step in _schema["trackers"].Children<JProperty>().SelectMany(p => p.Value.Cast<JObject>()))
			{
				if ((string)step["variable"] != variable)
				{
					continue;
				}

				var choice = Choices(step).FirstOrDefault(c => (string)c["key"] == value);
				if (choice != null)
				{
					return (string)choice["defineValue"];
				}
			}

			return null;
		}

		/// <summary>The steps answered so far with the text of the chosen option (text input steps show "Custom values").</summary>
		public IEnumerable<KeyValuePair<string, string>> AnsweredSteps()
		{
			foreach (var entry in _history.Reverse())
			{
				var step = StepAt(entry.StepIndex, entry.ConfigurationBefore);
				if (step == null)
				{
					continue;
				}

				var config = FindConfig((string)step["variable"]);
				string description = "";
				if (config?.Value is JArray)
				{
					description = "Values entered";
				}
				else if (config != null)
				{
					var choice = Choices(step).FirstOrDefault(c => JsString(c["key"]) == JsString(config.Value));
					description = choice != null ? (string)choice["value"] : JsString(config.Value);
				}

				yield return new KeyValuePair<string, string>((string)step["title"], description);
			}
		}

		public IEnumerable<JObject> EnabledChoices(JObject step)
		{
			return Choices(step).Where(ch =>
			{
				var tree = ch["conditionTree"];
				if (tree == null)
				{
					return true;
				}

				return Evaluate(tree).Bool == true;
			});
		}

		/// <summary>The value shown in a text input before the user edits it.</summary>
		public string DisplayDefaultValue(JObject choice)
		{
			var value = GetDefaultValue(choice["defaultValue"]);
			return value == null ? string.Empty : JsString(value);
		}

		public void Select(string key)
		{
			var step = CurrentStep ?? throw new InvalidOperationException("No current step");
			var variable = (string)step["variable"];
			var chosen = Choices(step).FirstOrDefault(c => (string)c["key"] == key) ?? throw new ArgumentException("Unknown choice " + key);

			PushHistory(new JObject { ["step"] = step["id"], ["key"] = key });
			_configuration = _configuration.Where(c => c.Variable != variable).ToList();
			_configuration.Add(new ConfigEntry { Variable = variable, Value = key });

			if (chosen["additionalVariables"] is JArray additional)
			{
				foreach (JObject vr in additional)
				{
					var prop = vr.Properties().First();
					_configuration.Add(new ConfigEntry { Variable = prop.Name, Value = prop.Value.DeepClone() });
				}
			}

			Advance();
		}

		/// <summary>
		/// Completes a text input step. <paramref name="editedValues"/> holds only the fields the user changed
		/// (key -> text); untouched fields fall back to their default values, as on the website.
		/// </summary>
		public void SubmitText(IDictionary<string, string> editedValues)
		{
			var step = CurrentStep ?? throw new InvalidOperationException("No current step");
			var variable = (string)step["variable"];

			var text = new JObject();
			foreach (var kv in editedValues ?? new Dictionary<string, string>())
			{
				text[kv.Key] = kv.Value;
			}

			PushHistory(new JObject { ["step"] = step["id"], ["text"] = text });
			var values = new JArray();
			if (editedValues == null || editedValues.Count == 0)
			{
				foreach (var choice in Choices(step))
				{
					var def = GetDefaultValue(choice["defaultValue"]);
					values.Add(new JObject { ["key"] = choice["key"], ["value"] = IsTruthy(def) ? def.DeepClone() : "" });
				}
			}
			else
			{
				foreach (var kv in editedValues)
				{
					values.Add(new JObject { ["key"] = kv.Key, ["value"] = kv.Value });
				}
			}

			_configuration = _configuration.Where(c => c.Variable != variable).ToList();
			_configuration.Add(new ConfigEntry { Variable = variable, Value = values });
			Advance();
		}

		/// <summary>
		/// Goes back to the previously answered step and forgets that answer and everything after it. (The website keeps
		/// later answers when going back, which makes it skip steps whose answers no longer apply; we deliberately don't.)
		/// </summary>
		public void GoBack()
		{
			if (!CanGoBack)
			{
				return;
			}

			var entry = _history.Pop();
			_configuration = entry.ConfigurationBefore;
			StepIndex = entry.StepIndex;
			IsComplete = false;
		}

		/// <summary>The answers given so far, in order, for saving and replaying with <see cref="ReplayAnswers"/>.</summary>
		public JArray ExportAnswers()
		{
			return new JArray(_history.Reverse().Select(h => h.Answer.DeepClone()));
		}

		/// <summary>
		/// Gives saved answers again on a fresh wizard. Stops at the first answer that no longer fits the steps (for
		/// example after the step definitions changed), so the user continues from there.
		/// </summary>
		public void ReplayAnswers(JArray answers)
		{
			foreach (var answer in answers.OfType<JObject>())
			{
				var step = CurrentStep;
				if (step == null || (string)step["id"] != (string)answer["step"])
				{
					return;
				}

				var keys = EnabledChoices(step).Select(c => (string)c["key"]).ToList();
				var isText = (string)step["control"]["type"] == "textinput";
				if (isText && answer["text"] is JObject text && text.Properties().All(p => keys.Contains(p.Name)))
				{
					SubmitText(text.Properties().ToDictionary(p => p.Name, p => (string)p.Value));
				}
				else if (!isText && answer["key"] != null && keys.Contains((string)answer["key"]))
				{
					Select((string)answer["key"]);
				}
				else
				{
					return;
				}
			}
		}

		public List<string> GenerateLines()
		{
			var steps = Steps;
			var configKey = "";
			var defines = new List<string>();

			foreach (var checkStep in steps)
			{
				var config = FindConfig((string)checkStep["variable"]);
				if (config == null)
				{
					continue;
				}

				if (configKey.Length > 0)
				{
					configKey += ",";
				}

				JObject property = null;
				var properties = steps.Where(p => (string)p["variable"] == config.Variable).ToList();
				if (properties.Count == 1)
				{
					property = properties[0];
				}
				else
				{
					foreach (var prop in properties)
					{
						var result = Evaluate(prop["conditionTree"]);
						bool skip = result.Skip || result.Bool != true;
						if (!skip)
						{
							if (property != null)
							{
								throw new InvalidOperationException("More than one property satisfies condition [" + prop["condition"] + "]. " + string.Join(", ", properties.Select(p => (string)p["id"])));
							}

							property = prop;
						}
					}
				}

				if (property == null)
				{
					continue;
				}

				configKey += (string)property["id"];
				var control = property["control"] as JObject;
				if (control != null)
				{
					if ((string)control["type"] == "textinput")
					{
						AddLines(defines, property["preamble"]);
						foreach (JObject choice in Choices(property))
						{
							bool skip = false;
							if (choice["conditionTree"] != null)
							{
								var result = Evaluate(choice["conditionTree"]);
								skip = result.Bool != true;
							}

							if (skip)
							{
								continue;
							}

							var configVal = (config.Value as JArray)?.Cast<JObject>().FirstOrDefault(v => JsString(v["key"]) == (string)choice["key"]);
							JToken val = configVal?["value"];
							if (!IsTruthy(val))
							{
								val = GetDefaultValue(choice["defaultValue"]);
							}

							var valText = IsTruthy(val) ? JsString(val) : "";
							configKey += (string)choice["key"] + valText + ":";
							var defineLine = ((string)choice["defineLine"]).Replace("{0}", valText);
							defines.AddRange(defineLine.Split('\n'));
							AddLines(defines, choice["additionalLines"]);
						}

						AddPostamble(defines, property, null);
					}
					else
					{
						var propertyValue = Choices(property).FirstOrDefault(c => (string)c["key"] == JsString(config.Value));
						if (propertyValue == null)
						{
							// A saved answer that the current steps no longer offer, e.g. because a newer
							// steps.json was downloaded after the questions were answered. BuildAsync reports
							// this message in the flasher's status line; the user has to redo the config.
							throw new InvalidOperationException("The saved answer '" + config.Value + "' for '" + config.Variable + "' is not offered by the current configuration steps; they may have changed since they were answered. Use Redo Config in the firmware dialog and answer the questions again.");
						}
						configKey += ":" + (string)propertyValue["key"];
						if (property["preamble"] is JArray preamble)
						{
							defines.AddRange(preamble.Select(pre => ReplaceFirst((string)pre, "{v}", JsString(propertyValue["value"]))));
						}

						if (IsTruthy(property["define"]))
						{
							defines.Add("#define " + (string)property["define"] + " " + JsString(propertyValue["defineValue"]));
						}

						if (IsTruthy(propertyValue["defineLine"]))
						{
							defines.Add((string)propertyValue["defineLine"]);
						}

						AddLines(defines, propertyValue["additionalLines"]);
						AddPostamble(defines, property, JsString(propertyValue["defineValue"]));
					}
				}

				defines.Add("");
			}

			defines.Insert(Math.Min(5, defines.Count), "// Unique ConfigKey: " + configKey);
			defines.Add("///////////////////////");
			defines.Add("// Debug settings");
			defines.Add("#define DEBUG_LEVEL (DEBUG_NONE)");
			defines.Add("");
			return defines;
		}

		/// <summary>The file content exactly as the website's Download button writes it.</summary>
		public string GenerateFileContent()
		{
			return string.Concat(GenerateLines().Select(l => l + "\n"));
		}

		private void AddPostamble(List<string> defines, JObject property, string defineValue)
		{
			if (!(property["postamble"] is JArray postamble))
			{
				return;
			}

			foreach (JObject entry in postamble)
			{
				bool output = true;
				if (entry["conditionTree"] != null && Evaluate(entry["conditionTree"]).Bool == false)
				{
					output = false;
				}

				if (output && entry["literal"] is JArray literal)
				{
					defines.AddRange(literal.Select(l => defineValue == null ? (string)l : ReplaceFirst((string)l, "{v}", defineValue)));
				}
			}
		}

		private static void AddLines(List<string> defines, JToken lines)
		{
			if (lines is JArray array)
			{
				defines.AddRange(array.Select(l => (string)l));
			}
		}

		private void PushHistory(JObject answer)
		{
			_history.Push(new HistoryEntry
			{
				Answer = answer,
				StepIndex = StepIndex,
				ConfigurationBefore = _configuration.Select(c => new ConfigEntry { Variable = c.Variable, Value = c.Value.DeepClone() }).ToList()
			});
		}

		private void Advance()
		{
			var steps = Steps;
			int index = StepIndex + 1;
			while (index < steps.Count)
			{
				var nextStep = steps[index];
				bool skip;
				if (FindConfig((string)nextStep["variable"]) != null)
				{
					skip = true;
				}
				else if (nextStep["conditionTree"] != null)
				{
					var result = Evaluate(nextStep["conditionTree"]);
					skip = result.Skip || result.Bool != true;
				}
				else
				{
					skip = nextStep["control"] == null;
				}

				if (!skip)
				{
					StepIndex = index;
					return;
				}

				index++;
			}

			StepIndex = index;
			IsComplete = true;
		}

		private JObject StepAt(int index, List<ConfigEntry> configuration)
		{
			var tracker = configuration.FirstOrDefault(c => c.Variable == "tracker")?.Value?.ToString() ?? "OAT";
			var steps = _schema["trackers"][tracker] as JArray;
			return steps != null && index >= 0 && index < steps.Count ? (JObject)steps[index] : null;
		}

		private ConfigEntry FindConfig(string variable)
		{
			return _configuration.FirstOrDefault(c => c.Variable == variable);
		}

		private static IEnumerable<JObject> Choices(JObject step)
		{
			return (step["control"]?["choices"] as JArray)?.Cast<JObject>() ?? Enumerable.Empty<JObject>();
		}

		private EvalResult Evaluate(JToken expr)
		{
			if (expr["lhs"] is JObject)
			{
				var lhs = Evaluate(expr["lhs"]);
				if (lhs.Skip)
				{
					return lhs;
				}

				if (!(expr["rhs"] is JObject))
				{
					throw new InvalidOperationException("rhs should be expression!");
				}

				var rhs = Evaluate(expr["rhs"]);
				if (rhs.Skip)
				{
					return rhs;
				}

				bool l = lhs.Bool == true, r = rhs.Bool == true;
				switch ((string)expr["op"])
				{
					case "AND": return new EvalResult { Bool = l && r };
					case "OR": return new EvalResult { Bool = l || r };
					case "XOR": return new EvalResult { Bool = l != r };
					default: throw new InvalidOperationException("Unknown operator " + expr["op"]);
				}
			}

			return EvaluateLiteral(expr);
		}

		private EvalResult EvaluateLiteral(JToken expr)
		{
			var lhs = (string)expr["lhs"];
			var variable = lhs.StartsWith("$") ? lhs.Substring(1) : lhs;
			var conf = FindConfig(variable);
			if (conf == null)
			{
				return new EvalResult { Skip = true };
			}

			var rhs = (string)expr["rhs"];
			var value = conf.Value is JValue ? JsString(conf.Value) : null;
			switch ((string)expr["op"])
			{
				case "IN":
					return new EvalResult { Bool = rhs.Substring(1, rhs.Length - 2).Split(',').Contains(value) };
				case "NOTIN":
					return new EvalResult { Bool = !rhs.Substring(1, rhs.Length - 2).Split(',').Contains(value) };
				case "==":
				case "EQ":
					return new EvalResult { Bool = value == rhs };
				case "!=":
				case "NEQ":
					return new EvalResult { Bool = value != rhs };
				default:
					throw new InvalidOperationException("Unknown operator " + expr["op"]);
			}
		}

		/// <summary>Resolves "{Defaults.Group.variable}" to Defaults[Group][value of variable], like the website.</summary>
		private JToken GetDefaultValue(JToken val)
		{
			if (val != null && val.Type == JTokenType.String)
			{
				var text = (string)val;
				if (text.StartsWith("{") && text.EndsWith("}"))
				{
					var words = text.Substring(1, text.Length - 2).Split('.');
					var config = words.Length > 2 ? FindConfig(words[2]) : null;
					if (config != null && words[0] == "Defaults")
					{
						return _schema["defaults"]?[words[1]]?[JsString(config.Value)];
					}
				}
			}

			return val;
		}

		private static bool IsTruthy(JToken token)
		{
			if (token == null)
			{
				return false;
			}

			switch (token.Type)
			{
				case JTokenType.Null:
				case JTokenType.Undefined:
					return false;
				case JTokenType.String:
					return ((string)token).Length > 0;
				case JTokenType.Integer:
					return (long)token != 0;
				case JTokenType.Float:
					var d = (double)token;
					return d != 0 && !double.IsNaN(d);
				case JTokenType.Boolean:
					return (bool)token;
				default:
					return true;
			}
		}

		/// <summary>Converts a value to text the way JavaScript string concatenation does.</summary>
		private static string JsString(JToken token)
		{
			if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
			{
				return "undefined";
			}

			switch (token.Type)
			{
				case JTokenType.Float:
					return ((double)token).ToString("R", CultureInfo.InvariantCulture);
				case JTokenType.Integer:
					return ((long)token).ToString(CultureInfo.InvariantCulture);
				case JTokenType.Boolean:
					return (bool)token ? "true" : "false";
				default:
					return token.ToString();
			}
		}

		private static string ReplaceFirst(string text, string search, string replacement)
		{
			int pos = text.IndexOf(search, StringComparison.Ordinal);
			return pos < 0 ? text : text.Substring(0, pos) + replacement + text.Substring(pos + search.Length);
		}
	}
}
