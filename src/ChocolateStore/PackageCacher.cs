using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using HtmlAgilityPack;

namespace ChocolateStore
{
    class PackageCacher
    {

        private const string INSTALL_FILE = "tools/chocolateyInstall.ps1";

        public delegate void FileHandler(string fileName);
        public delegate void DownloadFailedHandler(string url, Exception ex);

        public event FileHandler SkippingFile = delegate { };
        public event FileHandler DownloadingFile = delegate { };
        public event DownloadFailedHandler DownloadFailed = delegate { };

        public void CachePackage(string packageName, string directory, IEnumerable<Tuple<string, IEnumerable<string>>> variables)
        {
            var packageInfo = new PackageInfo(packageName);

            var packagePath = DownloadFile(packageInfo.url, directory);

            using (var zip = ZipFile.Read(packagePath))
            {
                var entry = zip.FirstOrDefault(x => string.Equals(x.FileName, INSTALL_FILE, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    string content = null;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        entry.Extract(ms);
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms, true))
                        {
                            content = reader.ReadToEnd();
                        }
                    }

                    content = CacheUrlFiles(Path.Combine(directory, packageName), content, variables);
                    zip.UpdateEntry(INSTALL_FILE, content);
                    zip.Save();

                }
            }

            if (packageInfo.dependencies != null)
            {
                foreach (var dep in packageInfo.dependencies)
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

                foreach (var permutation in variablePermutations)
                {
                    var resolvedUrl = Variables.ResolveVariables(match.Value, permutation);
                    var resolvedFileName = Variables.ResolveVariables(fileNameWithVariables, permutation);
                    DownloadFile(resolvedUrl, localDirectory, resolvedFileName, true);
                }

                var localPath = Path.Combine(localDirectory, fileNameWithVariables);
                return localPath;
            });
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="localDirectory">The directory into which the file should be stored.</param>
        /// <param name="localFileName">The file name to which the file should be saved. If this arguments is left empty, the file name on the server will be used.</param>
        /// <param name="forceDownload">If this parameter is set to true, the method will download the file from the server even if it already exists on the local directory.</param>
        /// <returns>The local path of the downloaded file.</returns>
        private string DownloadFile(string url, string localDirectory, string localFileName = "", bool forceDownload = false)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                if (String.IsNullOrEmpty(localFileName))
                    localFileName = Path.GetFileName(response.ResponseUri.LocalPath);
                var filePath = Path.Combine(localDirectory, localFileName);

                if (File.Exists(filePath))
                {
                    if (forceDownload)
                    {
                        File.Delete(filePath);
                    }
                    else
                    {
                        SkippingFile(localFileName);
                        return filePath;
                    }
                }
                DownloadingFile(localFileName);
                using (var fs = File.Create(filePath))
                {
                    response.GetResponseStream().CopyTo(fs);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                DownloadFailed(url, ex);
                return url;
            }

        }

    }
}