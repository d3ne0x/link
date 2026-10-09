using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using Path = System.IO.Path;

namespace Symbolic11.Pages;

/// <summary>
/// Interaction logic for CreateLink.xaml
/// </summary>
public partial class CreateLink : Page
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);
    // Junctions are directory reparse points. Set their target using FSCTL_SET_REPARSE_POINT.
    private const uint FsctlSetReparsePoint = 0x000900A4;
    private const uint IoReparseTagMountPoint = 0xA0000003;
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle handle,
        uint controlCode, byte[] input, int inputSize, IntPtr output, int outputSize,
        out int returned, IntPtr overlapped);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle OpenDirectoryHandle(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    private static void CreateJunction(string linkPath, string targetPath)
    {
        string absoluteTarget = Path.GetFullPath(targetPath);
        if (!Path.IsPathFullyQualified(absoluteTarget))
            throw new ArgumentException("Junction target must be an absolute path.");
        string substitute = @"\??\" + absoluteTarget;
        byte[] substituteBytes = System.Text.Encoding.Unicode.GetBytes(substitute);
        byte[] printBytes = System.Text.Encoding.Unicode.GetBytes(absoluteTarget);
        int pathLength = substituteBytes.Length + 2 + printBytes.Length + 2;
        byte[] buffer = new byte[16 + pathLength];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), IoReparseTagMountPoint);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(4, 2), checked((ushort)(8 + pathLength)));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(8, 2), 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(10, 2), checked((ushort)substituteBytes.Length));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(12, 2), checked((ushort)(substituteBytes.Length + 2)));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(14, 2), checked((ushort)printBytes.Length));
        Buffer.BlockCopy(substituteBytes, 0, buffer, 16, substituteBytes.Length);
        Buffer.BlockCopy(printBytes, 0, buffer, 18 + substituteBytes.Length, printBytes.Length);

        Directory.CreateDirectory(linkPath);
        try
        {
            using var handle = OpenDirectoryHandle(linkPath, 0x40000000, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
            if (handle.IsInvalid)
                throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            if (!DeviceIoControl(handle, FsctlSetReparsePoint, buffer, buffer.Length,
                IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        }
        catch
        {
            // Only remove the empty directory we just created.
            if (Directory.Exists(linkPath) &&
                (File.GetAttributes(linkPath) & FileAttributes.ReparsePoint) == 0)
                Directory.Delete(linkPath, false);
            throw;
        }
    }

    public string linkFileType = "folder";
    public CreateLink()
    {
        InitializeComponent();

        if (!IsDeveloperModeEnabled() && !IsUserAdministrator()) {
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
            InfoBar.Title = "Insufficient Permissions";
            InfoBar.Message = "The application is not running with administrative privileges. Link creation may fail.";
            InfoBar.IsOpen = true;
        }

    }
    private bool IsUserAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
    private bool IsDeveloperModeEnabled()
    {
        const string developerModeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
        const string developerModeValue = "AllowDevelopmentWithoutDevLicense";

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(developerModeKey))
        {
            if (key != null)
            {
                object value = key.GetValue(developerModeValue);
                if (value != null && (int)value == 1)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void FileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (DestinationPath == null) { return; }

        var symbFileType = FileType.SelectedItem.ToString()?.Split(new string[] { ": " }, StringSplitOptions.None).Last();

        DestinationPath.Text = ""; //Clear path
        CreateLinkButton.IsEnabled = false;

        switch (symbFileType)
        {
            case "File":
                linkFileType = "file";
                DestinationText.Content = "Destination File";
                HardLink.IsEnabled = true;
                JunctionLink.IsEnabled = false;
                if (SymbolicType.Text.Equals(JunctionLink.Content))
                {
                    SymbolicType.SelectedIndex = 0;
                }
                break;
            case "Folder":
                linkFileType = "folder";
                DestinationText.Content = "Destination Folder";
                HardLink.IsEnabled = false;
                JunctionLink.IsEnabled = true;
                if (SymbolicType.Text.Equals(HardLink.Content))
                {
                    SymbolicType.SelectedIndex = 0;
                }
                break;
            default:
                linkFileType = "none";
                break;
        }

        if (linkFileType != "none")
        {
            DestinationExplore.IsEnabled = true;
            LinkExplore.IsEnabled = true;
        }
        else
        {
            DestinationExplore.IsEnabled = false;
            LinkExplore.IsEnabled = false;
        }
    }

    private void LinkExplore_Click(object sender, RoutedEventArgs e)
    {
       
        using var folderDialog = new FolderBrowserDialog();
        DialogResult result = folderDialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(folderDialog.SelectedPath))
        {
            LinkFolderPath.Text = folderDialog.SelectedPath;
        }
        validate_CreateLink();
    }

    private void DestinationExplore_Click(object sender, RoutedEventArgs e) {
        if (linkFileType == "folder") {
            using var folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(folderDialog.SelectedPath)) {
                DestinationPath.Text = folderDialog.SelectedPath;
            }
        } else if (linkFileType == "file") {
            using var fileDialog = new System.Windows.Forms.OpenFileDialog();
            DialogResult result = fileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fileDialog.FileName)) {
                DestinationPath.Text = fileDialog.FileName;
            }
        }
        validate_CreateLink();
    }
    private void useDestinationName(object sender, RoutedEventArgs e)
    {
        if (DestinationPath.Text != null)
        {
            string fileName = Path.GetFileName(DestinationPath.Text);
            LinkName.Text = Path.GetFileNameWithoutExtension(fileName);
        }
    }

    private void validate_CreateLink()
    {
        if (!string.IsNullOrEmpty(LinkFolderPath.Text) && !string.IsNullOrEmpty(DestinationPath.Text))
        {
            CreateLinkButton.IsEnabled = true;
        }
        else
        {
            CreateLinkButton.IsEnabled = false;
        }
    }

    private void CreateLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(LinkName.Text) || string.IsNullOrWhiteSpace(LinkName.Text))
        {
            //Invalid name
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
            InfoBar.Title = "Invalid Name!";
            InfoBar.Message = "";
            InfoBar.IsOpen = true;
        }
        else
        {
            //Create link
            InfoBar.IsOpen = false;
            string linkName = LinkName.Text;
            string linkFolderPath = LinkFolderPath.Text;

            // Combine the link path and name to get the complete link path
            string linkPath = System.IO.Path.Combine(linkFolderPath, linkName);

            string targetPath = DestinationPath.Text;

            try
            {
                if (linkName != Path.GetFileName(linkName) ||
                    linkName is "." or ".." ||
                    linkName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    linkName.EndsWith(' ') || linkName.EndsWith('.'))
                    throw new ArgumentException("Enter a valid file or folder name.");

                if (!Directory.Exists(linkFolderPath))
                    throw new DirectoryNotFoundException("The link parent folder does not exist.");

                bool isFile = linkFileType == "file";
                bool isFolder = linkFileType == "folder";
                if (!isFile && !isFolder)
                    throw new ArgumentException("Select a file or folder type.");

                if (isFile ? !File.Exists(targetPath) : !Directory.Exists(targetPath))
                    throw new FileNotFoundException("The selected destination does not exist.", targetPath);

                if (File.Exists(linkPath) || Directory.Exists(linkPath))
                    throw new IOException("The link path already exists or is invalid.");

                switch (SymbolicType.Text)
                {
                    case "Symbolic Link" when isFile:
                        File.CreateSymbolicLink(linkPath, targetPath);
                        break;
                    case "Symbolic Link" when isFolder:
                        Directory.CreateSymbolicLink(linkPath, targetPath);
                        break;
                    case "Hard Link" when isFile:
                        if (!CreateHardLink(linkPath, targetPath, IntPtr.Zero))
                            throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        break;
                    case "Junction Link" when isFolder:
                        // Junctions use the Windows mklink /J operation; invoke without a command shell
                        // only after paths have been validated in the dedicated junction service.
                        CreateJunction(linkPath, targetPath);
                    default:
                        throw new ArgumentException("The selected link type does not match the file type.");
                }

                InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
                InfoBar.Title = "Link created";
                InfoBar.Message = linkPath;
                InfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
                InfoBar.Title = "Link creation failed";
                InfoBar.Message = ex.Message;
                InfoBar.IsOpen = true;
            }
        }
    }

    private void SymbolicType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? SymbolicTypeText = SymbolicType.SelectedItem.ToString()?.Split(new string[] { ": " }, StringSplitOptions.None).Last();

        if (SymbolicTypeText.Equals(HardLink.Content))
        {
            LinkText.Content = "Link File";
        }
        else if (LinkText != null)
        {
            LinkText.Content = "Link Folder";
        }
    }
}
