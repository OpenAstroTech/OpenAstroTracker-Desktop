using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OATControl.Firmware
{
	/// <summary>
	/// Finds PlatformIO, or installs a private copy (Python + PlatformIO, pinned like OATFWGUI, and Git if it isn't on
	/// the PATH: the firmware's libraries are git URLs) in %LocalAppData%\OATFW\tools. Existing installs are never modified.
	/// </summary>
	public static class FlashTools
	{
		public static readonly Version MinPlatformIOVersion = new Version(6, 1, 16); // the version OATFWGUI pins

		// The official python.org build, as published on NuGet (a zip that includes pip and venv).
		private const string PythonUrl = "https://www.nuget.org/api/v2/package/python/3.11.9";
		private const string PythonSha256 = "9283876d58c017e0e846f95b490da3bca0fc0a6ee1134b2870677cfb7eec3c67";
		private const long PythonDownloadMB = 17;
		private const long PlatformIODownloadMB = 2; // PlatformIO 6.1.16 and its dependencies as wheels

		// Portable Git from Git for Windows.
		private const string GitUrl = "https://github.com/git-for-windows/git/releases/download/v2.56.1.windows.1/MinGit-2.56.1-64-bit.zip";
		private const string GitSha256 = "4a92850b6e917da4fd010669b614b55dccf5b0ec333a534cc4be4700dfefdafd";
		private const long GitDownloadMB = 38;

		public static string ToolsDirectory => Path.Combine(FirmwareBuilder.WorkDirectory, "tools");
		private static string PythonDirectory => Path.Combine(ToolsDirectory, "python");
		private static string PrivatePython => Path.Combine(PythonDirectory, "python.exe");
		private static string PrivatePio => Path.Combine(PythonDirectory, "Scripts", "pio.exe");
		private static string GitDirectory => Path.Combine(ToolsDirectory, "git");
		private static string PrivateGitCmd => Path.Combine(GitDirectory, "cmd");

		/// <summary>The folder to add to PlatformIO's PATH for the private Git, or null if Git is on the PATH or not installed here.</summary>
		public static string GitPathOverride => !GitOnPath && File.Exists(Path.Combine(PrivateGitCmd, "git.exe")) ? PrivateGitCmd : null;

		private static bool GitOnPath => OnPath("git.exe").Any();

		/// <summary>The full paths of <paramref name="exe"/> in the PATH folders; entries that aren't valid paths are skipped.</summary>
		private static List<string> OnPath(string exe)
		{
			var found = new List<string>();
			foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
			{
				try
				{
					var path = Path.Combine(dir.Trim().Trim('"'), exe);
					if (dir.Trim().Length > 0 && File.Exists(path))
					{
						found.Add(path);
					}
				}
				catch (ArgumentException)
				{
					// A PATH entry with invalid characters.
				}
			}

			return found;
		}

		public static bool NeedsGit => !GitOnPath && !File.Exists(Path.Combine(PrivateGitCmd, "git.exe"));

		/// <summary>
		/// The compiler download per platform: platform keyword, the package whose folder shows it is installed, MB. Sizes are
		/// the registry sizes of what pio pkg install fetched for firmware v1.14 (2026-11-20). RP2040 is not measured.
		/// </summary>
		private static readonly (string Platform, string Package, int MB)[] CompilerSizes =
		{
			("atmelavr", "toolchain-atmelavr", 55), // toolchain 50.5, framework 1.3, scons 0.5, libraries
			("espressif32", "framework-arduinoespressif32", 370), // framework 246, toolchain 121, esptool and scons 1, libraries
		};

		/// <summary>
		/// The compiler packages go to the default PlatformIO folder if it exists (reusing what an existing install
		/// downloaded), otherwise into %LocalAppData%\OATFW\core. Once that folder exists it stays in use, so compilers
		/// aren't downloaded again when a PlatformIO install appears later. A PLATFORMIO_CORE_DIR set by the user wins.
		/// </summary>
		public static string CoreDirectoryOverride
		{
			get
			{
				var privateCore = Path.Combine(FirmwareBuilder.WorkDirectory, "core");
				if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLATFORMIO_CORE_DIR")) || (Directory.Exists(DefaultCoreDirectory) && !Directory.Exists(privateCore)))
				{
					return null;
				}

				return privateCore;
			}
		}

		private static string DefaultCoreDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".platformio");

		private static string CoreDirectory => Environment.GetEnvironmentVariable("PLATFORMIO_CORE_DIR") is string dir && dir.Length > 0 ? dir : CoreDirectoryOverride ?? DefaultCoreDirectory;

		/// <summary>
		/// Returns the first pio.exe of at least <see cref="MinPlatformIOVersion"/>: on the PATH, in the default
		/// PlatformIO install (also used by the VS Code extension), or the private copy. Null if there is none.
		/// </summary>
		public static async Task<string> FindPlatformIOAsync(Action<string> log)
		{
			var candidates = OnPath("pio.exe");
			candidates.Add(Path.Combine(DefaultCoreDirectory, "penv", "Scripts", "pio.exe"));
			candidates.Add(PrivatePio);

			foreach (var pio in candidates.Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists))
			{
				var version = await GetVersionAsync(pio).ConfigureAwait(false);
				if (version != null && version >= MinPlatformIOVersion)
				{
					return pio;
				}

				log($"Not using {pio}: version {version?.ToString() ?? "unknown"}, {MinPlatformIOVersion} or newer is needed");
			}

			return null;
		}

		private static async Task<Version> GetVersionAsync(string pio)
		{
			try
			{
				var output = new StringBuilder();
				using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
				{
					await FirmwareBuilder.RunAsync(pio, "--version", null, s => output.AppendLine(s), null, cts.Token).ConfigureAwait(false);
				}

				var match = Regex.Match(output.ToString(), @"version (\d+\.\d+\.\d+)");
				return match.Success ? new Version(match.Groups[1].Value) : null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>The approximate download in MB to install what is missing, and whether the compiler size is known.</summary>
		public static int DownloadSizeMB(string existingPio, string firmwareDir, PioEnvironment env, out bool compilerKnown)
		{
			long mb = 0;
			if (existingPio == null && !File.Exists(PrivatePython))
			{
				mb += PythonDownloadMB;
			}

			if (existingPio == null && !File.Exists(PrivatePio))
			{
				mb += PlatformIODownloadMB;
			}

			if (NeedsGit)
			{
				mb += GitDownloadMB;
			}

			compilerKnown = true;
			if (env != null)
			{
				var platform = ReadPlatform(firmwareDir, env);
				var entry = CompilerSizes.FirstOrDefault(c => platform != null && platform.IndexOf(c.Platform, StringComparison.OrdinalIgnoreCase) >= 0);
				if (entry.Package == null)
				{
					compilerKnown = false;
				}
				else if (!Directory.Exists(Path.Combine(CoreDirectory, "packages", entry.Package)))
				{
					mb += entry.MB;
				}
			}

			return (int)mb;
		}

		/// <summary>The env's "platform =" value, falling back to the common [env] section. Null if not found.</summary>
		private static string ReadPlatform(string firmwareDir, PioEnvironment env)
		{
			try
			{
				string section = null, common = null, own = null;
				foreach (var rawLine in File.ReadAllLines(Path.Combine(firmwareDir, "platformio.ini")))
				{
					var line = rawLine.Trim();
					var header = Regex.Match(line, @"^\[(.+)\]$");
					if (header.Success)
					{
						section = header.Groups[1].Value;
						continue;
					}

					var value = Regex.Match(line, @"^platform\s*=\s*(\S+)");
					if (value.Success)
					{
						if (section == "env")
						{
							common = value.Groups[1].Value;
						}
						else if (section == "env:" + env.Name)
						{
							own = value.Groups[1].Value;
						}
					}
				}

				return own ?? common;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Installs the private Python and PlatformIO unless <paramref name="existingPio"/> is given, Git if missing, then the
		/// compiler and libraries for <paramref name="env"/>. Returns the pio.exe to use. Hot patches must already be applied
		/// to a downloaded release.
		/// </summary>
		public static async Task<string> InstallAsync(string existingPio, string firmwareDir, PioEnvironment env, Action<string> log, IProgress<double> progress, CancellationToken token)
		{
			Directory.CreateDirectory(ToolsDirectory);
			if (existingPio == null && !File.Exists(PrivatePython))
			{
				// The package holds Python in its tools folder; the rest is NuGet metadata.
				await InstallZipAsync("Python", PythonUrl, PythonSha256, PythonDownloadMB, "tools/", PythonDirectory, log, progress, token).ConfigureAwait(false);
			}

			if (NeedsGit)
			{
				await InstallZipAsync("Git", GitUrl, GitSha256, GitDownloadMB, "", GitDirectory, log, progress, token).ConfigureAwait(false);
			}

			progress.Report(-1);
			token.ThrowIfCancellationRequested();
			var pio = existingPio ?? PrivatePio;
			if (existingPio == null && (!File.Exists(PrivatePio) || await GetVersionAsync(PrivatePio).ConfigureAwait(false) == null))
			{
				// -I and --isolated: ignore PYTHON* variables, the user's site packages and the user's pip configuration.
				var args = $"-I -m pip --isolated install --disable-pip-version-check --no-warn-script-location platformio=={MinPlatformIOVersion}";
				log($"> python {args}");
				var exitCode = await FirmwareBuilder.RunAsync(PrivatePython, args, null, log, log, token).ConfigureAwait(false);
				token.ThrowIfCancellationRequested();
				if (exitCode != 0 || !File.Exists(PrivatePio))
				{
					throw new InvalidOperationException($"Installing PlatformIO failed (exit code {exitCode}). See the log.");
				}
			}

			token.ThrowIfCancellationRequested();
			var pkgArgs = $"pkg install --environment {env.Name} --project-dir {FirmwareBuilder.QuoteArgument(firmwareDir)}";
			log($"> pio {pkgArgs}");
			var pkgExit = await FirmwareBuilder.RunAsync(pio, pkgArgs, null, log, log, token).ConfigureAwait(false);
			token.ThrowIfCancellationRequested();
			if (pkgExit != 0)
			{
				throw new InvalidOperationException($"Installing the compiler for {env.Name} failed (exit code {pkgExit}). See the log.");
			}

			return pio;
		}

		/// <summary>Downloads a zip, checks its hash and extracts the entries under <paramref name="prefix"/> to <paramref name="targetDir"/>.</summary>
		private static async Task InstallZipAsync(string name, string url, string sha256, long expectedMB, string prefix, string targetDir, Action<string> log, IProgress<double> progress, CancellationToken token)
		{
			var package = targetDir + ".zip";
			var extractDir = targetDir + ".tmp";
			log($"Downloading {name} from {url}");
			progress.Report(0);
			try
			{
				using (var response = await FirmwareBuilder.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
				{
					response.EnsureSuccessStatusCode();
					var total = response.Content.Headers.ContentLength ?? expectedMB * 1024 * 1024;
					using (var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
					using (var file = File.Create(package))
					{
						var buffer = new byte[81920];
						long done = 0;
						int read;
						while ((read = await body.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
						{
							await file.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
							done += read;
							progress.Report(Math.Min(1.0, (double)done / total));
						}
					}
				}

				using (var file = File.OpenRead(package))
				using (var sha = SHA256.Create())
				{
					var hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
					if (hash != sha256)
					{
						throw new InvalidDataException($"The {name} download is corrupt or was changed (SHA-256 {hash}, expected {sha256}).");
					}
				}

				token.ThrowIfCancellationRequested();
				log($"Extracting {name}");
				progress.Report(-1);
				if (Directory.Exists(extractDir))
				{
					Directory.Delete(extractDir, true);
				}

				using (var zip = ZipFile.OpenRead(package))
				{
					var root = Path.GetFullPath(extractDir) + Path.DirectorySeparatorChar;
					foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith(prefix) && e.Name.Length > 0))
					{
						token.ThrowIfCancellationRequested();
						var target = Path.GetFullPath(Path.Combine(extractDir, entry.FullName.Substring(prefix.Length)));
						if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
						{
							throw new InvalidDataException($"Unexpected path in the {name} package: {entry.FullName}");
						}

						Directory.CreateDirectory(Path.GetDirectoryName(target));
						entry.ExtractToFile(target, true);
					}
				}

				if (Directory.Exists(targetDir))
				{
					Directory.Delete(targetDir, true); // an incomplete earlier install
				}

				Directory.Move(extractDir, targetDir); // only a complete install ends up at its final path
				log($"{name} installed to {targetDir}");
			}
			finally
			{
				try
				{
					File.Delete(package);
					if (Directory.Exists(extractDir))
					{
						Directory.Delete(extractDir, true);
					}
				}
				catch (Exception ex)
				{
					// For example a virus scanner still holding a file; must not replace the real error or the cancel.
					log($"Could not remove temporary files: {ex.Message}");
				}
			}
		}
	}
}
