using UnityEngine;

namespace XHHY.Game.Motion
{
    [CreateAssetMenu(fileName = "UiSfx", menuName = "炼丹游戏/UI音效", order = 41)]
    public class UiSfx : ScriptableObject
    {
        public AudioClip grab;
        public AudioClip swap;
        public AudioClip dock;
        public AudioClip toggle;

        [Range(0f, 1f)] public float volume = 0.35f;

        static AudioSource _source;

        public void PlayGrab() => Play(grab);
        public void PlaySwap() => Play(swap);
        public void PlayDock() => Play(dock);
        public void PlayToggle() => Play(toggle);

        void Play(AudioClip clip)
        {
            if (clip == null)
                return;
            EnsureSource();
            _source.PlayOneShot(clip, volume);
        }

        static void EnsureSource()
        {
            if (_source != null)
                return;
            var go = new GameObject("UiSfxPlayer");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }
    }
}
