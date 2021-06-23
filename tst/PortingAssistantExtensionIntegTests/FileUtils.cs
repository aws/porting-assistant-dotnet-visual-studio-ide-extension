using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PortingAssistantExtensionIntegTests
{
    //Copied from PA standalone tests
    class FileUtils
    {
        static string[] allowList = { ".cs", ".json", ".config", ".nupkg", ".targets"};
        static ISet<string> allowSet = new HashSet<string>(allowList);

        public static bool AreTwoDirectoriesEqual(
            string dirPath1, string dirPath2)
        {
            DirectoryInfo dir1 = new DirectoryInfo(dirPath1);
            DirectoryInfo dir2 = new DirectoryInfo(dirPath2);
           
            // Take a snapshot of the file system.  
            IEnumerable<FileInfo> list1 = dir1.GetFiles(
                "*.*", SearchOption.AllDirectories).Where(x => isValidFile(x)).ToList<FileInfo>();
            IEnumerable<FileInfo> list2 = dir2.GetFiles(
                "*.*", SearchOption.AllDirectories).Where(x => isValidFile(x)).ToList<FileInfo>();

            Console.WriteLine("---------FILES IN DIR 1-----------");
            var set1 = PrintFileInfos(list1);
            Console.WriteLine("---------FILES IN DIR 2-----------");
            var set2 = PrintFileInfos(list2);
            
            if (list1.Count() != list2.Count())
            {
                Console.WriteLine("Files count mismatch: {0} -> {1}", list1.Count(), list2.Count());
                return false;
            }

            set2.ExceptWith(set1);

            if (set2.Count != 0)
            {
                foreach (var file in set2)
                {
                    Console.WriteLine("Expected File Not found:  " + file);
                }

                return false;
            }

            Console.WriteLine("Two directories are Equal");
            return true;

        }

        private static bool isValidFile(FileInfo x)
        {
            if (allowSet.Count == 0)
            {
                return false;
            }

            string extn = x.Extension;
            if (string.IsNullOrEmpty(extn)) return false;

            if ("PortSolutionResult.json".Equals(x.Name)) return false;

            if (x.FullName.Contains("bin") || x.FullName.Contains("obj")) return false;

            return allowSet.Contains(extn);

        }
     
        static ISet<string> PrintFileInfos(IEnumerable<FileInfo> fis)
        {
            ISet<string> set = new HashSet<string>();
            foreach (FileInfo fi in fis)
            {
                set.Add(fi.Name + "-" + fi.Length);
                Console.WriteLine("{0} | {1} {2}", fi.Name, fi.Length, fi.FullName);
            }
            return set;
        }
    }

    // This implementation defines a very simple comparison  
    // between two FileInfo objects. It only compares the name  
    // of the files being compared and their length in bytes.  
    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            if (f1.Name != f2.Name)
            {
                Console.WriteLine("File name mismatch: {0}; {1}", f1.Name, f2.Name);
                return false;
            }
            if (f1.Length != f2.Length)
            {
                Console.WriteLine("File Length mismatch: {0}-{1}; {2}-{3}", f1.Name, f1.Length, f2.Name, f2.Length);

                return false;
            }
            // TODO: Potentially compare the content of the files
            return true;
        }

        // Return a hash that reflects the comparison criteria.
        // According to the rules for IEqualityComparer<T>, if
        // Equals is true, then the hash codes must also be
        // equal. Because equality as defined here is a simple
        // value equality, not reference identity, it is possible
        // that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(FileInfo fi)
        {
            string s = $"{fi.Name}{fi.Length}";
            return s.GetHashCode();
        }
    }
}
