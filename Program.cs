using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCTRotation
{
    class Program
    {
        private static StringBuilder _fileOutput;
        private static string cctDirectory = @"ccts";
        private static string outputDirectory = Path.Combine(cctDirectory, "extracted");

        static void Main(string[] args)
        {
            _fileOutput = new StringBuilder();

            if (!Directory.Exists(outputDirectory))
            {
                Console.WriteLine("The output directory doesn't exist... creating");
            }

            Directory.CreateDirectory(outputDirectory);

            string[] fileEntries = Directory.GetFiles(cctDirectory);

            foreach (string fullFileName in fileEntries)
            {
                string fileExtension = Path.GetExtension(fullFileName);
                string fileName = Path.GetFileNameWithoutExtension(fullFileName);

                if (fileExtension != ".cct" && fileExtension != ".dcr")
                    continue;

                var output = Path.Combine(outputDirectory, "unzipped_" + fileName);

                if (fileName.Contains("_xx_s_"))
                {
                    continue;
                }

                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }
                else
                {
                    continue;
                }

                Process p = new Process();
                p.StartInfo.FileName = "offzip.exe";
                p.StartInfo.Arguments = "-a " + fullFileName + " " + "\"" + Path.Combine(outputDirectory, "unzipped_" + fileName) + "\"";
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();

                Console.WriteLine("Extension: " + fileExtension);
            }

            Console.WriteLine("Done extracting");

            foreach (string fullFileName in fileEntries)
            {
                string fileName = Path.GetFileNameWithoutExtension(fullFileName);
                var output = Path.Combine(outputDirectory, "unzipped_" + fileName);

                if (!Directory.Exists(output))
                {
                    continue;
                }

                if (fileName.Contains("_s_"))
                {
                    continue;
                }              


                string[] files = Directory.GetFiles(output);
                var spriteName = fileName.Replace("hh_furni_xx_", "");

                foreach (string file in files)
                {
                    string fileContents = File.ReadAllText(file);

                    if (HasRotationData(fileContents, spriteName))
                    {
                        FindRotation(fileContents, spriteName);
                    }

                }
            }

            File.WriteAllText("allowed_rotations.sql", _fileOutput.ToString());

            Console.WriteLine("Done!");
            Console.Read();
        }

        private static bool HasRotationData(string fileContents, string furniName)
        {
            return fileContents.Contains("=" + furniName);
        }

        private static void FindRotation(string fileContents, string furniName)
        {
            try
            {
                string start = fileContents;

                start = start.Substring(start.IndexOf(furniName));
                start = start.Substring(0, start.IndexOf("" + (char)0));

                List<int> rotations = new List<int>();

                foreach (var line in start.Split("\r\n".ToCharArray()))
                {
                    if (line == "" || line.Contains("_sd_") || line.Contains("_sd=") || line.Contains(".props"))
                    {
                        continue;
                    }

                    try
                    {
                        string data = line.Substring(line.IndexOf("=") + 1);
                        var data2 = data.Substring(furniName.Length).Substring(9);
                        var data3 = data2.Substring(0, data2.IndexOf("_"));

                        var rot = int.Parse(data3);

                        if (!rotations.Contains(rot))
                        {
                            rotations.Add(rot);
                        }
                    }
                    catch
                    {

                    }

                    try
                    {
                        string data = line;
                        var data2 = data.Substring(furniName.Length).Substring(9);
                        var data3 = data2.Substring(0, data2.IndexOf("_"));

                        var rot = int.Parse(data3);

                        if (!rotations.Contains(rot))
                        {
                            rotations.Add(rot);
                        }
                    }
                    catch
                    {

                    }
                }

                _fileOutput.Append("UPDATE items_definitions SET allowed_rotations = '" + string.Join(",", rotations) + "' WHERE sprite LIKE '" + furniName + "%';\r\n");
                Console.WriteLine("Rotations for " + furniName + ": " + string.Join(",", rotations));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
