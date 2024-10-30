using UnityEngine;

namespace FL.PerObjectShadow.PerObjectShadow
{
    public interface IShadowCaster
    {
        int Id { get; set; }

        ShadowRendererList.ReadOnly RendererList { get; }

        Transform Transform { get; }

        bool CanCastShadow(ShadowUsage usage);
    }
}
