using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Bootstrapper : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitUntil(() => SoundManager.sndm != null);
        SceneManager.LoadScene("MainMenu");
    }
}