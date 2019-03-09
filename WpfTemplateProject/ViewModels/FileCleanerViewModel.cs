﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Caliburn.Micro;
using DupFileCleaner.Views;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Action = System.Action;

namespace DupFileCleaner.ViewModels
{
    sealed class FileCleanerViewModel : Screen
    {
        private string _folder;
        private string _logs = "";
        private bool _isBusy;
        private TextBox _logsTextBox;
        private bool _findAndDeleteVxOnly;

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
            }
        }

        public bool CanSelectFolder => !IsBusy;

        public FileCleanerViewModel()
        {
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

            IsBusy = true;
            await ProcessFolder(Folder);
            IsBusy = false;

            Debug.WriteLine("Operation Completed");
            Execute.OnUIThread(() => _logsTextBox?.ScrollToEnd());
        }

        public Task ProcessFolder(string startFolder)
        {
            Debug.WriteLine("Processing Folder: " + startFolder);

            if (FindAndDeleteVxOnly)
            {
                return Task.Run(() =>
                {
                    var files = Directory.EnumerateFiles(startFolder, "*_V*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                        Debug.WriteLine($"\tFile Deleted {file}");
                    }
                });
            }

            return Task.Run(() =>
            {

                var files = Directory.EnumerateFiles(startFolder, "*.*", SearchOption.TopDirectoryOnly);
                ProcessFiles(startFolder, files);

                var tasks = new List<Task>();
                var folders = Directory.GetDirectories(startFolder);
                foreach (var folder in folders)
                {
                    tasks.Add(ProcessFolder(folder));
                }

                Task.WaitAll(tasks.ToArray());
            });
        }

        public bool FindAndDeleteVxOnly
        {
            get => _findAndDeleteVxOnly;
            set => Set(ref _findAndDeleteVxOnly, value);
        }

        private void ProcessFiles(string folder, IEnumerable<string> files)
        {

            var myFiles = files.Select(s => new MyFile(s))
                .Where(file => !string.IsNullOrWhiteSpace(file.FileVersion))
                .GroupBy(file => file.FilenameOnlyWithoutVersion + file.FileExtention);

            foreach (var myFile in myFiles)
            {
                var sortedFiles = myFile.OrderByDescending(file => file.FileVersion).ToArray();

                for (int i = sortedFiles.Length; i-- > (File.Exists(Path.Combine(folder, myFile.Key)) ? 0 : 1);)
                {

                    File.Delete(sortedFiles[i].FullName);
                    Debug.WriteLine($"\tFile Deleted {sortedFiles[i].FullName}");
                }
            }
        }

        public string Folder
        {
            get => _folder;
            set
            {
                Set(ref _folder, value);
                NotifyOfPropertyChange(nameof(CanStartProcessing));
            }
        }

        protected override void OnViewReady(object view)
        {
            if (view is FileCleanerView fileCleanerView)
            {
                _logsTextBox = fileCleanerView.Logs;

                Debug.Listeners.Add(new DebugListener(s =>
                {
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Logs = Logs + s;
                    }));
                }));
            }
        }
    }
}
