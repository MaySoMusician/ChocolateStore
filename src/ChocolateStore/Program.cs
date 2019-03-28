using System;
using System.IO;

namespace ChocolateStore
{
    class Program
    {

        static void Main(string[] args)
        {
            PackageCacher cacher = new PackageCacher();

            cacher.SkippingFile += cacher_SkippingFile;
            cacher.DownloadingFile += cacher_DownloadingFile;
            cacher.DownloadFailed += cacher_DownloadFailed;

            try
            {
                var arguments = ArgumentParser.ParseArguments(args);
                CreateDirectoryIfNonExistent(arguments.Directory);
                cacher.CachePackage(arguments.PackageName, arguments.Directory, arguments.Variables);
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());
            }

        }

        private static void CreateDirectoryIfNonExistent(string directory)
        {
            if (!Directory.Exists(directory))
            {
                if (!PromptConfirm("Directory '{0}' does not exist. Create?", directory))
                {
                    WriteError("Directory '{0}' does not exist.", directory);
                    return;
                }
                Directory.CreateDirectory(directory);
                Console.WriteLine("Created Directory '{0}'", directory);
            }
        }

        private static void cacher_SkippingFile(string fileName)
        {
            WriteWarning("Skipped: {0} - File already exists on disk.", fileName);
        }

        private static void cacher_DownloadingFile(string fileName)
        {
            WriteInfo("Downloading: {0}", fileName);
        }

        private static void cacher_DownloadFailed(string url, Exception ex)
        {
            WriteError("Download Failed: {0}", url);
            Console.WriteLine(ex.ToString());
        }

        private static void WriteInfo(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }

        private static void WriteWarning(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }

        private static void WriteError(string format, params object[] arg)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }

        private static bool PromptConfirm(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(format + " [y/n] ", arg);
            Console.ResetColor();
            ConsoleKey response = Console.ReadKey(false).Key;
            Console.WriteLine();
            return response == ConsoleKey.Y;
        }
    }
}
