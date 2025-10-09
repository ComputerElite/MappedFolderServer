using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using MappedFolderServer.Data;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MappedFolderServer.Scraping;

public class Scraper
{
    private List<string> Queued = new();
    private readonly string target = "http://localhost";
    List<string> _processingOrProcessed = new();
    List<string> _downloaded = new();
    List<Error> _errors = new();
    Regex r = new("https:\\/\\/[^\"']+");
    Regex relativeUrlRegex = new ("(href|src)=[\"'](?!http|data)([^\"']+)[\"']");
    private Dictionary<string, SlugEntry> allowedSlugs = new();

    public Scraper(IEnumerable<SlugEntry> entries)
    {
        foreach (SlugEntry entry in entries)
        {
            allowedSlugs.Add(entry.Slug, entry);
        }
    }

    string MakeRelativeUrl(string baseUrl, string url)
    {
        try
        {
            if (!Path.GetFileName(baseUrl).Contains('.') && !baseUrl.EndsWith('/')) baseUrl += "/";
            Uri baseUri = new Uri(baseUrl);
            Uri uri = new Uri(baseUri, url);
            string relative = baseUri.MakeRelativeUri(uri).ToString();
            if (!Path.GetFileName(uri.AbsolutePath).Contains('.'))
            {
                relative = relative.TrimEnd('/') + "/index.html";
            }
            return ("./" + relative).Replace("//", "/");
        }
        catch (Exception e)
        {
            return url;
        }
    }

    public List<string> processedFiles = new List<string>();
    private static readonly string[] bannedFolders = ["node_modules/", ".git/"];

    byte[] GetFile(string url, WebClient c)
    {
        if (!url.StartsWith(target))
        {
            if (!Config.Instance.DownloadFeatureEnabled)
            {
                throw new WebException("Download feature is disabled");
            }
            // get from web
            return c.DownloadData(url);
        }

        string relativePath = url.Substring(target.Length + 1);
        relativePath = Path.GetFullPath(relativePath, "/").Substring(1);
        //Console.WriteLine($"{url} => {relativePath}");
        string slug = relativePath.Substring(0,  relativePath.IndexOf('/'));
        string path = relativePath.Substring(relativePath.IndexOf('/') + 1);
        if (!allowedSlugs.ContainsKey(slug)) throw new FileNotFoundException("File not found", relativePath);
        string filePath = Path.Combine(allowedSlugs[slug].FolderPath, HttpUtility.UrlDecode(path));
        if(bannedFolders.Any(x => filePath.Contains(x))) throw new FileNotFoundException("File not found", filePath);
        if(!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);
        return File.ReadAllBytes(filePath);
    }

    void DownloadFile(string url, WebClient c, ZipArchive archive)
    {
        Console.WriteLine(url);
        Uri uri;
        try
        {
            uri = new Uri(url);
        } catch (Exception e)
        {
            Console.WriteLine("Error parsing " + url + ": " + e.Message);
            _errors.Add(new Error(url, -2));
            return;
        }
        string endFilePath = uri.Host + HttpUtility.UrlDecode(uri.AbsolutePath);
        string fileName = Path.GetFileName(uri.AbsolutePath);
        if (!fileName.Contains('.')) endFilePath += (endFilePath.EndsWith('/') ? "" : "/") + "index.html";
        
        if (processedFiles.Contains(endFilePath)) return;
        byte[] data;
        try
        {
            data = GetFile(url, c);
        }
        catch (WebException e)
        {
            HttpStatusCode? status = (e.Response as HttpWebResponse)?.StatusCode;
            Console.WriteLine((status.HasValue ? status.Value : "unknown error") + " at " + url);
            _errors.Add(new Error(url, (status.HasValue ? (int)status.Value : -1)));
            return;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("File not found at " + e.FileName);
            _errors.Add(new Error(url, 404));
            return;
        }
        
        string content = Encoding.UTF8.GetString(data);
        // Extract urls via regex
        if(content.Contains("<html"))
        {
            string newContent = content;
            foreach (Match match in relativeUrlRegex.Matches(content))
            {
                string relativeUrl = match.Groups[2].Value;
                string absoluteUrl;
            
                if(relativeUrl.StartsWith('/')) {
                    absoluteUrl = uri.Scheme + "://" + uri.Host + relativeUrl;
                    newContent = newContent.Replace("\"" + relativeUrl + "\"", MakeRelativeUrl(url, absoluteUrl));
                }
                else
                {
                    absoluteUrl = (url.EndsWith('/') ? url : url.Substring(0, url.LastIndexOf('/') + 1)) + relativeUrl;
                }
                //Console.WriteLine($"Relative: {relativeUrl}   from   {url} =>  Absolute URL: " + absoluteUrl);
                absoluteUrl = absoluteUrl.Split('#')[0];
                if (!Queued.Contains(absoluteUrl) && !_processingOrProcessed.Contains(absoluteUrl))
                {
                    Queued.Add(absoluteUrl);
                }
            }
            foreach (Match match in r.Matches(content))
            {
                if (!match.Value.StartsWith(target)) continue;
                if (!Queued.Contains(match.Value) && !_processingOrProcessed.Contains(match.Value))
                {
                    Queued.Add(match.Value);
                }

                string relative = MakeRelativeUrl(url, match.Value);
                Console.WriteLine(url + " -> " + match.Value + " = " + relative + " exists: " + newContent.Contains(match.Value));
                newContent = newContent.Replace(match.Value + "\"", relative + "\"");
            }
            data = Encoding.UTF8.GetBytes(newContent);
        }
        
        lock (archive)
        {
            Stream s = archive.CreateEntry(endFilePath, CompressionLevel.Fastest).Open();
            s.Write(data);
            s.Close();
        }   
    }
    
    public byte[] ScrapeSlug(SlugEntry slug)
    {
        string dir = slug.FolderPath;
        if (!dir.EndsWith("/")) dir += "/";
        Queued = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToList().ConvertAll(x =>
        {
            return $"{target}/{slug.Slug}/{x.Substring(dir.Length)}";
        });
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            while (Queued.Count > 0)
            {
                List<string> currentBatch;
                lock (Queued)
                {
                    currentBatch = new List<string>(Queued);
                    _processingOrProcessed.AddRange(currentBatch);
                    Queued.Clear();
                }

                Parallel.ForEach(currentBatch, url =>
                {
                    WebClient c = new WebClient();
                    DownloadFile(url, c, archive);
                    lock (_downloaded)
                    {
                        _downloaded.Add(url);
                    }

                    Console.WriteLine(_downloaded.Count + " downloaded, " + Queued.Count + " queued, " + _processingOrProcessed.Count + " processing");
                });
            }
        }
        Console.WriteLine("Done");
        
        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }
}

class Error
{
    public string url;
    public int status;
    public Error(string url, int status) {
        this.url = url;
        this.status = status;
    }
}