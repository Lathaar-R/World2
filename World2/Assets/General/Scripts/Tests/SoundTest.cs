using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    private void Update() {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            AudioManeger.Instace.PlaySound("thump");
        }

        if(Input.GetKeyDown(KeyCode.M))
        {
            AudioManeger.Instace.PlayMusic("mainTheme", 2);
        }

        if(Input.GetKeyDown(KeyCode.N))
        {
            AudioManeger.Instace.PlayMusic("stranger-things-124008", 2);
        }

        if(Input.GetKeyDown(KeyCode.B))
        {
            AudioManeger.Instace.PlayPlaylist();
        }
    }
}
