using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PictureRenamer
{
    class Program
    {
        private static readonly string[] extensions = new string[] { ".jpg", ".png", ".bmp", ".gif", ".tga", ".tiff" };

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No Path");
                Console.ReadLine();
                return;
            }

            FileInfo[] files = GetAllFiles(args).Where(f => extensions.Any(e => e == f.Extension.ToLower())).ToArray();
            Console.WriteLine(files.Length);

            Console.Write("Prefix: ");
            string prefix = Console.ReadLine();

            foreach (FileInfo file in files)
            {
                string toPath;

                if (prefix.Any()) toPath = ToFileName(file, prefix);
                else if (!TryToFileName(file, out toPath)) continue;

                if (file.FullName != toPath) File.Move(file.FullName, toPath);
            }
        }

        private static IEnumerable<FileInfo> GetAllFiles(string[] sources)
        {
            try
            {
                return sources.SelectMany(GetFileInfos).Where(f => (f.Attributes & FileAttributes.Hidden) == 0);
            }
            catch
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        private static IEnumerable<FileInfo> GetFileInfos(string path)
        {
            if (File.Exists(path)) yield return new FileInfo(path);

            if (Directory.Exists(path))
            {
                foreach (FileInfo file in Files(path)) yield return file;
            }
        }

        private static IEnumerable<FileInfo> Files(string folder)
        {
            foreach (string file in Directory.GetFiles(folder)) yield return new FileInfo(file);

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                foreach (FileInfo file in Files(subFolder)) yield return file;
            }
        }

        private static bool TryToFileName(FileInfo file, out string toPath)
        {
            DateTime catTime;
            string prefix;

            try
            {
                if (!TryGetTimeFromPathVLC(file.FullName, out catTime, out prefix) &&
                    !TryGetTimeFromPathFraps(file.FullName, out catTime, out prefix) &&
                    !TryGetTimeFromPathApp(file.FullName, out catTime, out prefix))
                {
                    toPath = null;
                    return false;
                }
            }
            catch
            {
                toPath = null;
                return false;
            }

            toPath = Path.Combine(file.DirectoryName, prefix + " " + Convert(catTime) + file.Extension);
            return true;
        }

        private static string ToFileName(FileInfo file, string prefix)
        {
            DateTime catTime;
            string wasPrefix;

            try
            {
                if (!TryGetTimeFromPathVLC(file.FullName, out catTime, out wasPrefix) &&
                    !TryGetTimeFromPathFraps(file.FullName, out catTime, out wasPrefix) &&
                    !TryGetTimeFromPathApp(file.FullName, out catTime, out wasPrefix))
                {
                    catTime = GetDateTakenFromImage(file.FullName);
                }
            }
            catch
            {
                catTime = file.LastWriteTime;
            }

            return Path.Combine(file.DirectoryName, prefix + " " + Convert(catTime) + file.Extension);
        }

        public static bool TryGetTimeFromPathVLC(string path, out DateTime time, out string prefix)
        {
            time = DateTime.MinValue;
            prefix = null;

            string fileName = Path.GetFileNameWithoutExtension(path);

            try
            {
                int milli, sec, min, hour, day, month, year;
                int index = fileName.Length - 1;

                if (!TryGetNumber(fileName, 3, ref index, out milli)) return false;

                if (fileName[index--] != 's') return false;
                if (!TryGetNumber(fileName, 2, ref index, out sec)) return false;

                if (fileName[index--] != 'm') return false;
                if (!TryGetNumber(fileName, 2, ref index, out min)) return false;

                if (fileName[index--] != 'h') return false;
                if (!TryGetNumber(fileName, 2, ref index, out hour)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out day)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out month)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 4, ref index, out year)) return false;

                time = new DateTime(year, month, day, hour, min, sec, milli);
                prefix = fileName.Remove(index);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetTimeFromPathFraps(string path, out DateTime time, out string prefix)
        {
            time = DateTime.MinValue;
            prefix = null;

            string fileName = Path.GetFileNameWithoutExtension(path);

            try
            {
                int milli, sec, min, hour, day, month, year;
                int index = fileName.Length - 1;

                if (!TryGetNumber(fileName, 2, ref index, out milli)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out sec)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out min)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out hour)) return false;

                if (fileName[index--] != ' ') return false;
                if (!TryGetNumber(fileName, 2, ref index, out day)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out month)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 4, ref index, out year)) return false;

                time = new DateTime(year, month, day, hour, min, sec, milli * 10);
                prefix = fileName.Remove(index);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetTimeFromPathApp(string path, out DateTime time, out string prefix)
        {
            time = DateTime.MinValue;
            prefix = null;

            string fileName = Path.GetFileNameWithoutExtension(path);

            try
            {
                int milli, sec, min, hour, day, month, year;
                int index = fileName.Length - 1;

                if (!TryGetNumber(fileName, 3, ref index, out milli)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out sec)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out min)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out hour)) return false;

                if (fileName[index--] != ' ') return false;
                if (!TryGetNumber(fileName, 2, ref index, out day)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 2, ref index, out month)) return false;

                if (fileName[index--] != '-') return false;
                if (!TryGetNumber(fileName, 4, ref index, out year)) return false;

                time = new DateTime(year, month, day, hour, min, sec, milli);
                prefix = fileName.Remove(index);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetNumber(string text, int numberDigits, ref int index, out int number)
        {
            int digit, factor = 1;

            number = 0;

            if (!int.TryParse(text[index--].ToString(), out digit)) return false;

            number = digit * factor;

            for (int i = 1; i < numberDigits; i++, index--)
            {
                if (!int.TryParse(text[index].ToString(), out digit)) break;

                factor *= 10;
                number += digit * factor;
            }

            return true;
        }

        private static Regex r = new Regex(":");

        public static DateTime GetDateTakenFromImage(string path)
        {
            using (Bitmap myImage = new Bitmap(path))
            {
                PropertyItem propItem = myImage.GetPropertyItem(36867);
                string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                return DateTime.Parse(dateTaken);
            }
        }

        private static string Convert(DateTime t)
        {
            string date = string.Format("{0,2}-{1,2}-{2,2}", t.Year, t.Month, t.Day).Replace(" ", "0");
            string time = string.Format("{0,2}-{1,2}-{2,2}-{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond).Replace(" ", "0");

            return date + " " + time;
        }
    }
}
