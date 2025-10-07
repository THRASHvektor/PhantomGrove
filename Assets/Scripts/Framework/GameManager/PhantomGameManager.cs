using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhantomGameManager : MonoBehaviour
{
    public static PhantomGameManager Instance { get; private set; }
    
    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState CurrentGameState = GameState.Menu;


    void Awake()
    {
        Debug.Log("Phantom Game Awake");
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Phantom Game Start");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Phantom Game Tick");
    }
}
