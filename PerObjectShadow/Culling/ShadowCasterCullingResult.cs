using UnityEngine;

namespace FL.PerObjectShadow.PerObjectShadow
{
    internal struct ShadowCasterCullingResult
    {
        public IShadowCaster Caster;
        public int RendererIndexStartInclusive;
        public int RendererIndexEndExclusive;
        public Vector4 LightDirection;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;
    }
}
