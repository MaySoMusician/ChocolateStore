using System;
using System.Linq;
using HtmlAgilityPack;

namespace ChocolateStore
{
    class PackageInfo
    {
        public string name { get; set; }
        public string url { get; set; }
        public string[] dependencies { get; set; }
        public PackageInfo(string packageName)
        {
            Console.WriteLine("Reading package '{0}'", packageName);
            this.name = packageName;
			var web = new System.Net.WebClient();
			var content = web.DownloadString("https://chocolatey.org/packages/" + packageName);
			var doc = new HtmlDocument ();
			doc.LoadHtml (content);
            this.url = doc.DocumentNode
                .SelectSingleNode("//a[contains(@title, 'nupkg')]")
                .Attributes["href"].Value;
            Console.WriteLine("package URL: '{0}'", this.url);
            try
            {
                this.dependencies = doc.DocumentNode.SelectNodes("//ul[@id='dependencySets']//a").Select(node => node.InnerText).ToArray<string>();
            } catch (Exception) { }

        }
    }
}
