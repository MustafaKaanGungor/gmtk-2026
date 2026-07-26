using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Endgamecontroller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void StartGame()
    {
        SceneManager.LoadScene("StartScene1");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    public void BacktoMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
