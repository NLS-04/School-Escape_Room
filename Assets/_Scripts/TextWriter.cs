using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextWriter : MonoBehaviour
{
    //Gibt die Tippgeschwindigkeit in Sekunden an
    // [Range(0, 50)]
    public uint charFrequency;
    float charWaitSeconds;

    //Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.
    [SerializeField] bool typing;

    //Das Textobjekt, das den Text auf dem Computerildschirm anzeigt
    [SerializeField] TMP_Text contentText= null;
    [SerializeField] TMP_Text answerText = null;
    [SerializeField] GameObject answerGameObj = null;

    [SerializeField] AudioSource audioSource;

    Content contents;
    TextSegment segment;
    uint segmentNumber;
    bool isTextAnswer;


    void Start() {
        DontDestroyOnLoad(this);

        contents = new Content();
        audioSource = GetComponent<AudioSource>();

        //Am Anfang wird typing auf false gesetzt, da am Anfang noch nicht getippt wird
        typing = false;
        charWaitSeconds = (float) 1/charFrequency;

        segmentSetup(1);
    }
    void Update() { }

    void segmentSetup(uint? id=null){
        contentText = GameObject.FindGameObjectWithTag("ContentText").GetComponent<TMP_Text>();
        answerText  = GameObject.FindGameObjectWithTag("AnswerText").GetComponent<TMP_Text>();
        answerGameObj = GameObject.FindGameObjectWithTag("AnswerGameObj");

        answerGameObj.SetActive(false);

        if ( !(id is null) ) segmentChoice((uint) id);
        writeSegment(true);
    }

    void segmentReset() {
        answerGameObj.SetActive(false);
        clearText();
        writeSegment(true);
    }

    void segmentChoice(uint id) {
        segmentNumber = id;
        segment = contents.GetTextSegment(segmentNumber);
        isTextAnswer = !segment.answer.isMultipleChoice;
    }

    #region Answer handling
        public void checkAnswer() {
            bool oldIsText = isTextAnswer;
            
            if(isTextAnswer) {
                TMP_InputField inputField = answerGameObj.GetComponent<TMP_InputField>();
                
                // is inputed answer equal to expected answer, disregarding Casings
                if(inputField.text.Equals(segment.answer.singleAnswer, System.StringComparison.CurrentCultureIgnoreCase)) {
                    segmentChoice(++segmentNumber);
                } else {
                    // wrong answer case
                    inputField.text = "~ WRONG ~";
                    return;
                }
            }

            // check if the new segment is not the same the segment before
            if(oldIsText != isTextAnswer) {
                // TODO: load the other scene
                segmentSetup();
                return;
            }
            
            segmentReset();
        }

    #endregion
    #region Darstellung des Textes auf dem Computerbildschirm
        void clearText() {
            contentText.text = "";
            answerText.text = "";
        }

        void writeSegment(bool activateAnsObj=false) {
            typing = true;
            audioSource.Play();
            StartCoroutine( Type(
                new string[2] {segment.content, segment.textAnswer},
                new TMP_Text[2] {contentText, answerText},
                activateAnsObj: activateAnsObj
            ) );
        }

        //Methode zum Anzeigen von Text auf dem Computerbildschirm
        void writeText(string text, TMP_Text txtchannel, bool activateAnsObj=false) {
            typing = true;
            audioSource.Play();
            StartCoroutine( Type( new string[1] {text}, new TMP_Text[1] {txtchannel}, activateAnsObj:activateAnsObj ) );
        }

        //Coroutine, die f�r die eigentliche Darstellung des Textes verantwortlich ist.
        IEnumerator Type(string[] texts, TMP_Text[] channels, bool activateAnsObj=false){
            if( texts.Length != channels.Length || texts.Length == 0 ) yield return null;

            //Der Counter gibt die Anzahl der Zeichen des Strings text an die bereits auf dem Bildschirm angezeigt werden
            int counter;  
                
            for(int i=0; i<texts.Length; i++) {
                counter=0;
                int length = texts[i].ToCharArray().Length;
                
                //Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
                foreach(char letter in texts[i].ToCharArray()) {
                    counter++;
                    channels[i].text += letter;

                    //Es wird gewartet; Wartedauer = Tippgeschwindigkeit
                    yield return new WaitForSeconds(charWaitSeconds);
                }
            }

            typing = false;
            // Wen nicht getippt wird, wird das Tippger�usch ausgeschaltet
            if (audioSource.isPlaying) audioSource.Stop();
            if (activateAnsObj && !(answerGameObj is null) ) answerGameObj.SetActive(true);            
        }

    #endregion
}
