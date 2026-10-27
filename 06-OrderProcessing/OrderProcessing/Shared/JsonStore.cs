using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1
{
    public static class JsonFileStore
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        private static SemaphoreSlim GetFileLock(string filePath)
        {
            return _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        }


        public static async Task<List<T>> ReadAsync<T>(string filePath, CancellationToken ct = default)
        {
            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync(ct); // استنى دورك لو في حد تاني بيقرأ/بيكتب

            try
            {
                if (!File.Exists(filePath)) return new List<T>();

                string json = await File.ReadAllTextAsync(filePath, ct);

                if (string.IsNullOrWhiteSpace(json)) return new List<T>();

                return JsonSerializer.Deserialize<List<T>>(json, JsonConfig.Options) ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(Path.GetFileName(filePath), ex);
            }
            finally
            {
                fileLock.Release(); 
            }
        }

        public static async Task WriteAsync<T>(string filePath, IEnumerable<T> data, CancellationToken ct = default)
        {
            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync(ct);

            try
            {
                string tempFilePath = filePath + ".tmp";
                string json = JsonSerializer.Serialize(data, JsonConfig.Options);

                await File.WriteAllTextAsync(tempFilePath, json, ct);

                File.Move(tempFilePath, filePath, overwrite: true);
            }
            finally
            {
                fileLock.Release();
            }
        }


        public static List<T> Read<T>(string filePath)
        {
            var fileLock = GetFileLock(filePath);
            fileLock.Wait(); // Sync wait

            try
            {
                if (!File.Exists(filePath)) return new List<T>();
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<T>();
                return JsonSerializer.Deserialize<List<T>>(json, JsonConfig.Options) ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(Path.GetFileName(filePath), ex);
            }
            finally
            {
                fileLock.Release();
            }
        }
        public static void Write<T>(string filePath, IEnumerable<T> data)
        {
            var fileLock = GetFileLock(filePath);
            fileLock.Wait();

            try
            {
                string tempFilePath = filePath + ".tmp";
                string json = JsonSerializer.Serialize(data, JsonConfig.Options);
                File.WriteAllText(tempFilePath, json);
                File.Move(tempFilePath, filePath, overwrite: true);
            }
            finally
            {
                fileLock.Release();
            }
        }
    }
}

