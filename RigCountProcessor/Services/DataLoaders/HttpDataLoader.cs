﻿namespace RigCountProcessor.Services.DataLoaders;

public class HttpDataLoader : IDataLoader
{
    private readonly HttpClient _httpClient;

    public HttpDataLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;

        ConfigureHttpClient();
    }

    // Configure http client to fully mimic a browser
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();

        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        );
        _httpClient.DefaultRequestHeaders.Add(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
        );
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<DataStream> LoadDataAsync(string fileLocation, CancellationToken cancellationToken = default)
    {
        if (fileLocation == null)
        {
            throw new IncorrectSettingsException(
                "Missing SourceFileLocation setting. Check appsettings.json file.");
        }

        var uri = new Uri(fileLocation);

        try
        {
            // Use ResponseHeadersRead to start processing as soon as headers are available
            HttpResponseMessage response =
                await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Throw an exception if the call is not successful
            response.EnsureSuccessStatusCode();

            string mediaType = GetMediaType(response);

            // Create a memory stream that can be returned while allowing the response to be disposed
            var memoryStream = new MemoryStream();

            await using (Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                await contentStream.CopyToAsync(memoryStream, cancellationToken);
            }
                
            if (memoryStream.Length == 0)
            {
                throw new HttpDataLoadException("Invalid data, memory stream is empty");
            }

            // Reset position to beginning so the caller can read from the start
            memoryStream.Position = 0;

            return new DataStream(mediaType, memoryStream);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpDataLoadException($"HTTP request error when downloading from {uri}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpDataLoadException("Request timed out when downloading from {uri}", ex);
        }
        catch (Exception ex)
        {
            throw new HttpDataLoadException("Unexpected error when downloading from {uri}", ex);
        }
    }

    private static string GetMediaType(HttpResponseMessage response)
    {
        // In case of an empty string, an exception will be thrown in the factory class
        return response.Content.Headers.ContentType?.MediaType ?? "";
    }
}