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
        // Start()는 Unity가 이 오브젝트가 생성된 뒤 자동으로 한 번 호출해주는 함수다(Awake보다 늦게 실행됨).
        private void Start()
        {
            // GetComponent<T>()는 같은 오브젝트에 붙어있는 다른 컴포넌트(여기서는 AudioSource)를 찾아오는 함수다.
            AudioSource source = GetComponent<AudioSource>();
            // 재생할 클립이 실제로 있고, 아직 재생 중이 아닐 때만 Play()를 호출한다.
            if (source != null && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }
    }
}
