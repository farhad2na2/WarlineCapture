using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class JsonSaveRepository
    {
        private readonly string _rootPath;

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
            Directory.CreateDirectory(_rootPath);
            string json = JsonUtility.ToJson(data ?? new T(), true);
            File.WriteAllText(GetPath(fileName), json, Encoding.UTF8);
        }

        public void Delete(string fileName)
        {
            string path = GetPath(fileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        public string GetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Save file name cannot be empty.", nameof(fileName));

            return Path.Combine(_rootPath, fileName);
        }
    }
}
