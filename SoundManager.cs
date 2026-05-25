using UnityEngine;

/// <summary>
/// 게임 효과음 재생. Inspector에서 클립을 할당하고 각 상황에서 Play 메서드를 호출합니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("전투")]
    [SerializeField] AudioClip knifeSwingClip;
    [SerializeField] AudioClip gunFireClip;
    [SerializeField] AudioClip landmineTriggerClip;

    [Header("이동 스킬")]
    [Tooltip("은신+이속 증가")]
    [SerializeField] AudioClip hideStealthSpeedClip;
    [Tooltip("암흑 시야")]
    [SerializeField] AudioClip blindDarkVisionClip;
    [Tooltip("동결")]
    [SerializeField] AudioClip frozenClip;

    [Header("캐릭터")]
    [SerializeField] AudioClip characterDeathClip;

    [Header("상점")]
    [SerializeField] AudioClip shopPurchaseClip;

    [Header("채굴")]
    [SerializeField] AudioClip miningClip;

    [SerializeField] [Range(0f, 1f)] float sfxVolume = 1f;

    AudioSource _audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayKnifeSwing() => Play2D(knifeSwingClip);
    public void PlayGunFire() => Play2D(gunFireClip);
    public void PlayHideStealthSpeed() => Play2D(hideStealthSpeedClip);
    public void PlayBlindDarkVision() => Play2D(blindDarkVisionClip);
    public void PlayFrozen() => Play2D(frozenClip);
    public void PlayLandmineTrigger() => Play2D(landmineTriggerClip);
    public void PlayCharacterDeath() => Play2D(characterDeathClip);
    public void PlayShopPurchase() => Play2D(shopPurchaseClip);
    public void PlayMining() => Play2D(miningClip);

    public void PlayKnifeSwingAt(Vector3 worldPosition) => PlayAt(knifeSwingClip, worldPosition);
    public void PlayGunFireAt(Vector3 worldPosition) => PlayAt(gunFireClip, worldPosition);
    public void PlayHideStealthSpeedAt(Vector3 worldPosition) => PlayAt(hideStealthSpeedClip, worldPosition);
    public void PlayBlindDarkVisionAt(Vector3 worldPosition) => PlayAt(blindDarkVisionClip, worldPosition);
    public void PlayFrozenAt(Vector3 worldPosition) => PlayAt(frozenClip, worldPosition);
    public void PlayLandmineTriggerAt(Vector3 worldPosition) => PlayAt(landmineTriggerClip, worldPosition);
    public void PlayCharacterDeathAt(Vector3 worldPosition) => PlayAt(characterDeathClip, worldPosition);
    public void PlayMiningAt(Vector3 worldPosition) => PlayAt(miningClip, worldPosition);

    void Play2D(AudioClip clip)
    {
        if (clip == null || _audioSource == null)
            return;
        _audioSource.PlayOneShot(clip, sfxVolume);
    }

    void PlayAt(AudioClip clip, Vector3 worldPosition)
    {
        if (clip == null)
            return;
        AudioSource.PlayClipAtPoint(clip, worldPosition, sfxVolume);
    }
}
