using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class JsonSaveRepository
    {
        private readonly string _rootPath;
        private static long _changeVersion;
        // Shared across repository instances: a settings/profile reset or another
        // SaveService must invalidate readers of the same on-disk data.
        public static long ChangeVersion => Interlocked.Read(ref _changeVersion);

        public string RootPath => _rootPath;

        public JsonSaveRepository(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Save root path cannot be empty.", nameof(rootPath));

            _rootPath = rootPath;
        }

        public bool Exists(string fileName)
        {
            return File.Exists(GetPath(fileName));
        }

        public T Load<T>(string fileName) where T : class, new()
        {
            string path = GetPath(fileName);
            if (!File.Exists(path))
                return new T();

            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                return new T();

            T data = JsonUtility.FromJson<T>(json);
            return data ?? new T();
        }

        public string ReadRaw(string fileName)
        {
            string path = GetPath(fileName);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
        }

        public void Save<T>(string fileName, T data) where T : class, new()
        {
            SaveAtomic(fileName, data);
        }

        public void SaveAtomic<T>(string fileName, T data) where T : class, new()
        {
            Directory.CreateDirectory(_rootPath);
            string json = JsonUtility.ToJson(data ?? new T(), true);
            string destination = GetPath(fileName);
            string temporary = destination + ".tmp";
            string backup = destination + ".bak";
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (FileStream stream = new(
                temporary,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            try
            {
                if (File.Exists(destination))
                {
                    File.Replace(temporary, destination, backup);
                    Interlocked.Increment(ref _changeVersion);
                    if (File.Exists(backup)) File.Delete(backup);
                }
                else
                {
                    File.Move(temporary, destination);
                    Interlocked.Increment(ref _changeVersion);
                }
            }
            catch
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                throw;
            }
        }

        public void Delete(string fileName)
        {
            string path = GetPath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Interlocked.Increment(ref _changeVersion);
            }
        }

        public string GetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Save file name cannot be empty.", nameof(fileName));

            return Path.Combine(_rootPath, fileName);
        }
    }
}
