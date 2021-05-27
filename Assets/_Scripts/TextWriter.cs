using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TextWriter : MonoBehaviour
{
    //// [Range(0, 50)]
    public uint charFrequency;  //* frequency char print for text displays
    float charWaitSeconds;      //* reciprocal of the frequency, i.e. the time to wait in seconds between two chars

    [SerializeField] bool typing;  //* Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.

    [SerializeField] TMP_Text    contentText   = null;  //* The Scenes Text Obj to display the current Segments Content
    [SerializeField] TMP_Text?   answerText    = null;  //* The Scenes Text Obj to display the current Segments textAnswer
    [SerializeField] GameObject? answerGameObj = null;  //* Hodling the the user Input Objs

    [SerializeField] AudioSource audioSource;
    [SerializeField] BatteryManager battManager;

    Content contents;       //* contents for the Scenes containing the riddles
    TextSegment segment;    //* current content Segment which the player is solving
    uint segmentNumber;     //* scalar refrence to the current Segment
    bool isTextAnswer;      //* indicates whether the current Segment is multiple or single Answer

    #region Scene constants
        //* static variables indicating in wich Scene/State we currently are
        const uint SCENE_MAIN_MENU       = 0;
        const uint SCENE_STORY           = 1;
        const uint SCENE_GAME_OVER       = 2;
        const uint SCENE_SINGLE_ANSWER   = 3;
        const uint SCENE_MULTIPLE_ANSWER = 4;

        static readonly string[] SCENE_CONTENTS       = {"Willkommen bei Spielname XY!", "", "Game Over!", "", ""};
        static readonly string[] SCENE_STORY_CONTENTS = {"\nMoin du Nudel! Hier könnte deine Story stehen!", "Das ist die nächste Seite"};

        uint SCENE_STORY_INDEX = 0;                //* Page index for story Scene is intialized to 0
        uint SCENE_IDENTIFIER  = SCENE_MAIN_MENU;  //* MAIN_MENU_SCENE is starting Scene
    #endregion


    void Start() {
        DontDestroyOnLoad(this);

        // Gather the contents for the Scenes
        contents = new Content();
        audioSource = GetComponent<AudioSource>();

        // Am Anfang wird typing auf false gesetzt, da am Anfang noch nicht getippt wird
        typing = false;
        charWaitSeconds = (float) 1/charFrequency;

        sceneSetup();
    }

    void Update() { }

    void getSceneRelevantObj(bool activateAnswerObj=false) {
        contentText   = GameObject.FindGameObjectWithTag("ContentText").GetComponent<TMP_Text>();
        answerText    = GameObject.FindGameObjectWithTag("AnswerText").GetComponent<TMP_Text>();
        answerGameObj = GameObject.FindGameObjectWithTag("AnswerGameObj");

        answerGameObj.SetActive(activateAnswerObj);
    }

    #region SCENES MANAGMENT
        void sceneSetup() {
            getSceneRelevantObj();
            writeText( SCENE_CONTENTS[SCENE_IDENTIFIER], contentText, true );
        }

        public void sceneButtonPress() {
            // if we are in the story Scene we check the page status to whether switch the Scene or turn the page
            if ( SCENE_IDENTIFIER == SCENE_STORY ) {
                // have all pages been flipped thru
                if ( SCENE_STORY_INDEX < SCENE_STORY_CONTENTS.Length-1 ) {
                    writeText( SCENE_STORY_CONTENTS[ ++SCENE_STORY_INDEX ], contentText, true );
                    return; //! ==> break HERE out of the Function
                }

                segmentChoice( 1 );  // preview the first scene to setup the segment data
            } else {
                // Current Scene is not the story Scene ==> Switch to according Scene
                if      ( SCENE_IDENTIFIER == SCENE_MAIN_MENU ) SCENE_IDENTIFIER = SCENE_STORY;
                else if ( SCENE_IDENTIFIER == SCENE_GAME_OVER ) SCENE_IDENTIFIER = SCENE_MAIN_MENU;
            }

            SceneManager.LoadScene( (int) SCENE_IDENTIFIER );
            sceneSetup();

            //* write first page of the Story if we have switched and loaded up the story Scene
            if ( SCENE_IDENTIFIER == SCENE_STORY )
                writeText( SCENE_STORY_CONTENTS[ SCENE_STORY_INDEX ], contentText, true );
        }
    #endregion
    #region SEGMENTS MANAGMENT
        void segmentSetup(uint? id=null){
            getSceneRelevantObj();

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
            segment       = contents.GetTextSegment(segmentNumber);
            isTextAnswer  = !segment.answer.isMultipleChoice;

            SCENE_IDENTIFIER = (uint) ( isTextAnswer ? SCENE_SINGLE_ANSWER : SCENE_MULTIPLE_ANSWER );
        }

        public void checkAnswer() {
            bool oldIsText = isTextAnswer;
            
            if(isTextAnswer) {
                TMP_InputField inputField = answerGameObj.GetComponent<TMP_InputField>();
                
                //* is NOT inputed answer equal to expected answer, disregarding Casings
                if( ! inputField.text.Equals(segment.answer.singleAnswer, System.StringComparison.CurrentCultureIgnoreCase) ) {
                    inputField.text = "~ WRONG ~";
                    battManager.wrongAnswer();
                    return; //! ==> break HERE out of the Function
                }

                segmentChoice(++segmentNumber);
            }

            //* check if the new segment is not the same the segment before
            if(oldIsText != isTextAnswer) {
                SceneManager.LoadScene( (int) SCENE_IDENTIFIER );
                segmentSetup();
                return; //! ==> break HERE out of the Function
            }
            
            segmentReset();
        }
    #endregion

    #region Darstellung des Textes auf dem Computerbildschirm
        void clearText() {
            contentText.text = "";
            answerText.text  = "";
        }

        // Methode zum Anzeigen von mehreren Texten nacheinander auf dem Computerbildschirm
        void writeSegment(bool activateAnsObj=false) {
            typing = true;
            audioSource.Play();
            StartCoroutine( Type(
                new string[2]   {segment.content, segment.textAnswer},
                new TMP_Text[2] {contentText, answerText},
                activateAnsObj: activateAnsObj
            ) );
        }

        // Methode zum Anzeigen von einem einzelnen Text auf dem Computerbildschirm
        void writeText(string text, TMP_Text txtchannel, bool activateAnsObj=false) {
            typing = true;
            audioSource.Play();
            StartCoroutine( Type( new string[1] {text}, new TMP_Text[1] {txtchannel}, activateAnsObj:activateAnsObj ) );
        }

        // Coroutine, die für die eigentliche Darstellung des Textes verantwortlich ist.
        IEnumerator Type(string[] texts, TMP_Text[] channels, bool activateAnsObj=false){
            if ( texts.Length != channels.Length || texts.Length == 0 )
                yield return null; //! ==> break HERE out of the Coroutine
            
            for (int i=0; i<texts.Length; i++) {                
                // Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
                foreach (char letter in texts[i].ToCharArray()) {
                    channels[i].text += letter;                    
                    yield return new WaitForSeconds(charWaitSeconds);  // Es wird gewartet; Wartedauer = Tippgeschwindigkeit
                }
            }

            typing = false;
            if (audioSource.isPlaying) audioSource.Stop();
            if (activateAnsObj && !(answerGameObj is null) ) answerGameObj.SetActive(true);  // Aktiviere das AnswerObjekt, wenn es existiert
        }

    #endregion
}
