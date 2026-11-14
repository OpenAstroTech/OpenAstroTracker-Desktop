using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OATControl.Firmware
{
	public class FirmwareRelease
	{
		public string Name { get; set; }
		public string ZipUrl { get; set; }
		public bool IsPrerelease { get; set; }
		public override string ToString() => Name;
	}

	public class PioEnvironment
	{
		public string Name { get; set; }
		public string Board { get; set; } // e.g. BOARD_AVR_RAMPS, from "-D BOARD=..." in the env's build flags
		public bool IsAvr => Name.IndexOf("mksgenl", StringComparison.OrdinalIgnoreCase) >= 0 || Name.IndexOf("ramps", StringComparison.OrdinalIgnoreCase) >= 0;
		public override string ToString() => Name;
	}

	public class SerialPortEntry
	{
		public string Port { get; set; }
		public string Description { get; set; }
		public override string ToString() => string.IsNullOrEmpty(Description) || Description == "n/a" ? Port : $"{Port} - {Description}";
	}

	/// <summary>
	/// Downloads OpenAstroTracker firmware and builds/uploads it with PlatformIO. This is a port of the flow in OATFWGUI
	/// (https://github.com/OpenAstroTech/OATFWGUI), including its hot patches for older firmware releases.
	/// </summary>
	public static class FirmwareBuilder
	{
		private const string ReleasesUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/releases";
		private const string BranchZipUrl = "https://github.com/OpenAstroTech/OpenAstroTracker-Firmware/archive/refs/heads/{0}.zip";

		private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

		static FirmwareBuilder()
		{
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "OATControl");
		}

		/// <summary>Kept short on purpose: PlatformIO builds exceed the 260 character Windows path limit in deep folders.</summary>
		public static string WorkDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OATFW");

		public static string FirmwareDirectory => Path.Combine(WorkDirectory, "fw");

		public static async Task<List<FirmwareRelease>> GetReleasesAsync()
		{
			var releases = new List<FirmwareRelease>();
			var json = await _httpClient.GetStringAsync(ReleasesUrl).ConfigureAwait(false);
			foreach (var release in JArray.Parse(json))
			{
				var name = (string)release["name"];
				releases.Add(new FirmwareRelease
				{
					Name = string.IsNullOrWhiteSpace(name) ? (string)release["tag_name"] : name,
					ZipUrl = (string)release["zipball_url"],
					IsPrerelease = (bool?)release["prerelease"] ?? false,
				});
			}

			releases.Add(new FirmwareRelease { Name = "develop (unreleased)", ZipUrl = string.Format(BranchZipUrl, "develop"), IsPrerelease = true });
			releases.Add(new FirmwareRelease { Name = "oae-fw (unreleased)", ZipUrl = string.Format(BranchZipUrl, "oae-fw"), IsPrerelease = true });
			return releases;
		}

		/// <summary>Downloads and extracts the firmware, replacing any previous download. Returns the firmware folder.</summary>
		public static async Task<string> DownloadAsync(FirmwareRelease release, Action<string> log, CancellationToken token)
		{
			Directory.CreateDirectory(WorkDirectory);
			var zipPath = Path.Combine(WorkDirectory, "fw.zip");
			var extractDir = Path.Combine(WorkDirectory, "extract");

			log($"Downloading {release.Name} from {release.ZipUrl}");
			using (var response = await _httpClient.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();
				using (var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				using (var file = File.Create(zipPath))
				{
					await body.CopyToAsync(file, 81920, token).ConfigureAwait(false);
				}
			}

			token.ThrowIfCancellationRequested();
			DeleteDirectory(extractDir);
			DeleteDirectory(FirmwareDirectory);
			log("Extracting firmware");
			ZipFile.ExtractToDirectory(zipPath, extractDir);

			var topDirs = Directory.GetDirectories(extractDir);
			if (topDirs.Length != 1)
			{
				throw new InvalidDataException("Unexpected firmware archive layout: expected a single top level folder.");
			}

			Directory.Move(topDirs[0], FirmwareDirectory);
			DeleteDirectory(extractDir);
			File.Delete(zipPath);
			log($"Firmware extracted to {FirmwareDirectory}");
			return FirmwareDirectory;
		}

		public static List<PioEnvironment> ReadEnvironments(string firmwareDir)
		{
			var environments = new List<PioEnvironment>();
			PioEnvironment current = null;
			foreach (var rawLine in File.ReadAllLines(Path.Combine(firmwareDir, "platformio.ini")))
			{
				var line = rawLine.Trim();
				var section = Regex.Match(line, @"^\[(.+)\]$");
				if (section.Success)
				{
					var name = section.Groups[1].Value;
					current = name.StartsWith("env:") && name != "env:native" ? new PioEnvironment { Name = name.Substring(4) } : null;
					if (current != null)
					{
						environments.Add(current);
					}

					continue;
				}

				var board = Regex.Match(line, @"-D\s*BOARD=(\w+)");
				if (current != null && board.Success && !line.StartsWith(";"))
				{
					current.Board = board.Groups[1].Value;
				}
			}

			return environments;
		}

		/// <summary>Patches for old firmware releases that no longer build with current PlatformIO (from OATFWGUI).</summary>
		public static void ApplyHotPatches(string firmwareDir, PioEnvironment env, Action<string> log)
		{
			var iniPath = Path.Combine(firmwareDir, "platformio.ini");
			var original = File.ReadAllText(iniPath);
			var lines = original.Split('\n');
			var badGitTag = new Regex(@"(^[^;\n]+github\.com.+)@");
			var badSsd1306 = new Regex(@"(ClutchplateDude/esp8266-oled-ssd1306\s*#\s*4\.6\.0)");

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				if (badGitTag.IsMatch(line))
				{
					line = badGitTag.Replace(line, "$1#");
				}

				if (env.IsAvr && line.TrimEnd('\r') == "platform = atmelavr")
				{
					line = "platform = atmelavr@4.2.0" + (line.EndsWith("\r") ? "\r" : "");
				}

				if (badSsd1306.IsMatch(line))
				{
					line = badSsd1306.Replace(line, "ClutchplateDude/esp8266-oled-ssd1306#4.6.2");
				}

				if (line != lines[i])
				{
					log($"Patching platformio.ini: '{lines[i].TrimEnd('\r')}' -> '{line.TrimEnd('\r')}'");
					lines[i] = line;
				}
			}

			var patched = string.Join("\n", lines);
			if (patched != original)
			{
				File.WriteAllText(iniPath, patched);
			}
		}

		public static void WriteConfiguration(string firmwareDir, string content)
		{
			File.WriteAllText(Path.Combine(firmwareDir, "Configuration_local.hpp"), content, new UTF8Encoding(false));
		}

		/// <summary>Finds pio.exe on the PATH or in the default PlatformIO install (also used by the VS Code extension).</summary>
		public static string FindPlatformIO()
		{
			var candidates = (Environment.GetEnvironmentVariable("PATH") ?? "")
				.Split(Path.PathSeparator)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Select(p => Path.Combine(p.Trim('"'), "pio.exe"))
				.ToList();
			candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".platformio", "penv", "Scripts", "pio.exe"));
			return candidates.FirstOrDefault(File.Exists);
		}

		public static async Task<List<SerialPortEntry>> ListSerialPortsAsync(string pio)
		{
			var output = new StringBuilder();
			var exitCode = await RunAsync(pio, "device list --serial --json-output", null, s => output.AppendLine(s), null, CancellationToken.None).ConfigureAwait(false);
			if (exitCode == 0)
			{
				try
				{
					return JArray.Parse(output.ToString())
						.Select(p => new SerialPortEntry { Port = (string)p["port"], Description = (string)p["description"] })
						.ToList();
				}
				catch (Exception)
				{
					// Fall back to the plain port names below.
				}
			}

			return System.IO.Ports.SerialPort.GetPortNames().Distinct().Select(p => new SerialPortEntry { Port = p }).ToList();
		}

		/// <summary>Builds the firmware and, if <paramref name="uploadPort"/> is set, uploads it to the board.</summary>
		public static Task<int> BuildAsync(string pio, string firmwareDir, PioEnvironment env, string uploadPort, Action<string> log, CancellationToken token)
		{
			var args = $"run --environment {env.Name} --project-dir \"{firmwareDir}\"";
			if (!string.IsNullOrEmpty(uploadPort))
			{
				args += $" --target upload --upload-port {uploadPort}";
			}

			var environment = new Dictionary<string, string>();
			if (!env.IsAvr && !EnvExtraScripts(firmwareDir).Contains("iprefix"))
			{
				// The ESP32 framework passes too many -I flags for the Windows command line limit; OATFWGUI's
				// pre-script shortens them. Assumes anything that isn't AVR is ESP32, like OATFWGUI does.
				var script = Path.Combine(WorkDirectory, "pre_script_esp32_iprefix.py");
				WriteEmbeddedResource("pre_script_esp32_iprefix.py", script);
				environment["PLATFORMIO_EXTRA_SCRIPTS"] = "pre:" + script;
			}

			log($"> pio {args}");
			return RunAsync(pio, args, environment, log, log, token);
		}

		private static string EnvExtraScripts(string firmwareDir)
		{
			var text = File.ReadAllText(Path.Combine(firmwareDir, "platformio.ini"));
			var match = Regex.Match(text, @"^\[env\]\s*$(.*?)^\[", RegexOptions.Multiline | RegexOptions.Singleline);
			return match.Success ? match.Groups[1].Value : string.Empty;
		}

		private static void WriteEmbeddedResource(string suffix, string path)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var name = assembly.GetManifestResourceNames().First(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
			using (var stream = assembly.GetManifestResourceStream(name))
			using (var file = File.Create(path))
			{
				stream.CopyTo(file);
			}
		}

		private static Task<int> RunAsync(string exe, string args, IDictionary<string, string> environment, Action<string> stdout, Action<string> stderr, CancellationToken token)
		{
			var psi = new ProcessStartInfo(exe, args)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
			};
			psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
			psi.EnvironmentVariables["PLATFORMIO_DISABLE_PROGRESSBAR"] = "true";
			if (environment != null)
			{
				foreach (var kv in environment)
				{
					psi.EnvironmentVariables[kv.Key] = kv.Value;
				}
			}

			var tcs = new TaskCompletionSource<int>();
			var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout?.Invoke(e.Data); };
			process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr?.Invoke(e.Data); };
			process.Exited += (s, e) =>
			{
				process.WaitForExit(); // flushes the async output readers
				tcs.TrySetResult(process.ExitCode);
			};

			process.Start();
			var pid = process.Id;
			tcs.Task.ContinueWith(t => process.Dispose());
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			token.Register(() =>
			{
				if (!tcs.Task.IsCompleted)
				{
					// Kill the whole tree: pio starts the compiler and uploader as child processes.
					Process.Start(new ProcessStartInfo("taskkill", $"/T /F /PID {pid}") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
				}
			});

			return tcs.Task;
		}

		private static void DeleteDirectory(string path)
		{
			if (!Directory.Exists(path))
			{
				return;
			}

			// Git checkouts inside .pio contain read-only files, which Directory.Delete refuses to remove.
			foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(file, FileAttributes.Normal);
			}

			Directory.Delete(path, true);
		}
	}
}
