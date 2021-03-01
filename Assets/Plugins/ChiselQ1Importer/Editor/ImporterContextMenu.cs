// MIT License
// Based off Henry's Source importer https://github.com/Henry00IS/Chisel.Import.Source
#if UNITY_EDITOR

using Chisel.Components;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Quixotic7.Chisel.Import.Q1.Editor
{
    public class ImporterContextMenu
    {
        [MenuItem("GameObject/Chisel/Import/Quake 1 Map...")]
        private static void ImportQ1Map()
        {
            GameObject go = null;
            try
            {

                string path = EditorUtility.OpenFilePanel("Import Quake 1 Map", "", "map");
                if (path.Length != 0)
                {

                    EditorUtility.DisplayProgressBar("RealtimeCSG: Importing Quake 1 Map", "Parsing Quake 1 Map File (*.map)...", 0.0f);
                    var importer = new MapImporter();
                    //importer.adjustTexturesForValve = mapImporter.adjustTexturesForValve;

                    var map = importer.Import(path);

                    // create parent game object to store all of the imported content.
                    go = new GameObject("Q1-" + Path.GetFileNameWithoutExtension(path));
                    go.SetActive(false);

                    // create chisel model and import all of the brushes.
                    EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Preparing Material Searcher...", 0.0f);
                    Q1MapWorldConverter.Import(go.transform, map);

                    // begin converting hammer entities to unity objects.
                    //Q1EntityConverter.Import(go.transform, map);
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Quake 1 Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (go != null)
                {
                    go.SetActive(true);
                }
            }
        }
    }
}

#endif