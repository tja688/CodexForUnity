#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ShadowTheatreSetup
{
    private const string BasePath = "Assets/Arts/Prefabs/ShadowTheatre";
    private const string RenderTexturePath = BasePath + "/RT_ShadowProjection.renderTexture";
    private const string MaterialPath = BasePath + "/ShadowScreen.mat";
    private const string VolumeProfilePath = BasePath + "/ShadowTheatreVolumeProfile.asset";
    private const string PrefabPath = BasePath + "/ShadowTheatreSetup.prefab";

    [MenuItem("Tools/Shadow Theatre/Setup Scene")]
    public static void SetupScene()
    {
        var renderTexture = EnsureRenderTexture();
        var material = EnsureMaterial();
        var volumeProfile = EnsureVolumeProfile();

        EnsureMainCameraMask();

        var root = GameObject.Find("ShadowTheatreRoot");
        if (root == null)
        {
            root = new GameObject("ShadowTheatreRoot");
        }

        var shadowCam = EnsureShadowCam(root, renderTexture);
        var shadowScreen = EnsureShadowScreen(root, material, renderTexture);
        EnsureVolume(root, volumeProfile);

        var puppet = GameObject.Find("test image");
        if (puppet != null)
        {
            var controller = shadowScreen.GetComponent<ShadowScreenController>();
            if (controller != null)
            {
                controller.puppet = puppet.transform;
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(root.scene);

        Selection.activeObject = root;
    }

    private static RenderTexture EnsureRenderTexture()
    {
        var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (existing != null)
        {
            return existing;
        }

        var rt = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGB32)
        {
            name = "RT_ShadowProjection",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
            autoGenerateMips = false
        };
        rt.Create();
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        AssetDatabase.SaveAssets();
        return rt;
    }

    private static Material EnsureMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("Custom/ShadowScreen");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        var curtain = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/TextMesh Pro/Examples & Extras/Textures/Sunny Days - Seamless.jpg");
        if (curtain != null)
        {
            material.SetTexture("_MainTex", curtain);
        }

        var normalPath = "Assets/TextMesh Pro/Examples & Extras/Textures/Small Crate_normal.jpg";
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal != null)
        {
            EnsureNormalMap(normalPath);
            material.SetTexture("_NormalMap", normal);
        }

        var dirt = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/TextMesh Pro/Examples & Extras/Textures/Floor Cement.jpg");
        if (dirt != null)
        {
            material.SetTexture("_DirtTex", dirt);
        }

        material.SetColor("_LightColor", new Color(1f, 0.86f, 0.7f, 1f));

        AssetDatabase.SaveAssets();
        return material;
    }

    private static VolumeProfile EnsureVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.35f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.45f;

        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.7f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.0f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.65f;

        if (!profile.TryGet(out FilmGrain filmGrain))
        {
            filmGrain = profile.Add<FilmGrain>(true);
        }
        filmGrain.active = true;
        filmGrain.intensity.overrideState = true;
        filmGrain.intensity.value = 0.12f;
        filmGrain.response.overrideState = true;
        filmGrain.response.value = 0.4f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void EnsureMainCameraMask()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        int puppetMask = LayerMask.GetMask("PuppetLayer");
        mainCamera.cullingMask &= ~puppetMask;
    }

    private static GameObject EnsureShadowCam(GameObject root, RenderTexture renderTexture)
    {
        var existing = GameObject.Find("ShadowCam");
        var shadowCam = existing != null ? existing : new GameObject("ShadowCam");
        shadowCam.transform.SetParent(root.transform);
        shadowCam.transform.localPosition = new Vector3(0, 0, -5);
        shadowCam.transform.localRotation = Quaternion.identity;

        var camera = shadowCam.GetComponent<Camera>();
        if (camera == null)
        {
            camera = shadowCam.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.cullingMask = LayerMask.GetMask("PuppetLayer");
        camera.targetTexture = renderTexture;

        return shadowCam;
    }

    private static GameObject EnsureShadowScreen(GameObject root, Material material, RenderTexture renderTexture)
    {
        var existing = GameObject.Find("ShadowScreen");
        var screen = existing != null ? existing : GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "ShadowScreen";
        screen.transform.SetParent(root.transform);
        screen.transform.localPosition = Vector3.zero;
        screen.transform.localRotation = Quaternion.identity;
        screen.transform.localScale = new Vector3(10, 6, 1);

        var renderer = screen.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sharedMaterial.SetTexture("_ShadowTex", renderTexture);

        if (screen.GetComponent<ShadowScreenController>() == null)
        {
            screen.AddComponent<ShadowScreenController>();
        }

        return screen;
    }

    private static void EnsureVolume(GameObject root, VolumeProfile profile)
    {
        var existing = GameObject.Find("Global Volume");
        var volumeObject = existing != null ? existing : new GameObject("Global Volume");
        volumeObject.transform.SetParent(root.transform);
        volumeObject.transform.localPosition = Vector3.zero;
        volumeObject.transform.localRotation = Quaternion.identity;

        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeObject.AddComponent<Volume>();
        }
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    private static void EnsureNormalMap(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }
}
#endif
