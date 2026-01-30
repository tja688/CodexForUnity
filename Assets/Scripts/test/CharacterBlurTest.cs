using System.Collections;
using UnityEngine;

public class CharacterBlurTest : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("The starting Z value (Rest position)")]
    public float minZ = 0f;

    [Tooltip("The peak Z value (Blur position)")]
    public float maxZ = 30f;

    [Tooltip("Controls the curve sharpness. Higher = more extreme acceleration/deceleration.")]
    [Range(1.0f, 10.0f)]
    public float intensity = 3.0f;

    [Header("Blur (Key 1) Settings")]
    [Tooltip("Time taken for one leg of the journey (0->30 or 30->0). Total time is double this.")]
    public float oneWayDuration = 0.5f;

    [Header("Flip (Key 2) Settings")]
    [Tooltip("Total time for the flip (180 degrees)")]
    public float flipDuration = 1.0f;

    [Header("Movement Settings")]
    [Tooltip("Movement speed for WASD")]
    public float moveSpeed = 5f;

    private Coroutine _activeCoroutine;

    void Update()
    {
        // Press 1 to trigger the simple blur effect
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(PlayBlurEffect());
        }

        // Press 2 to trigger the flip effect
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(PlayFlipEffect());
        }

        // WASD Movement
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;

        if (moveX != 0 || moveY != 0)
        {
            Vector3 movement = new Vector3(moveX, moveY, 0).normalized * moveSpeed * Time.deltaTime;
            transform.localPosition += movement;
        }
    }

    private IEnumerator PlayBlurEffect()
    {
        // Phase 1: Go to Max (Slow then Fast -> EaseIn)
        float elapsed = 0f;
        while (elapsed < oneWayDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / oneWayDuration);

            // EaseIn: t^intensity

            float curveValue = Mathf.Pow(t, intensity);


            float z = Mathf.Lerp(minZ, maxZ, curveValue);
            SetLocalZ(z);


            yield return null;
        }
        SetLocalZ(maxZ);

        // Phase 2: Return to Min (Fast then Slow -> EaseOut)
        elapsed = 0f;
        while (elapsed < oneWayDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / oneWayDuration);

            // EaseOut: 1 - (1-t)^intensity

            float curveValue = 1.0f - Mathf.Pow(1.0f - t, intensity);

            // Lerp from Max back to Min

            float z = Mathf.Lerp(maxZ, minZ, curveValue);
            SetLocalZ(z);


            yield return null;
        }
        SetLocalZ(minZ);


        _activeCoroutine = null;
    }

    private IEnumerator PlayFlipEffect()
    {
        Quaternion startRot = transform.localRotation;
        // Flip 180 degrees around Y axis
        Quaternion endRot = startRot * Quaternion.Euler(0, 180f, 0);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);

            // 1. Rotation: Smooth interpolation
            transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

            // 2. Z-Axis Mapping based on flip progress
            // We want the Z effect to peak (MaxZ) at 50% of the flip (t=0.5)
            float z = minZ;

            if (t < 0.5f)
            {
                // First half: Move OUT (Min -> Max)
                // Normalize t from [0, 0.5] to [0, 1]
                float subT = t / 0.5f;

                // Use the same EaseIn curve mechanism as 'Blur'
                // Slow start, fast finish approaching the midpoint

                float curveValue = Mathf.Pow(subT, intensity);
                z = Mathf.Lerp(minZ, maxZ, curveValue);
            }
            else
            {
                // Second half: Move IN (Max -> Min)
                // Normalize t from [0.5, 1] to [0, 1]
                float subT = (t - 0.5f) / 0.5f;

                // Use the same EaseOut curve mechanism as 'Blur'
                // Fast start leaving midpoint, slow finish

                float curveValue = 1.0f - Mathf.Pow(1.0f - subT, intensity);

                // Lerp Max -> Min

                z = Mathf.Lerp(maxZ, minZ, curveValue);
            }

            SetLocalZ(z);
            yield return null;
        }

        // Ensure final state is exact
        transform.localRotation = endRot;
        SetLocalZ(minZ);


        _activeCoroutine = null;
    }

    private void SetLocalZ(float z)
    {
        Vector3 pos = transform.localPosition;
        pos.z = z;
        transform.localPosition = pos;
    }
}
