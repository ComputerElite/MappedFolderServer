using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MappedFolderServer.Util;

namespace MappedFolderServer.Data;

public class GitRepo(string url)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Url { get; set; } = url;
    public string Branch { get; set; } = "main";
    public string? CurrentCommitHash { set; get; }
    public DateTime LastPulled { get; set; }
    public string? Username { get; set; }
    [NotMapped]
    public string? Password { get; set; }
    public string? EncryptedPassword { get; set; }
    [NotMapped]
    [JsonIgnore]
    public bool HasCredentials =>  Username != null && Password != null;

    /// <summary>
    /// Initializes a this git repo in the specified directory
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns>An error if something doesn't work out</returns>
    public ApiError? Init(string folderPath)
    {
        // first check if dir is empty
        if (Directory.GetFiles(folderPath).Length > 0 || Directory.GetDirectories(folderPath).Length > 0)
        {
            return new ApiError("Directory is not empty. Please choose another directory");
        }

        var co = new CloneOptions();
        co.BranchName = Branch;
        if (HasCredentials)
        {
            co.FetchOptions.CredentialsProvider = GetCredentials();
        }

        try
        {
            Repository.Clone(url, folderPath, co);
            CurrentCommitHash = new Repository(folderPath).Head.Tip.Sha;
            LastPulled = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            return new ApiError(e.Message);
        }
        return null;
    }

    public CredentialsHandler GetCredentials()
    {
        return (_url, _user, _cred) => new UsernamePasswordCredentials
            { Username = Username, Password = TokenEncryptor.Decrypt(EncryptedPassword) };
    }

    public ApiError? Update(string folderPath)
    {
        var credentials = GetCredentials();

        // --- UPDATE EXISTING REPO ---
        if (Repository.IsValid(folderPath))
        {
            using var repo = new Repository(folderPath);

            Console.WriteLine($"Updating repository on branch '{folderPath}'...");

            // Fetch latest from origin
            Commands.Fetch(repo, "origin", new[] { Branch }, new FetchOptions
            {
                CredentialsProvider = credentials
            }, null);

            // Checkout the branch (create local tracking branch if needed)
            if (repo.Head.FriendlyName != Branch)
            {
                Console.WriteLine("Switching branches...");

                var localBranch = repo.Branches[Branch] ??
                                  repo.CreateBranch(Branch, repo.Branches[$"origin/{Branch}"].Tip);

                Commands.Checkout(repo, localBranch);
            }

            // Pull
            Commands.Pull(repo,
                new Signature("AutoUpdater", "autoupdater@example.com", DateTimeOffset.Now),
                new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = credentials
                    }
                });
            CurrentCommitHash = new Repository(folderPath).Head.Tip.Sha;
            LastPulled = DateTime.UtcNow;

            return null;
        }

        return new ApiError("Invalid repo found");
    }
}