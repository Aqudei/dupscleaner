using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Caliburn.Micro;
using DupFileCleaner.Views;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Action = System.Action;

namespace DupFileCleaner.ViewModels
{
    sealed class FileCleanerViewModel : Screen
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private string _folder;
        private string _logs = "";
        private bool _isBusy;
        private TextBox _logsTextBox;
        private bool _findAndDeleteVxOnly;
        private string _trash;

        private readonly bool _deferredDelete = false;

        public string Logs
        {
            get => _logs;
            set => Set(ref _logs, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                Set(ref _isBusy, value);
                NotifyOfPropertyChange(nameof(CanStartProcessing));
                NotifyOfPropertyChange(nameof(CanSelectFolder));
                NotifyOfPropertyChange(nameof(CanSwapFolderLevels));
            }
        }

        public bool CanSelectFolder => !IsBusy;

        public FileCleanerViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            DisplayName = "File Cleaner";
            //Execute.OnUIThread(() => Logs = Logs + s))
        }

        public void SelectFolder()
        {
            var directoryBrowser = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false
            };

            var result = directoryBrowser.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
                Folder = directoryBrowser.FileName;
        }

        public bool CanStartProcessing => !string.IsNullOrWhiteSpace(Folder) && Directory.Exists(Folder) && !IsBusy;

        public async void StartProcessing()
        {
            _logsTextBox.Clear();

            if (_deferredDelete)
            {
                _trash = Path.Combine(Path.GetDirectoryName(Folder), "trash");
                Directory.CreateDirectory(_trash);
            }

            IsBusy = true;

            await ProcessFolder(Folder);

            if (_deferredDelete)
            {
                await Task.Run(() => Directory.Delete(_trash, true));
            }

            IsBusy = false;
            Debug.WriteLine("Operation Completed");
        }

        public Task ProcessFolder(string startFolder)
        {
            if (FindAndDeleteVxOnly)
            {
                return Task.Run(() =>
                {
                    Debug.WriteLine($"Processing folder {startFolder}");
                    try
                    {
                        var files = Directory.EnumerateFiles(startFolder, "*_V*.*", SearchOption.AllDirectories);
                        var rgx = new Regex(@"_(v\d+)\.", RegexOptions.IgnoreCase);
                        foreach (var file in files)
                        {
                            try
                            {
                                if (rgx.IsMatch(Path.GetFileName(file)))
                                    File.Delete(file);
                                //Debug.WriteLine($"\tFile Deleted {file}");
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Error in file {file}");
                                Debug.WriteLine(e.Message);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($"Something went wrong while processing folder: {startFolder}");
                        Debug.WriteLine(exception.Message);
                    }

                    Debug.WriteLine($"Done processing folder {startFolder}");
                });
            }

            return Task.Run(() =>
            {

                try
                {
                    var files = Directory.EnumerateFiles(startFolder, "*_V*.*", SearchOption.TopDirectoryOnly);
                    ProcessFiles(startFolder, files);

                    var tasks = new List<Task>();
                    var folders = Directory.EnumerateDirectories(startFolder);

                    foreach (var folder in folders)
                    {
                        tasks.Add(ProcessFolder(folder));
                    }

                    Task.WaitAll(tasks.ToArray());
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Something went wrong while processing folder: {startFolder}");
                    Debug.WriteLine(e.Message);
                }
            });
        }

        public bool FindAndDeleteVxOnly
        {
            get => _findAndDeleteVxOnly;
            set => Set(ref _findAndDeleteVxOnly, value);
        }

        private void ProcessFiles(string folder, IEnumerable<string> files)
        {
            try
            {
                var myFiles = files.Select(s => new MyFile(s))
                    .Where(file => !string.IsNullOrWhiteSpace(file.FileVersion))
                    .GroupBy(file => file.FilenameOnlyWithoutVersion + file.FileExtention);

                foreach (var myFile in myFiles)
                {
                    var sortedFiles = myFile.OrderByDescending(file => file.FileVersion).ToArray();

                    for (int i = sortedFiles.Length; i-- > (File.Exists(Path.Combine(folder, myFile.Key)) ? 0 : 1);)
                    {
                        if (!_deferredDelete)
                            File.Delete(sortedFiles[i].FullName);
                        else
                            File.Move(sortedFiles[i].FullName, Path.Combine(_trash,
                                Guid.NewGuid().ToString()));
                    }
                }

                Debug.WriteLine($"\tDone processing folder {folder}");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public string Folder
        {
            get => _folder;
            set
            {
                Set(ref _folder, value);
                NotifyOfPropertyChange(nameof(CanStartProcessing));
                NotifyOfPropertyChange(nameof(CanSwapFolderLevels));
            }
        }


        public bool CanSwapFolderLevels => !string.IsNullOrWhiteSpace(Folder) && !IsBusy
                                                                              && Directory.Exists(Folder);
        //public async Task SwapFolderLevels()
        //{
        //    if (!Path.GetFileName(Folder).ToUpper().Contains("CLIENT FILES"))
        //    {
        //        await _dialogCoordinator.ShowMessageAsync(this, "Cannot proceed",
        //            "Please point the input folder to your 'CLIENT FILES' location");

        //        return;
        //    }

        //    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DupCleaner");
        //    var rgxDigit = new Regex(@"\d\d\d\d");
        //    var files = Directory.EnumerateFiles(Folder, "*.*", SearchOption.AllDirectories);
        //    var permanents = new HashSet<string>();

        //    foreach (var file in files)
        //    {
        //        var folderParts = file.Replace(Folder, "").Trim("\\/".ToCharArray()).Split("\\/".ToCharArray());

        //        //if (folderParts.Length < 2 || !rgxDigit.IsMatch(folderParts[1]))
        //        if (folderParts.Length < 2)
        //            continue;

        //        var tmp = folderParts[0];
        //        if (!rgxDigit.IsMatch(folderParts[1]))
        //        {
        //            permanents.Add(folderParts[1]);
        //        }

        //        folderParts[0] = folderParts[1];
        //        folderParts[1] = tmp;
        //        var newPath = Path.Combine(Folder, Path.Combine(folderParts));

        //        Directory.CreateDirectory(Path.GetDirectoryName(newPath));

        //        if (file.ToUpper() == newPath.ToUpper())
        //            continue;

        //        File.Move(file, newPath);
        //    }

        //    //CLeanup
        //    var folders = Directory.EnumerateDirectories(Folder);
        //    foreach (var folder in folders)
        //    {
        //        var folderName = Path.GetFileName(folder);
        //        if (rgxDigit.IsMatch(folderName) || permanents.Contains(folderName))
        //            continue;
        //        try
        //        {
        //            Directory.Delete(folder, true);
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.WriteLine(e.Message);
        //        }
        //    }

        //    await _dialogCoordinator.ShowMessageAsync(this, "Done", "Operation completed");
        //}

        private void MoveFile(string source, string destination)
        {
            try
            {
                if (source.ToUpper() == destination.ToUpper())
                    return;

                var directory = Path.GetDirectoryName(source);
                Directory.CreateDirectory(directory);

                File.Move(source, destination);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Cannot move {source} to {destination} because\n{e.Message}");
            }
        }

        private void RoboCopy(string sourceFolder, string destinationFolder)
        {
            Debug.WriteLine($"Running RoboCopy... Copy from {sourceFolder} to {destinationFolder}");

            Directory.CreateDirectory(destinationFolder);
            var startInfo = new ProcessStartInfo
            {
                FileName = "robocopy.exe",
                Arguments = $"\"{sourceFolder}\" \"{destinationFolder}\" *.* /E",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                process.Close();
            }
        }

        public async Task SwapFolderLevels()
        {
            if (!Path.GetFileName(Folder).ToUpper().Contains("CLIENT FILES"))
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Cannot proceed",
                    "Please point the input folder to your 'CLIENT FILES' location");

                return;
            }

            try
            {
                Debug.WriteLine("Swap operation started.");

                var rgxDigit = new Regex(@"\d\d\d\d");
                var clients = Directory.EnumerateDirectories(Folder, "*", SearchOption.TopDirectoryOnly);
                foreach (var client in clients)
                {
                    var clientCode = Path.GetFileName(client);
                    if (rgxDigit.IsMatch(clientCode) || clientCode == "Permanent")
                        continue;

                    var clientFiles = Directory.EnumerateFiles(client, "*.*", SearchOption.TopDirectoryOnly);
                    var clientFolders = Directory.EnumerateDirectories(client, "*", SearchOption.TopDirectoryOnly);

                    foreach (var clientFile in clientFiles)
                    {
                        MoveFile(clientFile, Path.Combine(Folder, "Permanent", clientCode));
                    }

                    foreach (var clientFolder in clientFolders)
                    {
                        var lastPath = Path.GetFileName(clientFolder).Trim();
                        if (rgxDigit.IsMatch(lastPath))
                        {
                            var year = rgxDigit.Match(lastPath);
                            RoboCopy(clientFolder, Path.Combine(Folder, year.Groups[0].Value, clientCode));
                        }
                        else if (lastPath.ToLower() == "permanent")
                        {
                            RoboCopy(clientFolder, Path.Combine(Folder, "Permanent", clientCode));
                        }
                        else
                        {
                            RoboCopy(clientFolder, Path.Combine(Folder, "Permanent", clientCode, lastPath));
                        }
                    }
                }
                // Cleanup

                var directories = Directory.EnumerateDirectories(Folder);
                foreach (var directory in directories)
                {
                    var lastPart = Path.GetFileName(directory);
                    if (lastPart.Contains("Permanent") || rgxDigit.IsMatch(lastPart))
                        continue;
                    Directory.Delete(directory, true);
                }

                Debug.WriteLine("Swap Operation Completed!");
                await _dialogCoordinator.ShowMessageAsync(this, "Success", "Swap Operation Completed!");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        protected override void OnViewReady(object view)
        {
            if (view is FileCleanerView fileCleanerView)
            {
                _logsTextBox = fileCleanerView.Logs;

                Debug.Listeners.Add(new DebugListener(s =>
                {
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        Logs = Logs + s;
                    }));
                }));
            }
        }
    }
}
