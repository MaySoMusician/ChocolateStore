using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using HtmlAgilityPack;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

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

        public void CachePackage(string packageName, string directory)
        {
            var packageInfo = new PackageInfo(packageName);

            var packagePath = DownloadFile(packageInfo.url, directory);

            using (var zip = ZipFile.Read(packagePath))
            {
                var entry = zip.FirstOrDefault(x => string.Equals(x.FileName, INSTALL_FILE, StringComparison.OrdinalIgnoreCase));

                if (entry != null) {
                    string content = null;

                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms, true))
                        {
                            content = reader.ReadToEnd();
                        }
                    }

                    content = CacheUrlFiles(Path.Combine(directory, packageName), content);
                    zip.UpdateEntry(INSTALL_FILE, content);
                    zip.Save();

                }

            }

            if (packageInfo.dependencies != null)
            {
                foreach (var dep in packageInfo.dependencies)
                {
                    CachePackage(dep, directory);
                }
            }

        }

        private string CacheUrlFiles(string folder, string content)
        {

            const string pattern = "(?<=['\"])http[\\S ]*(?=['\"])";

            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            return Regex.Replace(content, pattern, new MatchEvaluator(m => DownloadFile(m.Value, folder)));

        }

        private string DownloadFile(string url, string destination)
        {

            try
            {
				ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                var fileName = Path.GetFileName(response.ResponseUri.LocalPath);
                var filePath = Path.Combine(destination, fileName);

                if (File.Exists(filePath))
                {
                    SkippingFile(fileName);
                }
                else
                {
                    DownloadingFile(fileName);
                    using (var fs = File.Create(filePath))
                    {
                        response.GetResponseStream().CopyTo(fs);
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                DownloadFailed(url, ex);
                return url;
            }

        }

		public bool MyRemoteCertificateValidationCallback(System.Object sender,
			X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain,
			// look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None) {
				for (int i=0; i<chain.ChainStatus.Length; i++) {
					if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown) {
						continue;
					}
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build ((X509Certificate2)certificate);
					if (!chainIsValid) {
						isOk = false;
						break;
					}
				}
			}
			return isOk;
		}

    }
}