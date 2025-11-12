using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

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
        Destroy(transform.parent.gameObject);
        if (leftHand)
        {
            leftHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = false;
        }
        if (rightHand)
        {
            rightHand.GetComponent<Valve.VR.InteractionSystem.UI_LaserPointer>().enabled = false;
        }
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

}
