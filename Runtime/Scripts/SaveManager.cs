using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Bodardr.Saving
{
    public static class SaveManager
    {
        public static SaveFile CurrentSave;

        internal static List<Type> saveables;

        public static void Init()
        {
            saveables = new List<Type>();
            var assembly = Assembly.GetCallingAssembly();

            foreach (var t in assembly.GetTypes())
            {
                if (t.GetCustomAttributes(typeof(SaveableAttribute), true).Length > 0)
                {
                    Debug.Log($"Added {t}");
                    saveables.Add(t);
                }
            }
        }

        public static void NewSaveFile(string fileName = "")
        {
            if (CurrentSave != null)
                Debug.LogWarning("A save has already been loaded, make sure you've dumped the save file first.");

            CurrentSave = new SaveFile(fileName);

            foreach (var saveable in saveables)
            {
                var instance = Activator.CreateInstance(saveable);
                CurrentSave.SavedEntries.Add(saveable, instance);
            }
        }

        public static void SaveAll(string saveAs = "")
        {
            if (!string.IsNullOrEmpty(saveAs))
                CurrentSave.Filename = saveAs;

            StringBuilder str = new StringBuilder();
            
            CurrentSave.LastSaveTime = DateTime.Now;

            //Append dates
            str.Append($"\\{CurrentSave.CreationTime.Ticks}");
            str.AppendLine($"\\{CurrentSave.LastSaveTime.Ticks}");

            foreach (var entry in CurrentSave.SavedEntries)
            {
                str.AppendLine($"\\{entry.Key}\r");
                str.AppendLine(JsonUtility.ToJson(entry.Value));
            }

            var filePath = Path.Combine(Application.persistentDataPath, CurrentSave.Filename);

            Debug.Log(filePath);
            
            FileStream saveFile;
            saveFile = File.Exists(filePath) ? File.OpenWrite(filePath) : File.Create(filePath);

            var bytes = Encoding.Default.GetBytes(str.ToString());

            saveFile.Write(bytes, 0, bytes.Length);
        }

        public static bool Load(string filename = "")
        {
            if (string.IsNullOrEmpty(filename))
                filename = SaveFile.defaultFilename;

            try
            {
                var file = File.ReadAllText(Path.Combine(Application.persistentDataPath,filename)).Split(new []{'\\'}, StringSplitOptions.RemoveEmptyEntries);
                CurrentSave = new SaveFile(filename)
                {
                    Filename = filename,
                    CreationTime = new DateTime(long.Parse(file[0])),
                    LastSaveTime = new DateTime(long.Parse(file[1]))
                };

                for (int i = 2; i < file.GetLength(0); i++)
                {
                    var obj = file[i].Split(new [] {'\r'}, StringSplitOptions.RemoveEmptyEntries);
                    
                    var type = saveables.Find(x => x.Name == obj[0]);
                    CurrentSave.SavedEntries.Add(type ?? typeof(object), JsonUtility.FromJson(obj[1], type));
                }
                
                //todo : add new saveable instances to update save file...
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public static bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}