using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryTextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    public float speed;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    public Text screenText;

    // Start is called before the first frame update
    void Start()
    {
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

        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
        foreach (char letter in text.ToCharArray())
        {
            screenText.text += letter;

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }
}
