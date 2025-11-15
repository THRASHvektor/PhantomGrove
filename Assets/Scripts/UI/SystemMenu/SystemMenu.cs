using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemMenu : MonoBehaviour
{
    Valve.VR.InteractionSystem.Hand leftHand;
    Valve.VR.InteractionSystem.Hand rightHand;
    void Start()
    {
        leftHand = GetComponentInParent<Valve.VR.InteractionSystem.Player>().leftHand;
        rightHand = GetComponentInParent<Valve.VR.InteractionSystem.Player>().rightHand;
        if (leftHand)
        {
            leftHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = true;
        }
        if (rightHand)
        {
            rightHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = true;
        }
        
    }
    public void PauseGame()
    {
        Debug.LogWarning("PauseGame!!");
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Debug.LogWarning("ResumeGame!!");
        Time.timeScale = 1f;
        if (leftHand)
        {
            leftHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = false;
        }
        if (rightHand)
        {
            rightHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = false;
        }
        Destroy(transform.parent.gameObject);
        
    }

    public void ExitGame()
    {
        Debug.LogWarning("Exit Game!!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

     public void RestartGame()
    {
        Time.timeScale = 1f;
        // Destroy this menu instance (it may be parented under a persistent SteamVR object)
        GameObject menuRoot = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(menuRoot);

        // Destroy persistent SteamVR Player if present to avoid duplicate hands after reload
        var svPlayer = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
        if (svPlayer != null)
        {
            Destroy(svPlayer.gameObject);
        }

        // Also attempt to destroy common SteamVR root objects that may be marked DontDestroyOnLoad
        var steamVrRoot = GameObject.Find("SteamVRObjects");
        if (steamVrRoot != null)
        {
            Destroy(steamVrRoot);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
