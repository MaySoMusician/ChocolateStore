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

        private string CacheUrlFiles(string folder, string content, IEnumerable<Tuple<string, IEnumerable<string>>> variables)
        {
            const string pattern = "(?<=['\"])http[\\S ]*(?=['\"])";

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var variablesWithAlternatives = variables.ToList();

            // replace variables for which there is only one value
            foreach (var variable in variables)
            {
                if (variable.Item2.Count() == 1)
                {
                    content = content.Replace("${" + variable.Item1 + "}", variable.Item2.First());
                    variablesWithAlternatives.Remove(variable);
                }
            }

            var variableCombinations = GetVariablePermutations(variablesWithAlternatives);
            return Regex.Replace(content, pattern, m =>
            {
                var fileNameWithVariables = Path.GetFileName(new Uri(m.Value).LocalPath);
                fileNameWithVariables = variablesWithAlternatives.Aggregate("", (output, variable) => output + "${" + variable.Item1 + "}_") + fileNameWithVariables;
                var path = Path.Combine(folder, fileNameWithVariables);

                foreach (var combination in variableCombinations)
                {
                    var uriWithoutVariables = m.Value;
                    var fileNameWithOutVariables = fileNameWithVariables.ToString();
                    foreach (var variable in combination)
                    {
                        uriWithoutVariables = uriWithoutVariables.Replace("${" + variable.Item1 + "}", variable.Item2);
                        fileNameWithOutVariables = fileNameWithOutVariables.Replace("${" + variable.Item1 + "}", variable.Item2);
                    }
                    DownloadFile(uriWithoutVariables, folder, fileNameWithOutVariables, true);
                }

                return path;
            });
        }

        /// <summary>
        /// Returns a list of all possible permutations that result from combining the variables passed into the moethod.
        /// </summary>
        /// <param name="variableOptions"></param>
        /// <returns></returns>
        private List<List<Tuple<string, string>>> GetVariablePermutations(List<Tuple<string, IEnumerable<string>>> variableOptions)
        {
            var variableCombinations = new List<List<Tuple<string, string>>>();
            RecursiveVariableCombiner(variableOptions.ToList(), new List<Tuple<string, string>>(), variableCombinations);
            return variableCombinations;
        }

        private void RecursiveVariableCombiner(List<Tuple<string, IEnumerable<string>>> remainingVariableOptions, List<Tuple<string, string>> alreadySetVariableValues, List<List<Tuple<string, string>>> variableCombinations)
        {
            var newVariableList = remainingVariableOptions?.ToList();
            var currentVariable = newVariableList?.FirstOrDefault();
            if (currentVariable == null)
            {
                variableCombinations.Add(alreadySetVariableValues);
                return;
            }

            newVariableList.Remove(currentVariable);

            foreach (var value in currentVariable.Item2)
            {
                var newPath = alreadySetVariableValues.ToList();
                newPath.Add(Tuple.Create(currentVariable.Item1, value));
                RecursiveVariableCombiner(newVariableList, newPath, variableCombinations);
            }
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