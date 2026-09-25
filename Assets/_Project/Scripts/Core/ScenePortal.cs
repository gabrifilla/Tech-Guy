using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ScenePortal : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private string _destination;
    [SerializeField, Min(.2f)] private float _radius = 1.25f;
    public string Destination => _destination;
    public void Configure(Transform player, string destination) { _player = player; _destination = destination; }
    private IEnumerator Start()
    {
        if (!_player || string.IsNullOrEmpty(_destination))
        { Debug.LogError("Portal requires a player and destination.", this); yield break; }
        var interval = new WaitForSeconds(.15f);
        while (_player)
        {
            Vector3 offset = _player.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude < _radius * _radius)
            {
                if (!Application.CanStreamedLevelBeLoaded(_destination))
                { Debug.LogError("Portal destination is missing from Build Settings: " + _destination, this); yield break; }
                SceneManager.LoadSceneAsync(_destination);
                yield break;
            }
            yield return interval;
        }
    }
}
