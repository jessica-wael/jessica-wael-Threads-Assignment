using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Schema;
using OrderProcessing.Shared.Exceptions;

namespace OrderProcessing.Shared.Storage;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
      private static readonly SemaphoreSlim  Semaphore = new(1, 1);
      
    public static async Task<List<T>> ReadAsync<T>(string filename, CancellationToken ct = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", filename);
        if (!File.Exists(path))
        {
            return new List<T>();
        }
        var json = await File.ReadAllTextAsync(path, ct);
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, Options)?? new List<T>();
        }
        catch (JsonException)
        {
            throw new DataFileCorruptedException(filename);
        }
    }
    public static List<T> Read<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (!File.Exists(path)) return new List<T>();

        var json = File.ReadAllText(path);
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
        }
        catch (JsonException)
        {
            throw new DataFileCorruptedException(fileName);
        }
    }
    public static async Task WriteAsync<T>(string filename, List<T> items, CancellationToken ct = default)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", filename);
            var tempPath = path + ".tmp";

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(items, Options);
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            Semaphore.Release();
        }

    }
    
    public static void Write<T>(string fileName, List<T> items)
    {
        Semaphore.Wait();
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
            var tempPath = path + ".tmp";

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(items, Options);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}