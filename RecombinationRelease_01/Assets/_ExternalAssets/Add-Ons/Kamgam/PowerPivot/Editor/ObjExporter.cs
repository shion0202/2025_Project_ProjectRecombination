using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Kamgam.PowerPivot
{
    public static class ObjExporter
    {
#if UNITY_EDITOR

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">File path relative to the Assets directory. You don not have to include "Assets/". The file extension is irrelevant (it will be stripped anyways).</param>
        /// <param name="objectName"></param>
        /// <param name="mesh"></param>
        /// <param name="materials"></param>
        /// <param name="logFilePaths"></param>
        /// <returns>A dictionary containing the file name (with extension) and the content as string.</returns>
        public static Dictionary<string, string> SaveMeshAsObj(string filePath, string objectName, Mesh mesh, IList<Material> materials, bool logFilePaths)
        {
            // Remove "Assets/" from the start
            if (filePath.StartsWith("Assets/") || filePath.StartsWith("Assets\\"))
            {
                filePath = filePath.Substring(7);
            }

            // Create dir if necessary
            string dirPath = System.IO.Path.GetDirectoryName(Application.dataPath + "/" + filePath);
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var files = meshToObj(fileName, objectName, mesh, materials);
            string subdir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(subdir))
            {
                subdir += "/";
            }

            foreach (var kv in files)
            {
                string name = kv.Key;
                string content = kv.Value;
                string path = Application.dataPath + "/" + subdir + name;
                System.IO.File.WriteAllText(path, content);
                if(logFilePaths)
                    Logger.LogMessage("File generated in <color=yellow>Assets/" + (subdir + name) + "</color>");
            }

            AssetDatabase.Refresh();

            return files;
        }

        /// <summary>
        /// Converts a mesh into an OBJ formated string.
        /// OBJ Format Infos: https://en.wikipedia.org/wiki/Wavefront_.obj_file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="objectName"></param>
        /// <param name="mesh"></param>
        /// <param name="materials"></param>
        /// <returns>A dictionary of filenames (key) and file contents (value). Usually that's "fileName.obj" and "fileName.mtl".</returns>
        static Dictionary<string,string> meshToObj(string fileName, string objectName, Mesh mesh, IList<Material> materials)
		{
            var files = new Dictionary<string, string>();

			System.Text.StringBuilder meshString = new System.Text.StringBuilder();
            meshString.Append("# OBJ created via Mesh Extractor by KAMGAM").Append("\n");

            fileName = sanitizeString(fileName);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "Mesh";
            }


            // Material Lib
            // If not materials are given then default material values are used to create the material lib.
            
            // Ref to material lib file in .obj
            meshString.Append("mtllib ").Append(fileName).Append(".mtl").Append("\n");

            // Material lib file
            System.Text.StringBuilder materialsString = new System.Text.StringBuilder();
            materialsString.Append("# MTL created via Mesh Extractor by KAMGAM").Append("\n");

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Material material = null;
                string materialName = null;
                if (materials != null && materials.Count > i)
                {
                    material = materials[i];
                    materialName = sanitizeString(material.name);
                }

                if (string.IsNullOrEmpty(materialName))
                {
                    materialName = "Material" + i;
                }

                materialsString.Append("newmtl ").Append(materialName).Append("\n");
                // Specular Exponent (ranges between 0 and 1000)
                materialsString.Append("Ns ").Append(string.Format(CultureInfo.InvariantCulture, "{0:F6}", 225f)).Append("\n");
                // Ambient Color
                materialsString.Append("Ka ").Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", 1f, 1f, 1f)).Append("\n");
                // Diffuse Color
                materialsString.Append("Kd ").Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}",
                    material == null || !material.HasProperty("_Color") ? 1f : material.color.r,
                    material == null || !material.HasProperty("_Color") ? 1f : material.color.g,
                    material == null || !material.HasProperty("_Color") ? 1f : material.color.b)).Append("\n");
                // Specular
                materialsString.Append("Ks ").Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", 0.5f, 0.5f, 0.5f)).Append("\n");
                // Emissive
                materialsString.Append("Ke ").Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", 0f, 0f, 0f)).Append("\n");
                // Optical desity (aka refraction index), glass ~= 1.45
                materialsString.Append("Ni ").Append(string.Format(CultureInfo.InvariantCulture, "{0}", 1.45f)).Append("\n");
                // Opacity (0 = fully transparent, 1 = fully opaque)
                materialsString.Append("d ").Append(string.Format(CultureInfo.InvariantCulture, "{0}", 1f)).Append("\n");
                // Illumination mode (can be skipped)
                //   0 = Color on and Ambient off
                //   1 = Color on and Ambient on
                //   2 = Highlight on
                //   3 = Reflection on and Ray trace on
                //   4 = Transparency: Glass on, Reflection: Ray trace on
                //   5 = Reflection: Fresnel on and Ray trace on
                //   6 = Transparency: Refraction on, Reflection: Fresnel off and Ray trace on
                //   7 = Transparency: Refraction on, Reflection: Fresnel on and Ray trace on
                //   8 = Reflection on and Ray trace off
                //   9 = Transparency: Glass on, Reflection: Ray trace off
                //  10 = Casts shadows onto invisible surfaces
                materialsString.Append("illum 2").Append("\n");

                // Textures
                if (material != null)
                {
                    // Diffuse Texture
                    if (material.mainTexture != null)
                    {
                        var materialPath = AssetDatabase.GetAssetPath(material);
                        var texturePath = AssetDatabase.GetAssetPath(material.mainTexture);
                        var relativeTexturePath = texturePath.Replace(System.IO.Path.GetDirectoryName(materialPath), "");
                        if (!string.IsNullOrEmpty(relativeTexturePath))
                            materialsString.Append("map_Kd ").Append(relativeTexturePath).Append("\n");
                    }
                }
                materialsString.Append("\n");
            }

            files.Add(fileName + ".mtl", materialsString.ToString());


            // Object
            string objName = sanitizeString(objectName);
            if (string.IsNullOrEmpty(objName))
            {
                objName = "Object";
            }
            meshString.Append("o ").Append(objName).Append("\n");

            // Vertices
			foreach (Vector3 v in mesh.vertices)
			{
                // flip x-axis to convert from Unitys left-handed coordinates to OBJ right-handed.
				meshString.Append(string.Format(CultureInfo.InvariantCulture, "v {0:F6} {1:F6} {2:F6}\n", -v.x, v.y, v.z));
			}

            // UVs
            foreach (Vector3 v in mesh.uv)
            {
                meshString.Append(string.Format(CultureInfo.InvariantCulture, "vt {0:F6} {1:F6}\n", v.x, v.y));
            }

            // Normals
            foreach (Vector3 v in mesh.normals)
			{
                // flip x-axis to convert from Unitys left-handed coordinates to OBJ right-handed.
                meshString.Append(string.Format(CultureInfo.InvariantCulture, "vn {0:F6} {1:F6} {2:F6}\n", -v.x, v.y, v.z));
			}

            // Faces (and sub meshes / groups)
			for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
			{
                string materialName = null;
                if (materials != null && materials.Count > subMeshIndex)
                {
                    materialName = sanitizeString(materials[subMeshIndex].name);
                }
                if (string.IsNullOrEmpty(materialName))
                {
                    materialName = "Material" + subMeshIndex;
                }

                meshString.Append("\n");
                if (subMeshIndex == 0)
                {
                    meshString.Append("g ").Append(fileName).Append("\n");
                }
                meshString.Append("usemtl ").Append(materialName).Append("\n");
                if(subMeshIndex == 0)
                {
                    // Smooth shading between sub meshes ON
                    meshString.Append("s 1\n");
                    // Smooth shading between sub meshes OFF
                    // meshString.Append("s off\n");
                }


				int[] triangles = mesh.GetTriangles(subMeshIndex);
				for (int i = 0; i < triangles.Length; i += 3)
				{
                    // flip order to convert from Unitys left-handed (0,1,2) coordinates to OBJ right-handed (2,1,0).
                    meshString.Append(string.Format("f {2}/{2}/{2} {1}/{1}/{1} {0}/{0}/{0}\n",
						triangles[i] + 1,
                        triangles[i + 1] + 1,
                        triangles[i + 2] + 1));
				}
			}

            // Mesh file (.obj)
            files.Add(fileName + ".obj", meshString.ToString());

            return files;
        }

        static string sanitizeString(string name)
        {
            var regexp = new Regex(@"[^-_.0-9A-Z]+", RegexOptions.IgnoreCase);
            return regexp.Replace(name, "");
        }
#endif

    }
}

