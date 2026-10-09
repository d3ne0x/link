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

                if (File.Exists(linkPath) || Directory.Exists(linkPath) ||
                    (File.GetAttributes(linkFolderPath) & FileAttributes.ReparsePoint) != 0 &&
                    Path.GetFullPath(linkPath) == Path.GetFullPath(linkFolderPath))
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
                        File.CreateHardLink(linkPath, targetPath);
                        break;
                    case "Junction Link" when isFolder:
                        // Junctions use the Windows mklink /J operation; invoke without a command shell
                        // only after paths have been validated in the dedicated junction service.
                        throw new NotSupportedException("Junction creation is temporarily disabled until a safe native implementation is available.");
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
