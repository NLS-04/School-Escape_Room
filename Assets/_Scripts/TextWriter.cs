using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    [Range(0f, 50f)]
    public float speed;

    //Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.
    [SerializeField] bool typing;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    [SerializeField] Text contentText;
    [SerializeField] Text answerText;

    [SerializeField] AudioSource audioSource;

    private Content contents;

    void Start()
    {
        contents = new Content();
        audioSource = GetComponent<AudioSource>();

        //Am Anfang wird typing auf false gesetzt, da am Anfang noch nicht getippt wird
        typing = false;

        //Debug, tnx i couldnt have known that there comes a debug
        writeSegment(1);
    }

    void Update()
    {
        // Wen nicht getippt wird, wird das Tippger�usch ausgeschaltet
        if (!typing && audioSource.isPlaying) {
            audioSource.Stop();
        }
    }

#region Darstellung des Textes auf dem Computerbildschirm
    // not work, needed: Coroutine management/timing :)
    void writeSegment(uint id) {
        TextSegment segment = contents.GetTextSegment(id);
        writeText(segment.content, contentText);
        writeText(segment.textAnswer, answerText);
    }

    //Methode zum Anzeigen von Text auf dem Computerbildschirm
    void writeText(string text, Text txtchannel) {

        //Typing wird auf true gesetzt, damit das Audio nicht abgebrochen wird
        typing = true;

        //Das Audio wird gestartet
        audioSource.Play();

        //Die Coroutine zur Darstellung des Textes wird gestartet
        StartCoroutine(Type(text, txtchannel));
    }

    //Coroutine, die f�r die eigentliche Darstellung des Textes verantwortlich ist.
    IEnumerator Type(string text, Text txtchannel){
        //Der Counter gibt die Anzahl der Zeichen des Strings text an die bereits auf dem Bildschirm angezeigt werden
        int counter = 0;

        char[] chArray = text.ToCharArray();

        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugef�gt
        foreach (char letter in chArray) {
            counter++;

            contentText.text += letter;

            // Wenn alle Zeichen dargestellt werden, wird typing auf false gesetzt damit das Audio abbricht
            if (counter >= chArray.Length)
                typing = false;

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }

#endregion
}
