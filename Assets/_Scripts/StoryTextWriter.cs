using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryTextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    public float speed;

    //Gibt an wie viele Seiten Story angezeigt werden müssen
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
        writeText("\nMoin du Nudel! Hier könnte deine Story stehen!");
    }

    //Methode zum Anzeigen von Text auf dem Computerbildschirm
    void writeText(string text)
    {
        //Die Coroutine zur Darstellung des Textes wird gestartet
        StartCoroutine(Type(text));
    }

    //Coroutine, die für die eigentliche Darstellung des Textes verantwortlich ist.
    IEnumerator Type(string text)
    {
        int counter = 0; 
        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
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
            //Der Text wird zurückgesetzt und der Button deaktiviert
            screenText.text = "";
            continue_button.SetActive(false);

            //TODO: Nächste Seite wird angezeigt

            //Debug
            writeText("Das ist die nächste Seite");

        }else {
            // Wenn alle Story Seiten angezeigt wurden wird die nächste Szene geladen
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
