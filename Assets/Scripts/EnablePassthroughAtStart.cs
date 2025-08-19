using UnityEngine;

public class EnablePassthroughAtStart : MonoBehaviour
{
    void Awake()
    {
#if OCULUS_SDK || META_SDK
        var m = FindObjectOfType<OVRManager>();
        if (m) m.isInsightPassthroughEnabled = true;
        var layer = FindObjectOfType<OVRPassthroughLayer>();
        if (layer) layer.enabled = true;
#endif
    }
}
