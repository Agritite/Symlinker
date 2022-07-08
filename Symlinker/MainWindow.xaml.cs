using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Agritite.Symlinker
{
    public static class PublicStrings
    {
        public const string File = "File",
                            Folder = "Folder",
                            Ready = "Ready",
                            Done = "Done",
                            DefaultName = "DefaultSymlink";
    }

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    ///

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //private field

        private ObservableCollection<Entry> DTSource;
        private bool mode; // true=Normal mode, false=osu! mode

        //public field

        public bool Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged("Mode");
                DTSource.Clear();     
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //constructor

        public MainWindow()
        {
            InitializeComponent();

            DTSource = new ObservableCollection<Entry>();
            filesDataGrid.ItemsSource = DTSource;
            Mode = true;
        }

        //private method

        private void Load_Files_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            for (int i = 0; i < openFileDialog.FileNames.Length; i++)
                DTSource.Add(new Entry(openFileDialog.FileNames[i], openFileDialog.SafeFileNames[i], true));
        }

        private void Add_Folder_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            string newname = Path.GetFileName(fbd.SelectedPath);
            if (string.IsNullOrEmpty(newname))
                newname = PublicStrings.DefaultName;
            DTSource.Add(new Entry(fbd.SelectedPath, newname, false));
        }

        private void Batch_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPathValidity(OutputTextBox.Text))
            {
                MessageBox.Show("Output Path does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (string.IsNullOrEmpty(OutputTextBox.Text))
            {
                MessageBox.Show("Output Path cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (DTSource.Count <= 0)
            {
                MessageBox.Show("There is nothing on the list!", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (Entry entry in DTSource)
            {
                if (entry.Status == PublicStrings.Done)
                    continue;
                if (!CheckFilenameValidity(OutputTextBox.Text, entry.OutputName))
                {
                    MessageBox.Show(entry.InputPath + " has an invalid  output name and will not be processed!",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    continue;
                }
                string outputPath = Path.Combine(OutputTextBox.Text, entry.OutputName);
                uint dwFlags = (entry.EntryType == PublicStrings.File) ? 0u : 1u;
                if (!CreateSymbolicLink(outputPath, entry.InputPath, dwFlags))
                {
                    MessageBox.Show("Cannot Create Symlink!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                    entry.Status = PublicStrings.Done;
            }
        }

        private void Output_Path_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            OutputTextBox.Text = fbd.SelectedPath;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new ObservableCollection<Entry>(DTSource);
            foreach (Entry entry in tmp)
            {
                if (entry.Status == PublicStrings.Done)
                    DTSource.Remove(entry);
            }
        }

        private void Reset_to_Ready_Click(object sender, RoutedEventArgs e)
        {
            foreach (Entry entry in filesDataGrid.SelectedItems)
                entry.Status = PublicStrings.Ready;
        }

        private void Load_Folder_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            foreach (string path in Directory.GetFiles(fbd.SelectedPath))
            {
                string newname = Path.GetFileName(path);
                DTSource.Add(new Entry(path, newname, true));
            }
            foreach(string path in Directory.GetDirectories(fbd.SelectedPath))
            {
                string newname = Path.GetFileName(path);
                if (string.IsNullOrEmpty(newname))
                    newname = PublicStrings.DefaultName;
                DTSource.Add(new Entry(path, newname, false));
            }
        }


        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //public method

        //static members

        static bool CheckFilenameValidity(string dir, string filename)
        {
            return !string.IsNullOrEmpty(filename) &&
                   filename.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   !File.Exists(Path.Combine(dir, filename));
        }

        static bool CheckPathValidity(string path)
        {
            return !String.IsNullOrWhiteSpace(path)
                    && path.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                    && Path.IsPathRooted(path)
                    && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                    && Directory.Exists(path);
        }

        [DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);

        //dwFlags: 0=File, 1=Directory
    }

    public class Entry : INotifyPropertyChanged
    {
        public Entry(string _inputpath, string _outputname, bool IsFile, bool IsReady = true)
        {
            InputPath = _inputpath;
            OutputName = _outputname;
            EntryType = IsFile ? PublicStrings.File : PublicStrings.Folder;
            Status = IsReady ? PublicStrings.Ready : PublicStrings.Done;
        }

        string inputPath;
        string outputName;
        string status;
        string entrytype;

        public string InputPath
        {
            get => inputPath;
            set
            {
                inputPath = value;
                OnPropertyChanged(nameof(InputPath));
            }
        }
        public string OutputName
        {
            get => outputName;
            set
            {
                outputName = value;
                OnPropertyChanged(nameof(OutputName));
            }
        }
        public string EntryType
        {
            get => entrytype;
            set
            {
                entrytype = value;
                OnPropertyChanged(nameof(EntryType));
            }
        }
        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
