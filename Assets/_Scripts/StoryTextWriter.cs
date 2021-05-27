using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryTextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    public float speed;

    //Gibt an wie viele Seiten Story angezeigt werden m�ssen
    public int pages;

    //Nummer der aktuell angezeigten Seite
    private int current_page;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    public Text screenText;
    public GameObject continue_button;

    // Start is called before the first frame update
    void Start()
    {
        current_page = 1;

        //Debug
        writeText("\nMoin du Nudel! Hier k�nnte deine Story stehen!");
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

            if (counter == text.ToCharArray().Length) {
                continue_button.SetActive(true);
            }

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }

    public void buttonClicked() {
        current_page++;
        if (current_page <= pages)
        {
            //Der Text wird zur�ckgesetzt und der Button deaktiviert
            screenText.text = "";
            continue_button.SetActive(false);

            //TODO: N�chste Seite wird angezeigt

            //Debug
            writeText("Das ist die n�chste Seite");

        } else {
            // Wenn alle Story Seiten angezeigt wurden wird die n�chste Szene geladen
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
