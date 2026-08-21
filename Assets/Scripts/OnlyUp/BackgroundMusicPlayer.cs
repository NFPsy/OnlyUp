using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// AudioSource.Play()를 생성된 바로 그 프레임(GameBootstrap.Awake)에서 곧바로 호출하면
    /// 아직 컴포넌트 초기화가 끝나지 않아 조용히 무시되는 경우가 있다. Start()에서 재생하면
    /// 씬의 모든 Awake()가 끝난 뒤(다음 라이프사이클 단계)에 실행되는 것이 보장되어 안정적으로 재생된다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusicPlayer : MonoBehaviour
    {
        private void Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source != null && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }
    }
}
