using System.IO;
using System.Text.RegularExpressions;

namespace DupFileCleaner
{
    class MyFile
    {
        public string Folder { get; }
        public string FilenameOnlyWithoutVersion { get; }
        public string FileExtention { get; }
        public string FileVersion { get; }
        public string FullName { get; set; }


        public MyFile(string filename)
        {
            Folder = Path.GetDirectoryName(filename);
            FilenameOnlyWithoutVersion = GetFilenameWithoutVersion(Path.GetFileName(filename));
            FileExtention = Path.GetExtension(filename);
            FileVersion = filename.Contains("~") ? string.Empty : GetFileVersion(Path.GetFileName(filename));
            FullName = filename;
        }

        private string GetFilenameWithoutVersion(string filename)
        {
            var rgx = new Regex(@"^(.*)_(v\d+)\.", RegexOptions.IgnoreCase);
            var match = rgx.Match(filename);
            if (match.Success)
                return match.Groups[1].Value;
            return filename;
        }

        private string GetFileVersion(string filename)
        {
            var rgx = new Regex(@"_(v\d+)\.", RegexOptions.IgnoreCase);
            var match = rgx.Match(filename);
            if (match.Success)
            {
                return match.Groups[1].Value.ToUpper();
            }

            return string.Empty;
        }
    }
}
