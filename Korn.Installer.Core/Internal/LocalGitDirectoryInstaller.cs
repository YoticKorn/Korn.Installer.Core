using System.Collections.Generic;
using Korn.Utils.GithubExplorer;
using Korn.Installer.Core;
using Korn.Interface;
using System.Linq;
using System.IO;
using Korn.Utils;
using Korn.Modules.WinApi;

class LocalGitDirectoryInstaller
{            
    static RepositoryID OverviewRepository = new RepositoryID("YoticKorn", "Overview");

    public LocalGitDirectoryInstaller(GithubEntry entry) 
        : this(entry.Name, entry.GithubPath, entry.DirectoryPath, entry.VersionFilePath, entry.VersionableFileName) { }

    public LocalGitDirectoryInstaller(string name, string githubPath, string directoryPath, string versionFilePath, string versionableFileName) 
        : this(name, githubPath, directoryPath, versionableFileName, new VersionFile(versionFilePath, name)) { }

    LocalGitDirectoryInstaller(string name, string githubPath, string directoryPath, string versionableFileName, VersionFile versionFile)
        => (this.name, this.githubPath, this.directoryPath, this.versionableFileName, this.versionFile) = (name, githubPath, directoryPath, versionableFileName, versionFile);

    string name, githubPath, directoryPath, versionableFileName;
    VersionFile versionFile;
    List<RepositoryEntryJson> entries;
    List<RepositoryEntryJson> Entries => entries != null ? entries : entries = GithubClient.Instance.GetAllRepositoryEntries(OverviewRepository, githubPath);
    string githubNormalizedPath => githubPath.Replace('/', '\\').TrimEnd('\\');

    public string Name => name;

    public long GetTotalBytes() => Entries.Sum(entry => entry.Size);

    public void Install(InstallTrace trace)
    {
        var totalBytes = GetTotalBytes();
        var part = new InstallTrace.Part(name, totalBytes);
        trace.SetPart(part);

        foreach (var entry in Entries)
        {
            var path = entry.Path.Replace('/', '\\').Substring(githubNormalizedPath.Length + 1);
            path = Path.Combine(directoryPath, path);

            if (entry.Type == "dir")
                Directory.CreateDirectory(path);
            else
            {
                var bytes = GithubClient.Instance.DownloadAsset(entry);
                File.WriteAllBytes(path, bytes);
                trace.AddDownloadedBytes(bytes.LongLength);
            }
        }

        var version = GetVersion();
        versionFile.SetVersion(version);
    }

    public bool IsOutdated() => versionFile.GetVersion() != GetVersion();

    string GetVersion()
    {
        var versionableEntry = Entries.Find(e => e.Name == versionableFileName);
        if (versionableEntry == null)
            User32.MessageBox(
                "Korn.Installer.Core.InstallerCore.LocalGitDirectoryInstaller->CheckUpdates:\n" +
               $"Can't find a versionable entry in the github overview directory for {name} module.\n" +
                "This could be due to problems uploading files to github or git failures.\n"
            );

        return versionableEntry.Sha;
    }
}