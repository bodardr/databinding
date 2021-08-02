using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Saving
{
    public class SaveFile : IEnumerable
    {
        internal const string defaultFilename = "save.dat";

        private DateTime creationTime;

        private DateTime lastSaveTime;

        private Dictionary<Type, object> savedEntries;

        public Dictionary<Type, object> SavedEntries => savedEntries;

        public DateTime CreationTime
        {
            get => creationTime;
            internal set => creationTime = value;
        }

        public DateTime LastSaveTime
        {
            get => lastSaveTime;
            internal set => lastSaveTime = value;
        }

        public TimeSpan TimePlayed => LastSaveTime - CreationTime;

        public string Filename { get; set; }

        public SaveFile(string fileName = "")
        {
            savedEntries = new Dictionary<Type, object>();
            
            if (string.IsNullOrEmpty(fileName))
                Filename = defaultFilename;
            else
            {
                //Remove entries with extensions already in them.
                fileName = fileName.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)[0];

                Filename = string.Concat(fileName, ".dat");
            }

            creationTime = lastSaveTime = DateTime.Now;
        }

        public T GetFile<T>()
        {
            var type = typeof(T);

            if (SaveManager.saveables.Find(x => x == type) == null)
            {
                Debug.LogError("Type does not contain the Saveable attribute.");
                return default;
            }

            if (SavedEntries[type] != null)
                return (T) SavedEntries[type];

            Debug.LogError("Type was not found inside the savefile.");
            return default;
        }

        public IEnumerator GetEnumerator()
        {
            return SavedEntries.GetEnumerator();
        }
    }
}