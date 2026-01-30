using UnityEngine;

[ExecuteAlways]
public class ShadowScreenController : MonoBehaviour
{
    public Transform puppet;
    [Tooltip("Distance scale for blur amount.")]
    public float blurDistanceScale = 0.15f;
    [Tooltip("Minimum emission intensity.")]
    public float emissionMin = 1.0f;
    [Tooltip("Maximum emission intensity.")]
    public float emissionMax = 1.2f;
    [Tooltip("Speed of emission flicker.")]
    public float flickerSpeed = 0.6f;
    [Tooltip("Shadow color saturation boost.")]
    public float saturation = 1.1f;

    private static readonly int BlurId = Shader.PropertyToID("_BlurAmount");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");

    private Renderer cachedRenderer;
    private Material cachedMaterial;

    private void OnEnable()
    {
        Cache();
    }

    private void Cache()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
        {
            cachedMaterial = cachedRenderer.sharedMaterial;
        }
    }

    private void Update()
    {
        if (cachedMaterial == null)
        {
            Cache();
            if (cachedMaterial == null)
            {
                return;
            }
        }

        if (puppet == null)
        {
            int layer = LayerMask.NameToLayer("PuppetLayer");
            if (layer >= 0)
            {
                foreach (var candidate in FindObjectsOfType<Transform>())
                {
                    if (candidate.gameObject.layer == layer)
                    {
                        puppet = candidate;
                        break;
                    }
                }
            }
        }

        float blur = 0f;
        if (puppet != null)
        {
            blur = Mathf.Abs(puppet.position.z - transform.position.z) * blurDistanceScale;
        }

        cachedMaterial.SetFloat(BlurId, blur);
        cachedMaterial.SetFloat(SaturationId, saturation);

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.5f);
        float emission = Mathf.Lerp(emissionMin, emissionMax, noise);
        cachedMaterial.SetFloat(EmissionId, emission);
    }
}
