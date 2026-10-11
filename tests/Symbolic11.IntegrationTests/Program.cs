using System.Reflection;
using Symbolic11.Pages;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("Windows integration tests require NTFS.");

string root = Path.Combine(Path.GetTempPath(), "Symbolic11-Regression-" + Guid.NewGuid().ToString("N"));
string targets = Path.Combine(root, "Targets");
string links = Path.Combine(root, "Links");
Directory.CreateDirectory(targets);
Directory.CreateDirectory(links);
string targetFile = Path.Combine(targets, "original.txt");
string targetDir = Path.Combine(targets, "OriginalFolder");
Directory.CreateDirectory(targetDir);
File.WriteAllText(targetFile, "original file content");
File.WriteAllText(Path.Combine(targetDir, "keep.txt"), "preserve me");

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

try
{
    string fileSymlink = Path.Combine(links, "file-symlink.txt");
    File.CreateSymbolicLink(fileSymlink, targetFile);
    Check(new FileInfo(fileSymlink).LinkTarget is not null, "File symlink created");
    Check(File.ReadAllText(fileSymlink) == "original file content", "File symlink resolves");
    File.Delete(fileSymlink);
    Check(File.Exists(targetFile), "Deleting file symlink preserves target");

    string dirSymlink = Path.Combine(links, "directory-symlink");
    Directory.CreateSymbolicLink(dirSymlink, targetDir);
    Check(new DirectoryInfo(dirSymlink).LinkTarget is not null, "Directory symlink created");
    Directory.Delete(dirSymlink, false);
    Check(File.Exists(Path.Combine(targetDir, "keep.txt")), "Deleting directory symlink preserves target");

    MethodInfo hardLinkMethod = typeof(CreateLink).GetMethod("CreateHardLink", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException("CreateHardLink");
    string hardLink = Path.Combine(links, "hard-link.txt");
    bool created = (bool)(hardLinkMethod.Invoke(null, new object[] { hardLink, targetFile, IntPtr.Zero }) ?? false);
    Check(created, "Hard link created by application native method");
    Check(File.ReadAllText(hardLink) == "original file content", "Hard link resolves");
    File.Delete(hardLink);
    Check(File.Exists(targetFile), "Deleting hard link preserves original file");

    MethodInfo junctionMethod = typeof(CreateLink).GetMethod("CreateJunction", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException("CreateJunction");
    string junction = Path.Combine(links, "junction");
    junctionMethod.Invoke(null, new object[] { junction, targetDir });
    Check((File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0, "Junction created by application method");
    Check(File.Exists(Path.Combine(junction, "keep.txt")), "Junction resolves");
    Directory.Delete(junction, false);
    Check(File.Exists(Path.Combine(targetDir, "keep.txt")), "Deleting junction preserves target");

    string missing = Path.Combine(root, "missing-target");
    Check(!Directory.Exists(missing), "Invalid target fixture is absent");
    Check(!File.Exists(Path.Combine(links, "not-created")), "No unexpected link created");
    Console.WriteLine($"All {passed} integration assertions passed.");
}
finally
{
    // Never traverse links during cleanup; tests delete their own reparse points.
    if (Directory.Exists(root))
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(links))
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
                Directory.Delete(path, false);
            else File.Delete(path);
        }
        Directory.Delete(root, true);
    }
}
