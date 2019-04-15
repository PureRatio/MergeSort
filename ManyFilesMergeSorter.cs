using System;
using System.Collections.Generic;
using System.IO;

namespace FileSort
{
    class ManyFilesMergeSorter<T> where T : IComparable
    {
		public ManyFilesMergeSorter(Func<string, T> convertFunc) {
			this.convert = convertFunc;
		}

        protected bool cachedSortOrder = true;
        protected int inMemorySortCount = 100;
        protected int mergedFilesInSameTimeCount = 10;
		protected string sourceFileName;
		protected string tmpDirPath;
		protected Func<string, T> convert;

		List<List<string>> levels =
			new List<List<string>>();

		public string Sort(string path, bool order = true)
        {
			//	проверяем, отсортирован ли уже файл в нужном порядке
			int currentSortOrder;
			if(CheckSorting (path, out currentSortOrder))
			{
				if
				(
					currentSortOrder == 0 ||
					(currentSortOrder > 0) == order
				)
				{
					return path;
				}
			}

			DirectoryInfo tmpDir = null;
			if (Directory.Exists ("tmp")) {
				tmpDir = new DirectoryInfo ("tmp");
			} else {
				tmpDir = Directory.CreateDirectory ("tmp");
			}
			tmpDirPath = tmpDir.FullName;

			levels.Clear ();
            cachedSortOrder = order;
			sourceFileName = Path.GetFileNameWithoutExtension(path);
            //  разбиваем исходный файл на файлы приемлемого размера

			CustomEnumerator<T> enmr = LinesEnumerator(path);
            List<T> lines = new List<T>();
            int counter = 0;

			while (!enmr.HasEnded)
            {
				if (enmr.MoveNext() && counter < inMemorySortCount) {
					T value = enmr.Current;
					lines.Add (value);
					counter++;
				}
				else
				{
					counter = 0;
					var arr = lines.ToArray ();
					lines.Clear ();
					SortInMemory (arr, 0, arr.Length);
					int partNum = levels.Count > 0 ? levels [0].Count : 0;
					string partPath = Path.Combine (tmpDirPath, $"{sourceFileName}_level_0_part_{partNum}");
					SaveToFile (arr, partPath);
				}
            }

            return FinalMerge();
        }

        //  сортировка слиянием в памяти
        protected void SortInMemory(T[] arr, int low, int high)
        {
            int currentSectionLength = high - low;
            if (currentSectionLength <= 1)
                return;

            int mid = low + currentSectionLength / 2;

            SortInMemory(arr, low, mid);
            SortInMemory(arr, mid, high);

            T[] tmpArr = new T[currentSectionLength];
            int i = low, j = mid;
            for (int k = 0; k < currentSectionLength; k++)
            {
                //  если элементы в первой части закончились
                if (i == mid) tmpArr[k] = arr[j++];
                //  если элементы во второй части закончились
                else if (j == high) tmpArr[k] = arr[i++];
                //  сравниваем и вставляем с учётом порядка сортировки
                else if (arr[i].CompareTo(arr[j]) < 0)
                {
                    //  если первый меньше второго
                    if (cachedSortOrder) tmpArr[k] = arr[i++];
                    else tmpArr[k] = arr[j++];

                }
                else
                {
                    //  если первый больше второго
                    if (cachedSortOrder) tmpArr[k] = arr[j++];
                    else tmpArr[k] = arr[i++];
                }
            }

            for (int k = 0; k < currentSectionLength; k++)
            {
                arr[low + k] = tmpArr[k];
            }
        }

        protected void SaveToFile(T[] arr, string partPath)
        {
            using (StreamWriter sw = new StreamWriter(partPath))
            {
                foreach (var line in arr)
                {
                    sw.WriteLine(line);
                }
            }

            if(levels.Count == 0)
            {
				levels.Add(new List<string>());
            }

			levels[0].Add(partPath);

            if (levels[0].Count < mergedFilesInSameTimeCount) return;

            ClampMergeLevels();
        }

        //  проверяем, нет ли на каком-то уровне слишком большого
        //  количества файлов
        protected void ClampMergeLevels()
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].Count >= mergedFilesInSameTimeCount)
                {
                    //  когда набирается достаточное количество одинаковых файлов, 
                    //  сливаем отсортированные файлы в один
					bool lastLevel = i >= levels.Count - 1;
					int fileIndex = lastLevel ? 0 : levels[i+1].Count;
					string path = Path.Combine(tmpDirPath, $"{sourceFileName}_level_{i+1}_part_{fileIndex}");
					var nextLevelPart = MergeFiles(levels[i], path, cachedSortOrder);

					if (lastLevel)
                    {
						var nextList = new List<string>();
                        levels.Add(nextList);
                    }

					levels[i + 1].Add(nextLevelPart);
                }

            }
        }

		protected string FinalMerge()
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].Count == 0) continue;
                //  когда набирается достаточное количество одинаковых файлов,
                //  сливаем отсортированные файлы в один
				bool lastLevel = i >= levels.Count - 1;
				int fileIndex = lastLevel ? 0 : levels[i+1].Count;
				string path = Path.Combine(tmpDirPath, $"{sourceFileName}_level_{i+1}_part_{fileIndex}");
				var nextLevelPart = MergeFiles(levels[i], path, cachedSortOrder);

				if (lastLevel)
                {
                    //  конец
                    return nextLevelPart;
                }
                else
                {
                    levels[i + 1].Add(nextLevelPart);
                }
            }

            return null;
        }

        //  тут не будет без рекурсий, т.к. глубина неизвестна и
        //  чтоб не менять размер стэка вызовов, ну и работать быстрее будет
		public string MergeFiles
        (
			List<string> filesPaths,
            string sortedMergedFilePath,
            bool order,
            bool deleteSources = true
        )
        {
			if (filesPaths.Count == 0)
				return null;

            //  на случай, если файл всего один и не с чем его сливать
            if (filesPaths.Count == 1)
            {
                File.Move(filesPaths[0], sortedMergedFilePath);
				filesPaths.Clear();
				return sortedMergedFilePath;
            }

			CustomEnumerator<T>[] enumsArr =
				new CustomEnumerator<T>[filesPaths.Count];

            for (int i = 0; i < filesPaths.Count; i++)
            {
				enumsArr[i] = LinesEnumerator(filesPaths[i]);
				enumsArr[i].MoveNext();
            }

            using (StreamWriter sw = new StreamWriter(sortedMergedFilePath))
            {

                bool done = false;
                while (!done)
                {
                    //  ищем среди файлов значение, удовлетворяющее порядку
                    //  сортировки
                    int nextValueIndex = -1;
                    T value = default(T);

                    for (int i = 0; i < enumsArr.Length; i++)
                    {
                        if (!enumsArr[i].HasEnded)
                        {
                            if (nextValueIndex < 0)
                            {
                                nextValueIndex = i;
                                value = enumsArr[i].Current;
                            }
                            else
                            {
                                //  берём значение из другого файла, если оно больше
                                //  удовлетворяет порядку сортировки, чем текущее
								if (order)
								{
									//	выбираем наименьший
									if (value.CompareTo (enumsArr [i].Current) > 0)
									{
										nextValueIndex = i;
										value = enumsArr [i].Current;
									}
										
								}
								else
								{
									//	выбираем наибольший
									if (value.CompareTo (enumsArr [i].Current) < 0)
									{
										nextValueIndex = i;
										value = enumsArr [i].Current;
									}
								}
                            }
                        }
                    }

					if (nextValueIndex == -1) {
						done = true;
						break;
					} else {
						enumsArr[nextValueIndex].MoveNext();
					}

                    //  пишем в итоговый файл
                    sw.WriteLine(value);
                }
            }

            //  удаляем исходные файлы
            if (deleteSources) {
                for (int i = 0; i < filesPaths.Count; i++)
                {
                    File.Delete(filesPaths[i]);
                }
            }
            filesPaths.Clear();

			//  на всякий случай чистим неиспользуемую память
			GC.Collect();

			return sortedMergedFilePath;
        }

		protected CustomEnumerator<T> LinesEnumerator(string path)
		{
			return new CustomEnumerator<T>(
				GetFileLinesEnumerator(path)
			);
		}

		protected IEnumerator<T> GetFileLinesEnumerator(string path)
		{
			IEnumerable<string> lines = File.ReadLines(path);
			foreach (var line in lines)
			{
				T value;
				try {
					value = convert(line);
				}
				catch(Exception e) {
					Console.WriteLine($"{path} file contains invalid value: {line}");
					continue;
				}
				yield return value;
			}
		}

		protected bool CheckSorting(string path, out int sortOrder)
		{
			bool sorted = true;
			sortOrder = 0;
			int order = 0;

			int tmpOrder = 0;

			CustomEnumerator<T> enmr = LinesEnumerator(path);
			T lastValue;
			if (enmr.MoveNext ()) {
				lastValue = enmr.Current;
			} else {
				return sorted;
			}
			while (!enmr.HasEnded)
			{
				if (!enmr.MoveNext())
					break;

				T tmpValue = enmr.Current;
				tmpOrder = tmpValue.CompareTo (lastValue);

				if (order != 0 && order != tmpOrder)
				{
					sorted = false;
					break;
				}


				order = tmpOrder;
				lastValue = tmpValue;
			}

			sortOrder = order;
			return sorted;
		}

		public void DeleteTmp() {
			if (Directory.Exists ("tmp")) {
				Directory.Delete("tmp");
			}
		}
    }
}