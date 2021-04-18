using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    public float speed;

    //Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.
    public bool typing;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    public Text screenText;

    public AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        //Am Anfang wird typing auf false gesetzt, da am Anfang noch nicht getippt wird
        typing = false;

        //Debug
        writeText("Drehle stinkt nach Maggi! Moin du hässliches Stück Scheiße! Das ist ein toller Beispieltext!");

    }

    void Update()
    {
        // Wen nicht getippt wird, wird das Tippgeräusch ausgeschaltet
        if (typing == false && audioSource.isPlaying) {
            audioSource.Stop();
        }
    }

    #region Darstellung des Textes auf dem Computerbildschirm

    //Methode zum Anzeigen von Text auf dem Computerbildschirm
    void writeText(string text) {

        //Typing wird auf true gesetzt, damit das Audio nicht abgebrochen wird
        typing = true;

        //Das Audio wird gestartet
        audioSource.Play();

        //Die Coroutine zur Darstellung des Textes wird gestartet
        StartCoroutine(Type(text));
    }

    //Coroutine, die für die eigentliche Darstellung des Textes verantwortlich ist.
    IEnumerator Type(string text){
        //Der Counter gibt die Anzahl der Zeichen des Strings text an die bereits auf dem Bildschirm angezeigt werden
        int counter = 0;

        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
        foreach (char letter in text.ToCharArray()) {
            screenText.text += letter;

            counter++;

            // Wenn alle Zeichen dargestellt werden, wird typing auf false gesetzt damit das Audio abbricht
            if (counter == text.ToCharArray().Length) {
                typing = false;
            }

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }

    #endregion
}
