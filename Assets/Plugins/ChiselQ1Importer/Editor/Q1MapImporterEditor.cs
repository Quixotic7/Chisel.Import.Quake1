// MIT License
// Based off Henry's Source importer https://github.com/Henry00IS/Chisel.Import.Source
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Quixotic7.Chisel.Import.Q1
{
    // Declare type of Custom Editor
    [CustomEditor(typeof(Q1MapImporter))] //1
    class Q1MapImporterEditor : UnityEditor.Editor
    {
        // OnInspector GUI
        public override void OnInspectorGUI() //2
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Import Map"))
            {
                ImportMap();
            }
        }

        private void ImportMap()
        {
            var mapImporter = target as Q1MapImporter;

            // Not currently in try catch so I can see Unity errors. 

            GameObject go = null;

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

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
                    // rebuild the world as we need the collision mesh for decals.
                    EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Rebuilding the world...", 0.5f);
                    go.SetActive(true);
                    ChiselNodeHierarchyManager.Rebuild();
#endif
                // begin converting hammer entities to unity objects.
                //Q1EntityConverter.Import(go.transform, map);
            }

            EditorUtility.ClearProgressBar();
            if (go != null)
            {
                go.transform.SetParent(mapImporter.transform);
                go.SetActive(true);
            }
        }
    }
}