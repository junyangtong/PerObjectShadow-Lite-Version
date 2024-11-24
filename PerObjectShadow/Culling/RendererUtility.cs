#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace FL.PerObjectShadow.Utils
{
    public static class RendererUtility
    {
        public static void SetMaterialProperties(Renderer renderer, Lazy<MaterialPropertyBlock> propertyBlock, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            // SRPBatcher 不支持 MaterialPropertyBlock
            // 但是在 Editor 里不用 MaterialPropertyBlock 的话不好搞
            // 所以 Editor 里用 MaterialPropertyBlock，Build 之后用 Material

            SetPropertiesViaPropertyBlock(renderer, propertyBlock, floats, vectors);
            SetPropertiesViaMaterial(renderer, floats, vectors);
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetPropertiesViaPropertyBlock(Renderer renderer, Lazy<MaterialPropertyBlock> propertyBlock, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            MaterialPropertyBlock properties = propertyBlock.Value;
            renderer.GetPropertyBlock(properties);

            for (int i = 0; i < floats.Count; i++)
            {
                properties.SetFloat(floats[i].Item1, floats[i].Item2);
            }

            for (int i = 0; i < vectors.Count; i++)
            {
                properties.SetVector(vectors[i].Item1, vectors[i].Item2);
            }

            renderer.SetPropertyBlock(properties);
        }

        [Conditional("NOT_UNITY_EDITOR")]
        private static void SetPropertiesViaMaterial(Renderer renderer, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            List<Material> materials = ListPool<Material>.Get();

            try
            {
                renderer.GetMaterials(materials);

                foreach (var material in materials)
                {
                    for (int i = 0; i < floats.Count; i++)
                    {
                        material.SetFloat(floats[i].Item1, floats[i].Item2);
                    }

                    for (int i = 0; i < vectors.Count; i++)
                    {
                        material.SetVector(vectors[i].Item1, vectors[i].Item2);
                    }
                }
            }
            finally
            {
                ListPool<Material>.Release(materials);
            }
        }
    }
}
