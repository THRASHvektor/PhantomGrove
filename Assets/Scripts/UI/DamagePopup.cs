using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple world-space floating damage text. Requires a TextMeshPro (not UGUI) component on the prefab root.
/// Usage: instantiate prefab, then call Init(damageString, color).
/// </summary>
public class DamagePopup : MonoBehaviour
{
    public float floatUpSpeed = 0.8f;
    public float lifeTime = 1.0f;
    public Vector3 randomOffsetRange = new Vector3(0.2f, 0.1f, 0.2f);

    private TextMeshPro tmp;
    private Color startColor;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            Debug.LogWarning("DamagePopup: TextMeshPro component not found on prefab root.");
        }
    }

    public void Init(string text, Color color)
    {
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
            startColor = color;
        }
        // small random offset so multiple popups don't perfectly overlap
        transform.position += new Vector3(Random.Range(-randomOffsetRange.x, randomOffsetRange.x), Random.Range(0f, randomOffsetRange.y), Random.Range(-randomOffsetRange.z, randomOffsetRange.z));
        StartCoroutine(Life());
    }

    private IEnumerator Life()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        while (t < lifeTime)
        {
            float dt = Time.deltaTime;
            t += dt;
            // move up
            transform.position += Vector3.up * floatUpSpeed * dt;
            // fade out towards end
            if (tmp != null)
            {
                float alpha = Mathf.Clamp01(1f - (t / lifeTime));
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
