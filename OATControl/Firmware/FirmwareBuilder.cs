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
		public string LocalPath { get; set; } // set for a custom firmware folder, which is built in place instead of downloaded
		public string CommitSha { get; set; } // the commit that was downloaded, known after DownloadAsync
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
		private const string BranchesUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/branches?per_page=100";
		private const string BranchZipUrl = "https://github.com/OpenAstroTech/OpenAstroTracker-Firmware/archive/refs/heads/{0}.zip";

		public static string DevelopZipUrl => string.Format(BranchZipUrl, "develop");

		private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

		static FirmwareBuilder()
		{
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "OATControl");
		}

		/// <summary>Kept short on purpose: PlatformIO builds exceed the 260 character Windows path limit in deep folders.</summary>
		public static string WorkDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OATFW");

		public static string FirmwareDirectory => Path.Combine(WorkDirectory, "fw");

		/// <summary>
		/// Lists the latest release, the develop branch, the release before the latest and the newest V1.11 release (the
		/// wizard's "Official Versions (V1.11.x)" answer needs it). If <paramref name="all"/> is set, every release and
		/// every other branch of the firmware repository is listed as well (for testing). Branches are marked as
		/// prerelease so they are never selected automatically.
		/// </summary>
		public static async Task<List<FirmwareRelease>> GetReleasesAsync(bool all)
		{
			var json = await _httpClient.GetStringAsync(ReleasesUrl).ConfigureAwait(false);
			var releases = JArray.Parse(json).Select(release =>
			{
				var name = (string)release["name"];
				return new FirmwareRelease
				{
					Name = string.IsNullOrWhiteSpace(name) ? (string)release["tag_name"] : name,
					ZipUrl = (string)release["zipball_url"],
					IsPrerelease = (bool?)release["prerelease"] ?? false,
				};
			}).ToList();

			// GitHub lists the newest release first.
			var stable = releases.Where(r => !r.IsPrerelease).ToList();
			if (!all)
			{
				releases = stable.Take(2).ToList();
				var v111 = stable.FirstOrDefault(r => r.Name.Contains("1.11."));
				if (v111 != null && !releases.Contains(v111))
				{
					releases.Add(v111);
				}
			}

			var develop = new FirmwareRelease { Name = "Latest develop branch (unreleased)", ZipUrl = DevelopZipUrl, IsPrerelease = true };
			var latest = stable.FirstOrDefault();
			releases.Remove(latest);
			releases.Insert(0, develop);
			if (latest != null)
			{
				releases.Insert(0, latest);
			}

			if (all)
			{
				var branches = await _httpClient.GetStringAsync(BranchesUrl).ConfigureAwait(false);
				foreach (var name in JArray.Parse(branches).Select(b => (string)b["name"]).Where(n => n != "develop").OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
				{
					releases.Add(new FirmwareRelease { Name = "Branch: " + name, ZipUrl = string.Format(BranchZipUrl, name), IsPrerelease = true });
				}
			}

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
			release.CommitSha = ReadZipCommit(zipPath);
			File.Delete(zipPath);
			log($"Firmware extracted to {FirmwareDirectory}");
			return FirmwareDirectory;
		}

		/// <summary>GitHub writes the commit of an archive as the zip file's comment, which is the end of the file. Null if absent.</summary>
		private static string ReadZipCommit(string zipPath)
		{
			var bytes = new byte[40];
			using (var file = File.OpenRead(zipPath))
			{
				if (file.Length < 40)
				{
					return null;
				}

				file.Seek(-40, SeekOrigin.End);
				file.Read(bytes, 0, 40);
			}

			var text = Encoding.ASCII.GetString(bytes);
			return Regex.IsMatch(text, "^[0-9a-f]{40}$") ? text : null;
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

		/// <summary>Returns the folder's Configuration_local.hpp, or null if it has none.</summary>
		public static string ReadConfiguration(string firmwareDir)
		{
			var path = Path.Combine(firmwareDir, "Configuration_local.hpp");
			return File.Exists(path) ? File.ReadAllText(path, ConfigurationEncoding(path)) : null;
		}

		/// <summary>UTF-8, or Latin-1 for an existing file that isn't valid UTF-8, so a hand-written ANSI file survives an edit.</summary>
		private static Encoding ConfigurationEncoding(string path)
		{
			if (File.Exists(path))
			{
				var bytes = File.ReadAllBytes(path);
				if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
				{
					return new UTF8Encoding(true);
				}

				try
				{
					new UTF8Encoding(false, true).GetString(bytes);
				}
				catch (DecoderFallbackException)
				{
					return Encoding.GetEncoding(28591); // Latin-1 maps every byte to a character and back, whatever the system code page
				}
			}

			return new UTF8Encoding(false);
		}

		/// <summary>Board specific Configuration_local_*.hpp files, which the firmware prefers over Configuration_local.hpp.</summary>
		public static List<string> BoardSpecificConfigurations(string firmwareDir)
		{
			return Directory.GetFiles(firmwareDir, "Configuration_local_*.hpp").Select(Path.GetFileName).ToList();
		}

		public static void WriteConfiguration(string firmwareDir, string content)
		{
			var path = Path.Combine(firmwareDir, "Configuration_local.hpp");
			File.WriteAllText(path, content, ConfigurationEncoding(path));
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

		/// <summary>
		/// Checks that the serial port can be opened, so a busy port is reported before the build instead of by a failed
		/// upload after it. Returns null if the port is free, otherwise a message for the user.
		/// </summary>
		public static async Task<string> CheckPortFreeAsync(string portName)
		{
			// A few attempts: a program that just disconnected (such as the ASCOM driver) can take a moment to let go.
			for (int attempt = 0; ; attempt++)
			{
				try
				{
					using (var port = new System.IO.Ports.SerialPort(portName))
					{
						port.Open();
					}

					return null;
				}
				catch (UnauthorizedAccessException)
				{
					if (attempt >= 4)
					{
						return $"{portName} is in use by another program (for example NINA or PHD2 through the ASCOM driver, or a serial monitor). Close it and try again.";
					}
				}
				catch (Exception ex)
				{
					return $"{portName} can't be opened: {ex.Message}";
				}

				await Task.Delay(400).ConfigureAwait(false);
			}
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
				Directory.CreateDirectory(WorkDirectory); // not there yet when building a custom folder without any download
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
