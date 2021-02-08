using System;
using System.Linq;
using NuGet;

namespace ChocolateStore
{
    class PackageInfo
    {
        private static IPackageRepository Repository { get; set; }
        private const string RepositoryUrl = "https://chocolatey.org/api/v2/";

        static PackageInfo()
        {
            Repository = PackageRepositoryFactory.Default.CreateRepository(RepositoryUrl);
        }

        public string Name { get; }
        public string Url { get; }
        public string[] Dependencies { get; }
        public string Version { get; }

        private PackageInfo(DataServicePackage package)
        {
            Url = package.DownloadUrl.AbsoluteUri;
            Dependencies = package.DependencySets.SelectMany(ds => ds.Dependencies).Select(dp => dp.Id).ToArray();
            Name = package.Id;
            Version = package.Version;            
        }

        /// <summary>
        /// Checks the nuget repository for the package and returns a PackageInfo object if one was found.
        /// Throws an exception if none is found.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static PackageInfo Find(string packageName)
        {
            var package = Repository.GetPackages().Where(p => p.Id == packageName && p.IsLatestVersion).ToList().SingleOrDefault(p => p.IsReleaseVersion()) as DataServicePackage;
            if (package == null)
            {
                throw new Exception($"Could not get package with id ${packageName} from ${RepositoryUrl}");
            }

            return new PackageInfo(package);
        }
    }
}
