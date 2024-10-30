using System;
using System.Collections.Generic;
using FL.PerObjectShadow.PerObjectShadow;
using FL.PerObjectShadow.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace FL.PerObjectShadow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("PerObjShadowRenderingController")]
    public sealed class PerObjShadowRenderingController : MonoBehaviour, IShadowCaster
    {
        private bool m_IsCastingShadow = true;

        [NonSerialized] private int m_ShadowCasterId = -1;
        [NonSerialized] private readonly List<Renderer> m_Renderers = new();
        [NonSerialized] private readonly ShadowRendererList m_ShadowRendererList = new();
        [NonSerialized] private readonly Lazy<MaterialPropertyBlock> m_PropertyBlock = new();

        public bool IsCastingShadow
        {
            get => m_IsCastingShadow;
            set => m_IsCastingShadow = value;
        }

        int IShadowCaster.Id
        {
            get => m_ShadowCasterId;
            set => m_ShadowCasterId = value;
        }

        ShadowRendererList.ReadOnly IShadowCaster.RendererList => m_ShadowRendererList.AsReadOnly();

        Transform IShadowCaster.Transform => transform;

        bool IShadowCaster.CanCastShadow(ShadowUsage usage)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            return IsCastingShadow;
        }

        private void OnEnable()
        {
            UpdateRendererList(fullUpdate: true);
            ShadowCasterManager.Register(this);
        }

        private void OnDisable()
        {
            ShadowCasterManager.Unregister(this);

            m_Renderers.Clear();
            m_ShadowRendererList.Clear();

            if (m_PropertyBlock.IsValueCreated)
            {
                m_PropertyBlock.Value.Clear();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Editor 中 Shader 可以任意修改，所以每次都要更新
            UpdateRendererList(fullUpdate: !Application.isPlaying);
#else
            UpdateMaterialProperties();
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_ShadowRendererList.TryGetWorldBounds(ShadowUsage.Scene, out Bounds bounds))
            {
                return;
            }

            Color color = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = color;
        }

        public void UpdateRendererList()
        {
            UpdateRendererList(true);
        }

        private void UpdateRendererList(bool fullUpdate)
        {
            if (fullUpdate)
            {
                m_Renderers.Clear();
                GetComponentsInChildren(true, m_Renderers);
            }

            // 因为 Build 之后需要实例化 Material，所以必须要先设置 MaterialProperties
            // 这样后面访问 SharedMaterial 时才能拿到正确的材质
            UpdateMaterialProperties();
            UpdateShadowRendererList();
        }

        private void UpdateMaterialProperties()
        {
            List<(int, float)> floats = ListPool<(int, float)>.Get();
            List<(int, Vector4)> vectors = ListPool<(int, Vector4)>.Get();

            try
            {
                floats.Add((PropertyIds._PerObjShadowCasterId, m_ShadowCasterId));

                foreach (Renderer r in m_Renderers)
                {
                    RendererUtility.SetMaterialProperties(r, m_PropertyBlock, floats, vectors);
                }
            }
            finally
            {
                ListPool<(int, float)>.Release(floats);
                ListPool<(int, Vector4)>.Release(vectors);
            }
        }

        private void UpdateShadowRendererList()
        {
            m_ShadowRendererList.Clear();
            foreach (Renderer r in m_Renderers)
            {
                m_ShadowRendererList.Add(r);
            }
        }

        private static class PropertyIds
        {
            public static readonly int _PerObjShadowCasterId = MemberNameHelpers.ShaderPropertyID();

        }
    }
}
