using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Align.Tests
{
    [TestClass]
    public class FindTests
    {
        private const string copyTarget = @"C:\Users\jakka\temp\pt\2";

        [TestMethod]
        public void Find()
        {
            var b = @"E:\ptbackup\tripods";
            _recurse(b);
            
        }

        void _recurse(string directory)
        {
            Debug.WriteLine($"Processing: {directory}");
            var d = new DirectoryInfo(directory);
            var f = d.GetFiles("*.jpg");
            if (d.Name == "orig" && f.Length > 6)
            {
                var newDir = new DirectoryInfo(Path.Join(copyTarget, d.Parent.Name));
                if (!Directory.Exists(newDir.FullName))
                {
                    Directory.CreateDirectory(newDir.FullName);
                }
                foreach (var file in f)
                {
                    file.CopyTo(Path.Join(newDir.FullName, file.Name));
                }
            }

            foreach (var subDir in d.GetDirectories())
            {
                _recurse(subDir.FullName);
            }
        }
    }
}
