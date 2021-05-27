using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject startGameButton;
    public float speed = 0.1f;
    public TMP_Text screenText;
    private AudioSource audio;

    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();
        writeText("");
        startGameButton.SetActive(false);
    }

    //Methode zum Anzeigen von Text auf dem Computerbildschirm
    void writeText(string text)
    {
        //Die Coroutine zur Darstellung des Textes wird gestartet
        StartCoroutine(Type(text));
    }

    //Coroutine, die f�r die eigentliche Darstellung des Textes verantwortlich ist.
    IEnumerator Type(string text)
    {
        int counter = 0;
        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugef�gt
        foreach (char letter in text.ToCharArray())
        {
            screenText.text += letter;
            counter++;

            if (counter == text.ToCharArray().Length)
            {
                startGameButton.SetActive(true);
                audio.Stop();
            }

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }

    //Methode, die beim klicken des Buttons ausgef�hrt wird (Spiel starten)
    public void startGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
