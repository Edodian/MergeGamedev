using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public GameObject MainMenu;
    public SceneReference NextScene;
    void Start()
    {
        MainMenu.SetActive(true);
        if(SoundManager.sndm==null){
            StartCoroutine(WaitForSndmInit());
        }else{
            MenuInit();
        }
    }

    public void OpenMainMenu()
    {
        MainMenu.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
    public void ResetProgress()
    {
        PlayerPrefs.SetInt("LelvelsPassed", 0);
    }
    public void BeginGame()
    {
        SceneManager.LoadScene(NextScene.SceneName);
    }
    
    private IEnumerator WaitForSndmInit()
    {
        while (SoundManager.sndm == null)
        yield return null;
        MenuInit();
    }   
    private void MenuInit()
    {
        SoundManager.sndm.StopAllSounds();
        SoundManager.sndm.Play("MainTheme");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

