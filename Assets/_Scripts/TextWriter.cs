using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    public float speed;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    public Text screenText;

    void Start()
    {
        writeText("Drehle stinkt nach Maggi! Moin du h�ssliches St�ck Schei�e! Hallo! Hallo! Hallo! Das ist ein toller Beispieltext! Wer das liest ist doof! Drehle und J�rgen Zechel stinken nach Maggi! Hallo! Hallo! Hallo!");
    }

    #region Darstellung des Textes auf dem Computerbildschirm

    //Methode zum Anzeigen von Text auf dem Computerbildschirm
    void writeText(string text) {
        StartCoroutine(Type(text));
    }

    //Coroutine, die f�r die eigentliche Darstellung des Textes verantwortlich ist.
    IEnumerator Type(string text){
        //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugef�gt
        foreach (char letter in text.ToCharArray()) {
            screenText.text += letter;

            //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
            yield return new WaitForSeconds(speed);
        }
    }

    #endregion
}
