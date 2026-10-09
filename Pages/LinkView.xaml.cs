using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Symbolic11.Pages;
/// <summary>
/// Interaction logic for LinkView.xaml
/// </summary>
/// 
public partial class LinkView : Page
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileAttributesEx(string name, GetFileAttributesExInfo infoLevel, ref REPARSE_POINT_DATA lpReparsePointData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetPathFromName(string pszPath, ref IntPtr pszPathOut);

    private string GetShortcutTarget(string file)
    {
        try
        {
            if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
            {
                throw new Exception("Supplied file must be a .LNK file");
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
            {
                fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                uint flags = fileReader.ReadUInt32();        // Read flags
                if ((flags & 1) == 1)
                {                      // Bit 1 set means we have to
                                       // skip the shell item ID list
                    fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                    uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                    fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                }

                long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                             // structure begins
                uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                           // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                    // base pathname (target)
                long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                    // the base pathname. I don't need the 2 terminating nulls.
                char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                var link = new string(linkTarget);

                int begin = link.IndexOf("\0\0");
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2) + 2;
                    end = link.IndexOf('\0', end) + 1;

                    string firstPart = link.Substring(0, begin);
                    string secondPart = link.Substring(end);

                    return firstPart + secondPart;
                }
                else
                {
                    return link;
                }
            }
        } catch
        {
            return "";
        }
    }

    [Flags]
    public enum GetFileAttributesExInfo : uint
    {
        Basic = 0,
        ReparsePoint = 1,
        Directory = 2,
        Volume = 3,
        FileSize = 4,
        FileAllocationSize = 5,
        ChangeTime = 6,
        AccessTime = 7,
        CreationTime = 8,
        InternalInfo = 9,
        All = Basic | ReparsePoint | Directory | Volume | FileSize | FileAllocationSize | ChangeTime | AccessTime | CreationTime | InternalInfo
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct REPARSE_POINT_DATA
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Data;
        public uint ReparseTag;
        public ushort Reserved;
        public char[] ReparseTarget;
        public short SubsNameOffset;
        public short SubsNameLength;
        public ushort ReparseDataLength;
    }
    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    private const uint IO_REPARSE_TAG_HSM = 0xC0000004;
    private const uint IO_REPARSE_TAG_SIS = 0x80000007;
    private const uint IO_REPARSE_TAG_DFS = 0x8000000A;
    private const uint IO_REPARSE_TAG_FILTER_MANAGER = 0x8000000B;
    private const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

    //---------------------------------------------------

    public class Link
    {
        public string Source
        {
            get; set;
        }
        public string Type
        {
            get; set;
        }
        public string Target
        {
            get; set;
        }
        public bool Result
        {
            get; set;
        }
    }


    public string defaultSearchPath = "C:\\Users\\";
    public string currentSearchPath = "C:\\Users\\";
    ObservableCollection<Link> links = new ObservableCollection<Link>();

    private EnumerationOptions enumerationOptions = new EnumerationOptions
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System,
        RecurseSubdirectories = true,
        MaxRecursionDepth = 5,
    };
    private CancellationTokenSource? cancellationTokenSource;
    private Task? searchTask;
    public LinkView()
    {
        InitializeComponent();
        SearchPathText.Text = defaultSearchPath;

        linkDataGrid.ItemsSource = links;

        linkDataGrid.Focus();
    }

    private async Task PerformSearchAsync(string root, CancellationToken token)
    {
        try
        {
            await Dispatcher.InvokeAsync(() => links.Clear());
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 5,
                ReturnSpecialDirectories = false
            };
            await Task.Run(async () =>
            {
                foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", options))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var attributes = File.GetAttributes(path);
                        bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
                        bool isReparse = attributes.HasFlag(FileAttributes.ReparsePoint);
                        bool isShortcut = !isDirectory && path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
                        if (!isReparse && !isShortcut) continue;
                        string? target = isShortcut ? GetShortcutTarget(path)
                            : isDirectory ? new DirectoryInfo(path).LinkTarget : new FileInfo(path).LinkTarget;
                        string type = isShortcut ? "Shortcut" : isDirectory ? "Directory Link" : "File Link";
                        string? resolved = target;
                        if (!string.IsNullOrWhiteSpace(target) && !Path.IsPathFullyQualified(target))
                            resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target));
                        bool exists = !string.IsNullOrWhiteSpace(resolved) &&
                            (File.Exists(resolved) || Directory.Exists(resolved));
                        var item = new Link { Source = path, Type = type, Target = target ?? "",
                            Result = exists };
                        await Dispatcher.InvokeAsync(() =>
                        {
                            links.Add(item);
                            currentSearchDirectory.Text = path;
                        });
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
                System.Windows.MessageBox.Show(ex.Message, "Search failed",
                    MessageBoxButton.OK, MessageBoxImage.Error));
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                searchButton.Content = "Search";
                currentSearchDirectory.Text = "";
                searchTask = null;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            });
        }
    }

    private void searchButton_Click(object sender, RoutedEventArgs e)
    {
        if (searchTask != null)
        {
            cancellationTokenSource?.Cancel();
            searchButton.Content = "Stopping...";
            searchButton.IsEnabled = false;
            _ = searchTask.ContinueWith(_ => Dispatcher.Invoke(() => searchButton.IsEnabled = true));
            return;
        }
        if (!Directory.Exists(currentSearchPath))
        {
            System.Windows.MessageBox.Show("Select an existing folder.", "Invalid search path");
            return;
        }
        cancellationTokenSource = new CancellationTokenSource();
        searchButton.Content = "Cancel";
        searchTask = PerformSearchAsync(currentSearchPath, cancellationTokenSource.Token);
    }

    private void changeSearchDirectory_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog();
        DialogResult result = folderDialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(folderDialog.SelectedPath))
        {
            SearchPathText.Text = folderDialog.SelectedPath;
            currentSearchPath = folderDialog.SelectedPath;
        }
    }

    private void linkDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool enableDelete = false;
        if (linkDataGrid.SelectedItem != null)
        {
            if (linkDataGrid.SelectedItem.GetType() == typeof(Link))
            {
                Link selectedLink = (Link)linkDataGrid.SelectedItem;

                enableDelete = (selectedLink.Source != null);
            }
        }
        DeleteLink.IsEnabled = enableDelete;
    }

    private void linkDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (linkDataGrid.SelectedItem is Link selectedLink) {
            if (!string.IsNullOrEmpty(selectedLink.Source)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{selectedLink.Source}\"");
                } catch (Exception ex) {
                    var messageBox = new Wpf.Ui.Controls.MessageBox {
                        Title = "Error",
                        Content = $"Failed to open link in File Explorer: {ex.Message}",
                        CloseButtonText = "OK",
                    };
                    messageBox.ShowDialogAsync();
                }
            }
        }
    }

    private void DeleteLink_Click(object sender, RoutedEventArgs e)
    {
        if (linkDataGrid.SelectedItem is not Link selected) return;
        if (System.Windows.MessageBox.Show(
            $"Remove this link only?\\n{selected.Source}\\n\\nThe target will not be deleted.",
            "Confirm link deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            string path = selected.Source;
            FileAttributes attrs = File.GetAttributes(path);
            bool directory = attrs.HasFlag(FileAttributes.Directory);
            bool reparse = attrs.HasFlag(FileAttributes.ReparsePoint);
            bool shortcut = !directory && path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
            if (!reparse && !shortcut)
                throw new IOException("The selected item is no longer a link. Nothing was deleted.");
            if (shortcut && selected.Type != "Shortcut")
                throw new IOException("The selected item has changed since the scan.");
            if (reparse && selected.Type == "Shortcut")
                throw new IOException("The selected item has changed since the scan.");
            if (directory) Directory.Delete(path, false);
            else File.Delete(path);
            links.Remove(selected);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Link deletion failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
