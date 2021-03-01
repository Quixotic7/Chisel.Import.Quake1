// MIT License
// Based off Henry's Source importer https://github.com/Henry00IS/Chisel.Import.Source

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Chisel.Components;
using Chisel.Core;

namespace Quixotic7.Chisel.Import.Q1
{
    /// <summary>
    /// Converts a Quake 1 Map to Chisel Brushes.
    /// </summary>
    public static class Q1MapWorldConverter
    {
        private static int _Scale = 32;

        private static float _conversionScale = 1.0f / 32;

        private static Transform CreateGameObjectWithUniqueName(string name, Transform parent)
        {
            var go = new GameObject(UnityEditor.GameObjectUtility.GetUniqueNameForSibling(parent, name));
            go.transform.SetParent(parent);
            return go.transform;
        }

        private class EntityContainer
        {
            public Transform transform;
            public MapEntity entity;

            public EntityContainer(Transform t, MapEntity e)
            {
                transform = t;
                entity = e;
            }
        }

        private static float SafeDivision(float numerator, float denominator)
        {
            return (Mathf.Approximately(denominator, 0)) ? 0 : numerator / denominator;
        }

        private static Vector3 SafeDivision(Vector3 numerator, float denominator)
        {
            return (Mathf.Approximately(denominator, 0)) ? Vector3.zero : numerator / denominator;
        }

        public static Matrix4x4 GetTextMatrix(Vector3 tX, Vector3 tY, Vector2 offset, Vector2 scale)
        {
            //var x = SafeDivision(tX, scale.x);
            //var y = SafeDivision(tY, scale.y);

            var x = tX * scale.x;
            var y = tY * scale.y;
            var z = Vector3.Cross(tX, tY);

            return new Matrix4x4(
                new Vector4(x.x, x.y, x.z, offset.x),
                new Vector4(y.x, y.y, y.z, offset.y),
                new Vector4(z.x, z.y, z.z, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        /// <summary>
        /// Imports the specified world into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(Transform rootTransform, MapWorld world)
        {
            _conversionScale = 1.0f / _Scale;

            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();

            var mapTransform = CreateGameObjectWithUniqueName(world.mapName, rootTransform);
            mapTransform.position = Vector3.zero;

            // Index of entities by trenchbroom id
            var entitiesById = new Dictionary<int, EntityContainer>();

            var layers = new List<EntityContainer>();

            for (int e = 0; e < world.Entities.Count; e++)
            {
                var entity = world.Entities[e];

                //EntityContainer eContainer = null;

                if(entity.tbId >= 0)
                {
                    var name = String.IsNullOrEmpty(entity.tbName) ? "Unnamed" : entity.tbName;
                    var t = CreateGameObjectWithUniqueName(name, mapTransform);
                    var eContainer = new EntityContainer(t, entity);
                    entitiesById.Add(entity.tbId, eContainer);

                    if(entity.tbType == "_tb_layer")
                    {
                        layers.Add(eContainer);
                        eContainer.transform.SetParent(null); // unparent until layers are sorted by sort index
                    }
                }
            }

            var defaultLayer = CreateGameObjectWithUniqueName("Default Layer", mapTransform);

            layers = layers.OrderBy(l => l.entity.tbLayerSortIndex).ToList(); // sort layers by layer sort index

            foreach(var l in layers)
            {
                l.transform.SetParent(mapTransform); // parent layers to map in order
            }

            bool valveFormat = world.valveFormat;

            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Importing Quake 1 Map", "Converting Quake 1 Entities To Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                MapEntity entity = world.Entities[e];

                Transform brushParent = mapTransform;

                bool isLayer = false;
                bool isTrigger = false;

                if(entity.ClassName == "worldspawn")
                {
                    brushParent = defaultLayer;
                }
                else if (entity.tbType == "_tb_layer")
                {
                    isLayer = true;
                    if (entitiesById.TryGetValue(entity.tbId, out EntityContainer eContainer))
                    {
                        brushParent = eContainer.transform;
                    }
                }
                else if(entity.tbType == "_tb_group")
                {
                    if (entitiesById.TryGetValue(entity.tbId, out EntityContainer eContainer))
                    {
                        brushParent = eContainer.transform;
                    }
                }
                else
                {
                    if (entity.ClassName.Contains("trigger")) isTrigger = true;

                    brushParent = CreateGameObjectWithUniqueName(entity.ClassName, mapTransform);
                }

                if (brushParent != mapTransform && brushParent != defaultLayer)
                {
                    if (entity.tbGroup > 0)
                    {
                        if (entitiesById.TryGetValue(entity.tbGroup, out EntityContainer eContainer))
                        {
                            brushParent.SetParent(eContainer.transform);
                        }
                    }
                    else if (entity.tbLayer > 0)
                    {
                        if (entitiesById.TryGetValue(entity.tbLayer, out EntityContainer eContainer))
                        {
                            brushParent.SetParent(eContainer.transform);
                        }
                    }
                    else if(!isLayer)
                    {
                        brushParent.SetParent(defaultLayer);
                    }
                }

                //if(entity.)

                if (entity.Brushes.Count == 0) continue;

                var model = ChiselModelManager.CreateNewModel(brushParent);
                // var model = OperationsUtility.CreateModelInstanceInScene(brushParent);
                var parent = model.transform;

                if (isTrigger)
                {
                    //model.Settings = (model.Settings | ModelSettingsFlags.IsTrigger | ModelSettingsFlags.SetColliderConvex | ModelSettingsFlags.DoNotRender);
                }

                // iterate through all entity brushes.
                for (int i = 0; i < entity.Brushes.Count; i++)
                {
                    MapBrush brush = entity.Brushes[i];


                    // build a very large cube brush.
                    ChiselBrush go = ChiselComponentFactory.Create<ChiselBrush>(model);
                    go.definition.surfaceDefinition = new ChiselSurfaceDefinition();
                    go.definition.surfaceDefinition.EnsureSize(6);
                    BrushMesh brushMesh = new BrushMesh();
                    go.definition.brushOutline = brushMesh;
                    BrushMeshFactory.CreateBox(ref brushMesh, new Vector3(-4096, -4096, -4096), new Vector3(4096, 4096, 4096), in go.definition.surfaceDefinition);

                    // prepare for uv calculations of clip planes after cutting.
                    var planes = new float4[brush.Sides.Count];
                    var planeSurfaces = new ChiselSurface[brush.Sides.Count];

                    // compute all the sides of the brush that will be clipped.
                    for (int j = brush.Sides.Count; j-- > 0;)
                    {
                        MapBrushSide side = brush.Sides[j];

                        // detect excluded polygons.
                        //if (IsExcludedMaterial(side.Material))
                        //polygon.UserExcludeFromFinal = true;
                        // detect collision-only brushes.
                        //if (IsInvisibleMaterial(side.Material))
                        //pr.IsVisible = false;

                        // find the material in the unity project automatically.
                        Material material;

                        string materialName = side.Material.Replace("*", "#");
                        material = materialSearcher.FindMaterial(new string[] { materialName });
                        if (material == null)
                        {
                            material = ChiselMaterialManager.DefaultFloorMaterial;
                        }

                        // create chisel surface for the clip.
                        ChiselSurface surface = new ChiselSurface();
                        surface.brushMaterial = ChiselBrushMaterial.CreateInstance(material, ChiselMaterialManager.DefaultPhysicsMaterial);
                        surface.surfaceDescription = SurfaceDescription.Default;

                        // detect collision-only polygons.
                        if (IsInvisibleMaterial(side.Material))
                        {
                            surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.RenderReceiveCastShadows;
                        }
                        // detect excluded polygons.
                        if (IsExcludedMaterial(side.Material))
                        {
                            surface.brushMaterial.LayerUsage &= LayerUsageFlags.CastShadows;
                            surface.brushMaterial.LayerUsage |= LayerUsageFlags.Collidable;
                        }

                        // calculate the clipping planes.
                        Plane clip = new Plane(go.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) * _conversionScale), go.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) * _conversionScale), go.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) * _conversionScale));
                        planes[j] = new float4(clip.normal, clip.distance);
                        planeSurfaces[j] = surface;
                    }

                    // cut all the clipping planes out of the brush in one go.
                    brushMesh.Cut(planes, planeSurfaces);

                    // now iterate over the planes to calculate UV coordinates.
                    int[] indices = new int[brush.Sides.Count];
                    for (int k = 0; k < planes.Length; k++)
                    {
                        var plane = planes[k];
                        int closestIndex = 0;
                        float closestDistance = math.lengthsq(plane - brushMesh.planes[0]);
                        for (int j = 1; j < brushMesh.planes.Length; j++)
                        {
                            float testDistance = math.lengthsq(plane - brushMesh.planes[j]);
                            if (testDistance < closestDistance)
                            {
                                closestIndex = j;
                                closestDistance = testDistance;
                            }
                        }
                        indices[k] = closestIndex;
                    }

                    for (int j = 0; j < indices.Length; j++)
                        brushMesh.planes[indices[j]] = planes[j];

                    for (int j = brush.Sides.Count; j-- > 0;)
                    {
                        MapBrushSide side = brush.Sides[j];

                        var surface = brushMesh.polygons[indices[j]].surface;
                        var material = surface.brushMaterial.RenderMaterial;

                        // calculate the texture coordinates.
                        int w = 256;
                        int h = 256;
                        if (material.mainTexture != null)
                        {
                            w = material.mainTexture.width;
                            h = material.mainTexture.height;
                        }
                        var clip = new Plane(planes[j].xyz, planes[j].w);

                        if (world.valveFormat)
                        {
                            var uAxis = new VmfAxis(side.t1, side.Offset.X, side.Scale.X);
                            var vAxis = new VmfAxis(side.t2, side.Offset.Y, side.Scale.Y);
                            CalculateTextureCoordinates(go, surface, clip, w, h, uAxis, vAxis);
                        }
                        else
                        {
                            if (GetTextureAxises(clip, out MapVector3 t1, out MapVector3 t2))
                            {
                                var uAxis = new VmfAxis(t1, side.Offset.X, side.Scale.X);
                                var vAxis = new VmfAxis(t2, side.Offset.Y, side.Scale.Y);
                                CalculateTextureCoordinates(go, surface, clip, w, h, uAxis, vAxis);
                            }
                        }
                    }

                    try
                    {
                        // finalize the brush by snapping planes and centering the pivot point.
                        go.transform.position += brushMesh.CenterAndSnapPlanes();
                    }
                    catch
                    {
                        // Brush failed, destroy brush
                        GameObject.DestroyImmediate(go);
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            if (name.StartsWith("sky"))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is an invisible material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an invisible material; otherwise, <c>false</c>.</returns>
        private static bool IsInvisibleMaterial(string name)
        {
            switch (name)
            {
                case "clip":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is a special material, these brush will not be
        /// imported into SabreCSG.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is a special material; otherwise, <c>false</c>.</returns>
        private static bool IsSpecialMaterial(string name)
        {
            switch (name)
            {
                case "trigger":
                case "skip":
                case "waterskip":
                    return true;
            }
            return false;
        }

        private static void CalculateTextureCoordinates(ChiselBrush pr, ChiselSurface surface, Plane clip, int textureWidth, int textureHeight, VmfAxis UAxis, VmfAxis VAxis)
        {
            var localToPlaneSpace = (Matrix4x4)MathExtensions.GenerateLocalToPlaneSpaceMatrix(new float4(clip.normal, clip.distance));
            var planeSpaceToLocal = (Matrix4x4)math.inverse(localToPlaneSpace);

            UAxis.Translation %= textureWidth;
            VAxis.Translation %= textureHeight;

            if (UAxis.Translation < -textureWidth / 2f)
                UAxis.Translation += textureWidth;

            if (VAxis.Translation < -textureHeight / 2f)
                VAxis.Translation += textureHeight;

            var scaleX = textureWidth * UAxis.Scale * _conversionScale;
            var scaleY = textureHeight * VAxis.Scale * _conversionScale;

            var uoffset = Vector3.Dot(Vector3.zero, new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y)) + (UAxis.Translation / textureWidth);
            var voffset = Vector3.Dot(Vector3.zero, new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y)) + (VAxis.Translation / textureHeight);

            var uVector = new Vector4(UAxis.Vector.X / scaleX, UAxis.Vector.Z / scaleX, UAxis.Vector.Y / scaleX, uoffset);
            var vVector = new Vector4(VAxis.Vector.X / scaleY, VAxis.Vector.Z / scaleY, VAxis.Vector.Y / scaleY, voffset);
            var uvMatrix = new UVMatrix(uVector, -vVector);
            var matrix = uvMatrix.ToMatrix();

            matrix = matrix * planeSpaceToLocal;

            surface.surfaceDescription.UV0 = new UVMatrix(matrix);
        }

        // For Quake 1 Standard format
        private static bool GetTextureAxises(Plane plane, out MapVector3 t1, out MapVector3 t2)
        {
            // feel free to improve this uv mapping code, it has some issues.
            // • 45 degree angled walls may not have correct UV texture coordinates (are not correctly picking the dominant axis because there are two).
            // • negative vertex coordinates may not have correct UV texture coordinates.

            int dominantAxis = 0; // 0 == x, 1 == y, 2 == z

            // find the axis closest to the polygon's normal.
            float[] axes =
            {
                    Mathf.Abs(plane.normal.x),
                    Mathf.Abs(plane.normal.z),
                    Mathf.Abs(plane.normal.y)
                };

            // defaults to use x-axis.
            dominantAxis = 0;
            // check whether the y-axis is more likely.
            if (axes[1] > axes[dominantAxis])
                dominantAxis = 1;
            // check whether the z-axis is more likely.
            if (axes[2] >= axes[dominantAxis])
                dominantAxis = 2;

            // x-axis:
            if (dominantAxis == 0)
            {
                t1 = new MapVector3(0, 0, 1);
                t2 = new MapVector3(0, 1, 0);
                return true;
            }

            // y-axis:
            if (dominantAxis == 1)
            {
                t1 = new MapVector3(0, 0, 1);
                t2 = new MapVector3(1, 0, 0);
                return true;
            }

            // z-axis:
            if (dominantAxis == 2)
            {
                t1 = new MapVector3(1, 0, 0);
                t2 = new MapVector3(0, 1, 0);
                return true;
            }

            t1 = null;
            t2 = null;
            return false;
        }
    }
}