using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;

namespace ChocolateStore
{
    class PackageCacher
    {
        public enum FileExistsBehavior
        {
            Replace,
            Skip,
            Rename
        }

        private const string INSTALL_FILE = "tools/chocolateyInstall.ps1";

        public delegate void FileHandler(string fileName);
        public delegate void DownloadFailedHandler(string url, Exception ex);

        public event FileHandler SkippingFile = delegate { };
        public event FileHandler DownloadingFile = delegate { };
        public event DownloadFailedHandler DownloadFailed = delegate { };

        public void CachePackage(string packageName, string directory, IEnumerable<Tuple<string, IEnumerable<string>>> variables)
        {
            var packageInfo = PackageInfo.Find(packageName);

            Console.WriteLine("Reading package '{0}'", packageInfo.Name);
            Console.WriteLine("package URL: '{0}'", packageInfo.Url);

            var packagePath = DownloadFile(packageInfo.Url, directory);

            using (var zip = ZipFile.Read(packagePath))
            {                
                var installFile = zip.FirstOrDefault(x => string.Equals(x.FileName, INSTALL_FILE, StringComparison.OrdinalIgnoreCase));

                if (installFile != null)
                {
                    string content = null;

                    using (var memoryStream = new MemoryStream())
                    {
                        installFile.Extract(memoryStream);
                        memoryStream.Position = 0;
                        using (var reader = new StreamReader(memoryStream, true))
                        {
                            content = reader.ReadToEnd();
                        }
                    }

                    content = CacheUrlFiles(Path.Combine(directory, packageName + "-" + packageInfo.Version), content, variables);
                    zip.UpdateEntry(INSTALL_FILE, content);
                    zip.Save();

                }
            }

            if (packageInfo.Dependencies != null)
            {
                foreach (var dep in packageInfo.Dependencies)
                {
                    CachePackage(dep, directory, variables);
                }
            }

        }

        /// <summary>
        /// Scans the content for URLs, downloads these files to the specified local directory and replaces
        /// the URLs in the content with the path of the downloaded local files.
        /// </summary>
        /// <param name="localDirectory">The directory in which the files should be stored locally</param>
        /// <param name="content">The string that should be scanned for URLs</param>
        /// <param name="variables">Variables like ${name} that must be resolved to get valid URLs. The first item in the tuple is the name of the variable. The second item of the tuple is a  list of the possible values of the variable.</param>
        /// <returns>The content string, in which all Urls are replaced by local file paths.</returns>
        private string CacheUrlFiles(string localDirectory, string content, IEnumerable<Tuple<string, IEnumerable<string>>> variables)
        {
            const string pattern = "(?<=['\"])http[\\S ]*(?=['\"])";

            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            var variablesHavingAlternatives = Variables.ResolveVariablesWithoutAlternatives(ref content, variables);
            var variablePermutations = Variables.GetVariablePermutations(variablesHavingAlternatives);

            return Regex.Replace(content, pattern, match =>
            {
                var fileName = Path.GetFileName(new Uri(match.Value).LocalPath);
                var fileNameWithVariables = Variables.GetPrefixForVariables(variablesHavingAlternatives.Select(t => t.Item1)) + fileName;

                var suffix = "";
                foreach (var permutation in variablePermutations)
                {
                    var resolvedUrl = Variables.ResolveVariables(match.Value, permutation);
                    var resolvedFileName = Variables.ResolveVariables(fileNameWithVariables, permutation);
                    DownloadFile(resolvedUrl, localDirectory, out suffix, resolvedFileName, FileExistsBehavior.Rename);
                }

                var localPath = Path.Combine(localDirectory, suffix + fileNameWithVariables);
                return localPath;
            });
        }

        /// <summary>
        /// Downloads a file from the specified URL. Skips already existing files.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="localDirectory"></param>
        /// <returns>the local path of the downloaded file</returns>
        private string DownloadFile(string url, string localDirectory)
        {
            return DownloadFile(url, localDirectory, out _, "", FileExistsBehavior.Skip);
        }

        /// <summary>
        /// Downloads a file from the specified URL.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="localDirectory">The directory into which the file should be stored.</param>
        /// <param name="suffix">The suffix added to the local file name to make the file unique.</param>
        /// <param name="localFileName">The file name to which the file should be saved. If this arguments is left empty, the file name on the server will be used.</param>
        /// <param name="fileExistsBehavior">Specifies what should be done if the file name already exists locally.</param>
        /// <returns>the local path of the downloaded file</returns>
        private string DownloadFile(string url, string localDirectory, out string suffix, string localFileName = "", FileExistsBehavior fileExistsBehavior = FileExistsBehavior.Skip)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                if (String.IsNullOrEmpty(localFileName))
                    localFileName = Path.GetFileName(response.ResponseUri.LocalPath);
                var localfilePath = Path.Combine(localDirectory, localFileName);

                suffix = "";

                if (File.Exists(localfilePath))
                {
                    switch (fileExistsBehavior)
                    {
                        case FileExistsBehavior.Replace:
                            File.Delete(localfilePath);
                            break;
                        case FileExistsBehavior.Skip:
                            SkippingFile(localFileName);
                            return localfilePath;
                        case FileExistsBehavior.Rename:
                            localfilePath = GetUniquePath(localfilePath, out suffix);
                            localFileName = suffix + localFileName;
                            break;
                    }
                }
                DownloadingFile(localFileName);
                using (var fs = File.Create(localfilePath))
                {
                    response.GetResponseStream().CopyTo(fs);
                }

                return localfilePath;
            }
            catch (Exception ex)
            {
                DownloadFailed(url, ex);
                suffix = "";
                return url;
            }
        }

        /// <summary>
        /// Creates a unique filename if another file already exists at the path.
        /// If there is no file at the path, it will return the original path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="addedSuffix">the suffix added before the filename to make the path unique</param>
        /// <returns>the unique file path</returns>
        private string GetUniquePath(string path, out string addedSuffix)
        {
            var dir = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);

            for (var i = 2; ; i++)
            {
                if (!File.Exists(path))
                {
                    addedSuffix = i == 2 ? "" : i - 1 + "_";
                    return path;
                }

                path = Path.Combine(dir, i + "_" + fileName);
            }
        }
    }
}