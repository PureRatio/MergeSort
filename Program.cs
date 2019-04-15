using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileSort
{
    class Program
    {
        protected static string outputFilePath = "out.txt";
        protected static string[] inputFilePaths = { "in1.txt", "in2.txt", "in3.txt" };
        protected static Type dataType = typeof(Int32);
        protected static bool isAscending = true;

        static void Main(string[] args)
		{
			try
			{
	            if (args.Length < 3)
	            {
	                Console.WriteLine("Not enough required argiments");
	            }
	            else
	            {
	                int argIndex = 0;
	                if (args[argIndex] == "-a" || args[argIndex] == "-d")
	                {
	                    isAscending = args[argIndex] == "-a";
	                    argIndex++;
	                }

	                if(args[argIndex] == "-i")
	                {
	                    dataType = typeof(Int32);
	                    argIndex++;
	                }
	                else if(args[argIndex] == "-s")
	                {
	                    dataType = typeof(String);
	                    argIndex++;
	                }
	                else
	                {
	                    Console.WriteLine("Unknown data type");
	                    return;
	                }

	                if(!string.IsNullOrEmpty(args[argIndex]))
	                {
	                    outputFilePath = args[argIndex];
	                    argIndex++;
	                }

	                int inputFilesCount = args.Length - argIndex;
	                if(inputFilesCount > 0)
	                {
	                    inputFilePaths = new string[inputFilesCount];
						for (int i = 0; i < args.Length - argIndex; i++)
	                    {
							inputFilePaths[i] = args[i + argIndex];
	                    }
	                }
	                else
	                {
	                    Console.WriteLine("No input files in args");
	                    return;
	                }

					//	проверяем входящие файлы на валидность
					List<string> inputFiles =
						new List<string>();
					for (int i = 0; i < inputFilePaths.Length; i++)
					{
						string path = inputFilePaths[i];

						if(!File.Exists(path))
						{
							Console.WriteLine($"File {path} does not exist");
							continue;
						}

						if(new FileInfo(path).Length <= 0)
						{
							Console.WriteLine($"File {path} is empty");
							continue;
						}

						inputFiles.Add(path);
					}

					if(inputFiles.Count == 0) {
						Console.WriteLine("No valid files to merge");
						return;
					}

	                if(dataType == typeof(Int32))
	                {
						Func<string, int> convert = (x) => {return int.Parse(x);};
						ManyFilesMergeSorter<int> sorter =
							new ManyFilesMergeSorter<int>(convert);

						List<string> sortedInputFiles = new List<string>();
						foreach(var inFile in inputFiles) {
							string str = sorter.Sort(inFile, isAscending);
							if(!string.IsNullOrEmpty(str)) {
								sortedInputFiles.Add(str);
							}
						}

						sorter.MergeFiles(sortedInputFiles, outputFilePath, isAscending);
						sorter.DeleteTmp();
	                }

	                if (dataType == typeof(String))
	                {
						Func<string, string> convert = (x) => {return x;};
						ManyFilesMergeSorter<string> sorter =
							new ManyFilesMergeSorter<string>(convert);

						List<string> sortedInputFiles = new List<string>();
						foreach(var inFile in inputFiles) {
							string str = sorter.Sort(inFile, isAscending);
							if(!string.IsNullOrEmpty(str)) {
								sortedInputFiles.Add(str);
							}
						}

						sorter.MergeFiles(sortedInputFiles, outputFilePath, isAscending);
						sorter.DeleteTmp();
	                }
	            }
				Console.WriteLine("Success");
			}
			catch (Exception e) {
				Console.WriteLine("Fail");
				Console.WriteLine(e);
			}

            Console.ReadLine();
        }
    }
}