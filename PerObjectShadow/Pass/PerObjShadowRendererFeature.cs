using System;
using FL.PerObjectShadow.Passes;
using FL.PerObjectShadow.PerObjectShadow;
using FL.PerObjectShadow.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FL.PerObjectShadow
{
    [HelpURL("https://srshader.stalomeow.com/")]
    [DisallowMultipleRendererFeature("Per Object Shadow")]
    public class PerObjShadowRendererFeature : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES2)]
        [UnityEditor.ShaderKeywordFilter.SelectIf(true, keywordNames: ShaderKeywordStrings.MainLightShadowScreen)]
        private const bool k_RequiresScreenSpaceShadowsKeyword = true;
#endif

        [SerializeField] private DepthBits m_SceneShadowDepthBits = DepthBits.Depth16;
        [SerializeField] private ShadowTileResolution m_SceneShadowTileResolution = ShadowTileResolution._512;
        [SerializeField] private bool m_SceneShadowDebugMode = false;

        [NonSerialized] private ShadowCasterManager m_SceneShadowCasterManager;

        [NonSerialized] private PerObjectShadowCasterPass m_ScenePerObjShadowPass;

        [NonSerialized] private ScreenSpaceShadowsPass m_ScreenSpaceShadowPass;


        public override void Create()
        {
            m_SceneShadowCasterManager = new ShadowCasterManager(ShadowUsage.Scene);

            m_ScenePerObjShadowPass = new PerObjectShadowCasterPass("MainLightPerObjectSceneShadow");


            m_ScreenSpaceShadowPass = new ScreenSpaceShadowsPass();
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // PreviewCamera 不会执行这部分代码！！！
            base.SetupRenderPasses(renderer, in renderingData);

            m_SceneShadowCasterManager.Cull(in renderingData, PerObjectShadowCasterPass.MaxShadowCount, m_SceneShadowDebugMode);
            m_ScenePerObjShadowPass.Setup(m_SceneShadowCasterManager, m_SceneShadowTileResolution, m_SceneShadowDepthBits);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // AfterRenderingShadows
            renderer.EnqueuePass(m_ScenePerObjShadowPass);

            // AfterRenderingGbuffer
            renderer.EnqueuePass(m_ScreenSpaceShadowPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_ScenePerObjShadowPass.Dispose();
            m_ScreenSpaceShadowPass.Dispose();

            base.Dispose(disposing);
        }

        private static class KeywordNames
        {
            public static readonly string _MAIN_LIGHT_SELF_SHADOWS = MemberNameHelpers.String();
            public static readonly string _MAIN_LIGHT_FRONT_HAIR_SHADOWS = MemberNameHelpers.String();
        }
    }
}
