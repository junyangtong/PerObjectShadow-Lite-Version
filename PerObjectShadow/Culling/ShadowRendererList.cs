using System.Collections.Generic;
using FL.PerObjectShadow.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace FL.PerObjectShadow.PerObjectShadow
{
    public class ShadowRendererList
    {
        private readonly struct RendererEntry
        {
            public readonly Renderer Renderer;
            public readonly int DrawCallIndexStartInclusive;
            public readonly int DrawCallIndexEndExclusive;

            public RendererEntry(Renderer renderer, int drawCallIndexStartInclusive, int drawCallIndexEndExclusive)
            {
                Renderer = renderer;
                DrawCallIndexStartInclusive = drawCallIndexStartInclusive;
                DrawCallIndexEndExclusive = drawCallIndexEndExclusive;
            }
        }

        private readonly struct DrawCallData
        {
            public readonly Material Material;
            public readonly int SubmeshIndex;
            public readonly int ShaderPass;

            public DrawCallData(Material material, int submeshIndex, int shaderPass)
            {
                Material = material;
                SubmeshIndex = submeshIndex;
                ShaderPass = shaderPass;
            }
        }

        private readonly List<RendererEntry> m_Renderers = new();
        private readonly List<DrawCallData> m_DrawCalls = new();

        public bool TryGetWorldBounds(ShadowUsage usage, out Bounds worldBounds, ICollection<int> outAppendRendererIndices = null)
        {
            worldBounds = default;
            bool firstBounds = true;

            for (int i = 0; i < m_Renderers.Count; i++)
            {
                RendererEntry entry = m_Renderers[i];

                if (!IsEntryEnabled(in entry, usage))
                {
                    continue;
                }

                outAppendRendererIndices?.Add(i);

                if (firstBounds)
                {
                    worldBounds = entry.Renderer.bounds;
                    firstBounds = false;
                }
                else
                {
                    worldBounds.Encapsulate(entry.Renderer.bounds);
                }
            }

            return !firstBounds;
        }

        private static bool IsEntryEnabled(in RendererEntry entry, ShadowUsage usage)
        {
            if (entry.DrawCallIndexEndExclusive - entry.DrawCallIndexStartInclusive <= 0)
            {
                // 没有 draw call
                return false;
            }

#if UNITY_EDITOR
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(entry.Renderer.gameObject))
            {
                return false;
            }
#endif

            // 自阴影只打在自己身上，要是 renderer 不可见的话，没必要渲染
            if (usage == ShadowUsage.Self && !entry.Renderer.isVisible)
            {
                return false;
            }

            return entry.Renderer.enabled && entry.Renderer.gameObject.activeInHierarchy &&
                   entry.Renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        public void Draw(CommandBuffer cmd, int rendererIndex)
        {
            RendererEntry entry = m_Renderers[rendererIndex];
            for (int i = entry.DrawCallIndexStartInclusive; i < entry.DrawCallIndexEndExclusive; i++)
            {
                DrawCallData dc = m_DrawCalls[i];
                cmd.DrawRenderer(entry.Renderer, dc.Material, dc.SubmeshIndex, dc.ShaderPass);
            }
        }

        public void Clear()
        {
            m_Renderers.Clear();
            m_DrawCalls.Clear();
        }

        public void Add(Renderer renderer)
        {
            int drawCallInitialCount = m_DrawCalls.Count;
            List<Material> materialList = ListPool<Material>.Get();

            try
            {
                renderer.GetSharedMaterials(materialList);
                for (int i = 0; i < materialList.Count; i++)
                {
                    Material material = materialList[i];

                        if (TryGetShadowCasterPass(material, out int passIndex))
                        {
                            m_DrawCalls.Add(new DrawCallData(material, i, passIndex));
                        }
                    
                }

                if (m_DrawCalls.Count > drawCallInitialCount)
                {
                    m_Renderers.Add(new RendererEntry(renderer, drawCallInitialCount, m_DrawCalls.Count));
                }
            }
            catch
            {
                m_DrawCalls.RemoveRange(drawCallInitialCount, m_DrawCalls.Count - drawCallInitialCount);
                throw;
            }
            finally
            {
                ListPool<Material>.Release(materialList);
            }
        }
        
        private static bool TryGetShadowCasterPass(Material material, out int passIndex)
        {
            Shader shader = material.shader;
            
            for (int i = 0; i < shader.passCount; i++)
            {
                if (shader.FindPassTagValue(i, ShaderTagIds.LightMode) == ShaderTagIds.PerObjectShadowCaster)
                {
                    passIndex = i;
                    return true;
                }
            }

            passIndex = -1;
            return false;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        public readonly struct ReadOnly
        {
            private readonly ShadowRendererList m_List;

            internal ReadOnly(ShadowRendererList list)
            {
                m_List = list;
            }

            public bool TryGetWorldBounds(ShadowUsage usage, out Bounds worldBounds, ICollection<int> outAppendRendererIndices = null)
            {
                return m_List.TryGetWorldBounds(usage, out worldBounds, outAppendRendererIndices);
            }

            public void Draw(CommandBuffer cmd, int rendererIndex)
            {
                m_List.Draw(cmd, rendererIndex);
            }
        }

        private static class ShaderTagIds
        {
            public static readonly ShaderTagId LightMode = MemberNameHelpers.ShaderTagId();
            public static readonly ShaderTagId PerObjectShadowCaster = MemberNameHelpers.ShaderTagId();
        }
    }
}
