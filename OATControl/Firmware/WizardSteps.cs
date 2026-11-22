using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OATControl.Firmware
{
	/// <summary>
	/// The wizard step definitions: downloaded from config.openastrotech.com (which generates them from its own step
	/// files), else the last good download, else the copy built into OATControl.
	/// </summary>
	public static class WizardSteps
	{
		public static readonly string Url = "https://config.openastrotech.com/steps.json";
		public const int SupportedSchemaVersion = 1; // the site bumps it when the format changes in a way this code can't handle

		private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(5);

		private static string CacheFile => Path.Combine(FirmwareBuilder.WorkDirectory, "steps.json");

		/// <summary>Never throws: falls back to the embedded copy. Source describes where the steps came from, for the user.</summary>
		public static async Task<(JObject Schema, string Source)> LoadAsync(Action<string> log)
		{
			try
			{
				string json;
				using (var cts = new CancellationTokenSource(DownloadTimeout))
				using (var response = await FirmwareBuilder.HttpClient.GetAsync(Url, cts.Token).ConfigureAwait(false))
				{
					response.EnsureSuccessStatusCode();
					json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				}

				var schema = Validate(json);
				SaveCopy(json, log);
				return (schema, "downloaded from config.openastrotech.com");
			}
			catch (Exception ex)
			{
				log($"Could not use the wizard steps from {Url}: {ex.Message}");
			}

			try
			{
				var built = File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location);
				if (File.Exists(CacheFile) && File.GetLastWriteTime(CacheFile) > built)
				{
					var schema = Validate(File.ReadAllText(CacheFile));
					return (schema, $"saved copy from config.openastrotech.com, {File.GetLastWriteTime(CacheFile):yyyy-MM-dd}");
				}
			}
			catch (Exception ex)
			{
				log($"Could not use the saved wizard steps in {CacheFile}: {ex.Message}");
			}

			return (FirmwareConfigWizard.LoadEmbeddedSchema(), "copy built into OATControl");
		}

		/// <summary>Saves a good download for offline use. A failure (disk, virus scanner, a second OATControl) is only logged.</summary>
		private static void SaveCopy(string json, Action<string> log)
		{
			var temp = CacheFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				Directory.CreateDirectory(FirmwareBuilder.WorkDirectory);
				File.WriteAllText(temp, json);
				if (File.Exists(CacheFile))
				{
					File.Replace(temp, CacheFile, null);
				}
				else
				{
					File.Move(temp, CacheFile);
				}
			}
			catch (Exception ex)
			{
				log($"Could not save the wizard steps to {CacheFile}: {ex.Message}");
				try
				{
					File.Delete(temp);
				}
				catch (Exception)
				{
					// Left for the next successful save to not matter: it uses a new name.
				}
			}
		}

		/// <summary>
		/// Parses the file and checks that the wizard can use it: the supported schema version, and one complete run
		/// through each tracker the first step offers (first choice, default text values) up to a generated
		/// configuration. Other branches aren't run; the dialog falls back to the built-in copy if one fails later.
		/// Throws if not. The site answers unknown paths with its HTML page, which fails the parse here.
		/// </summary>
		public static JObject Validate(string json)
		{
			var schema = JObject.Parse(json);
			var version = (int?)schema["schemaVersion"];
			if (version != SupportedSchemaVersion)
			{
				throw new InvalidDataException($"Schema version {version?.ToString() ?? "missing"} is not supported (this OATControl supports {SupportedSchemaVersion}).");
			}

			var trackers = schema["trackers"] as JObject;
			if (trackers == null || trackers.Count == 0)
			{
				throw new InvalidDataException("No trackers in the file.");
			}

			// Only the trackers the first step offers are reachable; anything else in the file is ignored.
			var probe = new FirmwareConfigWizard(schema);
			var trackerKeys = probe.CurrentStep == null ? new List<string>() : probe.EnabledChoices(probe.CurrentStep).Select(c => (string)c["key"]).ToList();
			if (trackerKeys.Count == 0)
			{
				throw new InvalidDataException("The first step offers no tracker.");
			}

			foreach (var tracker in trackerKeys)
			{
				if (!(trackers[tracker] is JArray steps) || steps.Count == 0)
				{
					throw new InvalidDataException($"No steps for {tracker}.");
				}

				var wizard = new FirmwareConfigWizard(schema);
				wizard.Select(tracker);
				for (int i = 0; !wizard.IsComplete; i++)
				{
					var step = wizard.CurrentStep;
					if (step == null || i > 500)
					{
						throw new InvalidDataException($"The {tracker} steps don't reach the end.");
					}

					if ((string)step["control"]["type"] == "textinput")
					{
						wizard.SubmitText(new Dictionary<string, string>());
					}
					else
					{
						var choice = wizard.EnabledChoices(step).FirstOrDefault() ?? throw new InvalidDataException($"Step {step["id"]} of {tracker} has no choice to make.");
						wizard.Select((string)choice["key"]);
					}
				}

				wizard.GenerateFileContent();
			}

			return schema;
		}
	}
}
