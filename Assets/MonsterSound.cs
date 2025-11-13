using UnityEngine;

public class MonsterSound : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;   // 挂在怪物身上的 AudioSource

    [Header("Clips")]
    public AudioClip idleClip;        // 怪物平时的声音（呼吸 / 低吼）
    public AudioClip hurtClip;        // 被打时的声音
    public AudioClip deathClip;       // 死亡时的声音

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // 怪物出生后，循环播放 idle 声音
        if (audioSource != null && idleClip != null)
        {
            audioSource.clip = idleClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void PlayHurt()
    {
        if (audioSource != null && hurtClip != null)
            audioSource.PlayOneShot(hurtClip);
    }

    public void PlayDeath()
    {
        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);
    }
}
