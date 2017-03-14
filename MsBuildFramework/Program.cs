using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using System.Diagnostics;

using Microsoft.Build;
using Microsoft.Build.Execution;
using Microsoft.Build.Evaluation;

using BatchFramework;

namespace MsBuildFramework
{
    class Program
    {
        static long sTotaTextFilesLength = 0;
        static string directoryPath;

        static void testNotify(string msg)
        {
            Console.WriteLine(msg);
        }

        static void AccumulateLength(IFileAccessLogic lo, FileInfo fi)
        {
            sTotaTextFilesLength += fi.Length;
        }

        public static void Compress(string Origindir)
        {
            directoryPath = directoryPath.TrimEnd('\\');

            if (Directory.Exists(directoryPath + @"\Release\"))
            {
                Directory.Delete(directoryPath + @"\Release\", true);
            }

            Directory.CreateDirectory(directoryPath + @"\Release\");

            Console.WriteLine("Creating zip file...");

            ZipFile.CreateFromDirectory(Origindir, directoryPath + @"\Release\executables.zip");

            FileInfo fileToCompress = new FileInfo(directoryPath + @"\Release\executables.zip");

            Console.WriteLine("Compressing to .gz...");

            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) &
                    FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                            CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                    FileInfo info = new FileInfo(directoryPath + @"\Release\executables.zip.gz");
                    Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                    fileToCompress.Name, fileToCompress.Length.ToString(), info.Length.ToString());
                }

            }
        }

        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }

        static int Main(string[] args)
        {
            string[] fileTypes = { "vcxproj", "csproj" };

            directoryPath = ChooseDirectory();

            /// going through the files and building msbuild scripts

            FileAccessLogic logic = new FileAccessLogic();

            logic.Recursive = true;
            logic.Verbose = false;
            logic.FilePattern = "*.*";
            logic.OnProcess += new FileAccessProcessEventHandler(OnProcessSimpleList);
            logic.OnNotify += new FileAccessNotifyEventHandler(OnNotify);

            Console.WriteLine("");
            Console.WriteLine("Processing files in folder " + directoryPath);
            Console.WriteLine("Press any key to start:");

            Console.ReadKey();

            Console.WriteLine("");
            Console.WriteLine("******************************");
            Console.WriteLine("");

            foreach (string extension in fileTypes)
            {
                logic.FilePattern = "*." + extension;

                logic.Execute(directoryPath);
            }
            
            Console.WriteLine("******************************");
            Console.WriteLine("");
            Console.WriteLine("Total length of msbuild files is {0}", sTotaTextFilesLength);
            Console.WriteLine("");

            /// copying executables
            
            Console.WriteLine("Copying executables to bin\\ folder...");
            Console.WriteLine("");
            Console.WriteLine("******************************");
            Console.WriteLine("");

            if (Directory.Exists(directoryPath.TrimEnd('\\') + @"\bin"))
            {
                Directory.Delete(directoryPath.TrimEnd('\\') + @"\bin", true);
            }
            Directory.CreateDirectory(directoryPath.TrimEnd('\\') + @"\bin");

            sTotaTextFilesLength = 0;

            logic.Recursive = true;
            logic.Verbose = true;
            logic.FilePattern = "*.exe";
            logic.OnProcess -= new FileAccessProcessEventHandler(OnProcessSimpleList);
            logic.OnProcess += new FileAccessProcessEventHandler(OnProcessExecutables);

            logic.Execute(directoryPath);

            Console.WriteLine("******************************");
            Console.WriteLine("");
            Console.WriteLine("Total length of executable files is {0}", sTotaTextFilesLength);
            Console.WriteLine("");

            /// compressing executables

            Compress(directoryPath.TrimEnd('\\') + @"\bin");

            Console.ReadKey();
            return 0;
        }

        private static void OnProcessSimpleList(object sender, ProcessEventArgs e)
        {
            if (e.Logic.Cancelled)
            {
                return;
            }

            AccumulateLength(e.Logic, e.FileInfo);

            FileInfo fi = e.FileInfo;
            var startinfo = new ProcessStartInfo();

            Console.WriteLine("Building file: {0}", fi.FullName);

            startinfo.FileName = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe";
            startinfo.Arguments = @"/t:build /p:Configuration=Debug /p:ToolsVersion=""14.0"" /p:PlatformToolset=""v140"" /p:OutputPath=C:\output\ " + fi.FullName;

            startinfo.RedirectStandardError = true;
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardInput = true;
            startinfo.UseShellExecute = false;

            Process build = Process.Start(startinfo);

            string error = build.StandardError.ReadToEnd();
            //string output = build.StandardOutput.ReadToEnd();

            //Console.WriteLine("0: {0}, 1: {1}", error, output);

            build.WaitForExit();

            int exitcode = build.ExitCode;

            Console.WriteLine("Building Complete. Exit code = {0}.", exitcode);

            if (exitcode != 0)
            {
                Console.WriteLine("Error: {0}", error);
            }

            Console.WriteLine();
        }

        private static void OnProcessExecutables(object sender, ProcessEventArgs e)
        {
            if (e.Logic.Cancelled)
            {
                return;
            }

            AccumulateLength(e.Logic, e.FileInfo);

            FileInfo fi = e.FileInfo;
            var startinfo = new ProcessStartInfo();

            string pathOrigin = fi.FullName;
            string pathDest = directoryPath.TrimEnd('\\') + @"\bin\";

            if (File.Exists(pathDest + fi.Name))
            {
                File.Delete(pathDest + fi.Name);
            }

            File.Copy(pathOrigin, pathDest + fi.Name);

            if (e.Logic.Verbose)
            {
                // printing the information if verbose is true
                Console.WriteLine("Copied: {0} -> To: {1}", pathOrigin, pathDest + fi.Name);
                Console.WriteLine();
            }
        }

        private static void OnNotify(object sender, NotifyEventArgs e)
        {
            //Console.WriteLine(e.Message);
            //testNotify(e.Message);
        }

        private static string ChooseDirectory()
        {
            bool validChoice = false;
            string directoryChoice = "";

            while (validChoice == false)
            {
                Console.WriteLine("Please, specify top level directory in full:");

                directoryChoice = Console.ReadLine().Trim().Replace("\"", "");

                if (Directory.Exists(directoryChoice))
                {
                    validChoice = true;
                }
                else
                {
                    Console.WriteLine("Invalid directory. Try again!");
                    Console.WriteLine("");
                }
            }

            return directoryChoice;
        }

        private static string[] ChooseFileTypes()
        {
            bool validChoice = false;
            string[] typeChoices = null;

            while (validChoice == false)
            {
                Console.WriteLine("Please, specify the file types that you are interested in:");

                typeChoices = Console.ReadLine().Trim().Replace("\"", "").Split(' ');

                if (typeChoices.Length == 1 && typeChoices[0] == "")
                {
                    typeChoices[0] = "*";
                    validChoice = true;
                }
                // checks if all types are unique
                else if (typeChoices.Distinct<string>().Count() == typeChoices.Length)
                {
                    validChoice = true;
                }
                else
                {
                    Console.WriteLine("{0} {1}", typeChoices.Distinct<string>().Count(), typeChoices.Length);
                    Console.WriteLine("Repeated types. Try again!");
                    Console.WriteLine("");
                }
            }

            return typeChoices;
        }
    }
}
