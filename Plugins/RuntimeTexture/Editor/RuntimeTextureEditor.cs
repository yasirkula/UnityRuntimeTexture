using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeTextureNamespace
{
    [CustomEditor(typeof(RuntimeTexture))]
    [CanEditMultipleObjects]
    public class RuntimeTextureEditor : Editor
    {
        private new RuntimeTexture[] targets;
        private Texture2D[] textures;
        private string fileSizeInfoString;

        protected void OnEnable()
        {
            targets = Array.ConvertAll(base.targets, (e) => (RuntimeTexture)e);
            textures = new Texture2D[targets.Length];

            ReloadPreviewInstances();
        }

        protected void OnDisable()
        {
            if (textures != null)
            {
                for (int i = 0; i < textures.Length; i++)
                    DestroyImmediate(textures[i]);
            }
        }

        public override void ReloadPreviewInstances()
        {
            base.ReloadPreviewInstances();

            long totalSize = 0L;
            for (int i = 0; i < targets.Length; i++)
            {
                if (textures[i] != null)
                    DestroyImmediate(textures[i]);

                textures[i] = targets[i].CreateTexture(true);
                totalSize += targets[i].bytes.LongLength;
            }

            if (targets.Length == 1)
                fileSizeInfoString = $"Size: {EditorUtility.FormatBytes(totalSize)}";
            else
                fileSizeInfoString = $"Size: {EditorUtility.FormatBytes(totalSize)} ({targets.Length} objects)";
        }

        public override bool HasPreviewGUI()
        {
            return textures != null && textures.Length > 0;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            int index = Array.IndexOf(targets, target);
            if (index < 0)
                return;

            // Preserve texture aspect ratio
            Texture2D texture = textures[index];
            float aspectRatio = (float)texture.width / texture.height;
            float width = aspectRatio * r.height;
            if (width <= r.width)
            {
                r.x += (r.width - width) * 0.5f;
                r.width = width;
            }
            else
            {
                float height = r.width / aspectRatio;
                r.y += (r.height - height) * 0.5f;
                r.height = height;
            }

            EditorGUI.DrawTextureTransparent(r, textures[index]);
        }

        // This is used by Unity to generate the icon of the RuntimeTexture in Inspector's header
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            Texture2D result = targets[0].CreateTexture(true);
            RuntimeTextureImporter.ResizeTexture(ref result, result.format, new Vector2Int(width, height), Vector2Int.zero, Color.clear);
            return result;
        }

        public override string GetInfoString()
        {
            return fileSizeInfoString;
        }
    }
}
