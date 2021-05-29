using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class TextWriter : MonoBehaviour {
    [Header("User Settables")]
    [Range(0, 50)]
    [SerializeField] uint charFrequency;  /// frequency char print for text displays
    float charWaitSeconds;                /// reciprocal of the frequency, i.e. the time to wait in seconds between two chars

    [SerializeField] bool typing;               /// Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.
    [SerializeField] bool activateAnsObjOnLoad; /// should the Answer Obj be activated after Scene Loads

    [Space, Header("Scene Specifics")]
    [SerializeField] TMP_Text?   contentText   = null;  /// The Scenes Text Obj to display the current Segments Content
    [SerializeField] TMP_Text?   answerText    = null;  /// The Scenes Text Obj to display the current Segments textAnswer
    [SerializeField] GameObject? answerGameObj = null;  /// Hodling the user Input Objs
    [SerializeField] GameObject? mcAnsGameObj  = null;  /// Hodling the refernce  to the scenes multiple Choice Objects Holder
    [SerializeField] MCToggle?[] mcAnswers     = null;  /// Hodling the refrences to the multiple Choice Answers
    [SerializeField] Button?     checkButton   = null;  /// Button which validates user input or turns pages

    /// a look-up-table for contentText and answerText
    /// Coroutine<Type> looks this up and waits (if needed) until the selected channel is not null anymore, i.e. when the channel has a refrence in the current Scene
    Dictionary<string, TMP_Text?> channelDictonary = new Dictionary<string, TMP_Text?>(2) { {"contentText", null}, {"answerText", null} };


    [Space, Header("Script Specifics")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] BatteryManager battManager;
    [SerializeField] GameObject MultipleChoiceObj;


    [Space, Header("State Indecators")]
    [SerializeField] uint segmentNumber;    /// scalar refrence to the current Segment
    [SerializeField] TextSegment segment;   /// current content Segment which the player is solving
    Content contents;                       /// contents for the Scenes containing the riddles
    bool isTextAnswer;                      /// indicates whether the current Segment is multiple or single Answer


    #region Scene management
        //* static variables indicating in wich Scene/State we currently are
        //! Scene values MUST match with the corresponding BUILD INDEX
        public enum SCENE {
            AWAKE           = 0,  /// starting Scene, only called when App gets launched to asure to only load up the GAME_MANAGER Obj once
            MAIN_MENU       = 1,  /// Main Menu Scene, where you can start the Game
            STORY           = 2,  /// tells the Story this game underlies
            GAME_OVER       = 3,  /// Scene where the game pre ends due to an empty battery
            SINGLE_ANSWER   = 4,  /// Scene for Single Answer Segments
            MULTIPLE_ANSWER = 5,  /// Scene for Multiple Choise Answer Segments

                //! GAME_ENEDED MUST be {10*GAME_OVER} since they point to the same SCENE
                //! but MUSTN'T be the same or else SCENE_CONTENTS can't distinguish between them
            GAME_ENDED      = 30  /// Scene where the game ends after solving all Segments
        }

        static int _sceneAmount = Enum.GetNames(typeof(SCENE)).Length;  // the amount of referendable scenes, e.g MAIN_MENU, ...

        public const string TAG_CONTENT_TEXT    = "ContentText";
        public const string TAG_ANSWER_TEXT     = "AnswerText";
        public const string TAG_ANSWER_GAMEOBJ  = "AnswerGameObj";
        public const string TAG_MULTIPLE_CHOICE = "MultipleChoice";

        static readonly string[] STORY_CONTENTS = {"\nMoin du Nudel! Hier könnte deine Story stehen!", "Das ist die nächste Seite"};
        static readonly Dictionary<SCENE, string> SCENE_CONTENTS = new Dictionary<SCENE, string>(_sceneAmount) {
            { SCENE.MAIN_MENU , "Willkommen bei Spielname XY!" },
            { SCENE.GAME_OVER , "Game Over!" },
            { SCENE.GAME_ENDED, "Herzlichen Glückwunsch!\n\nDu hast alle Rätsel erfolgreich absolviert und gezeigt,\ndass du dich in den mathematischen Künsten der 11. Jahrgangsstufe bewähren kannst" }
        };

        [SerializeField] 
        SCENE SCENE_IDENTIFIER = SCENE.AWAKE;  /// AWAKE is starting Scene
        int STORY_INDEX = 0;                   /// Page index for story Scene is intialized to 0
    #endregion
    

    void Awake() {
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += OnSceneLoaded;  //: appends Method to find the Scene Relevant Objects
    }

    void Start() {
        // Gather the contents (Segments) for the Scenes
        contents = new Content();

        playTypeSound(false);

        changeSceneToIndex( SCENE.MAIN_MENU );  // since we are in the Awake_Scene we must switch to the Main_Menu_Scene
    }


    void initiateLoadLevel(int? index=null) {
        // load Scene and set (every 'instance') of Scene relevant Objects to NULL, e.g. contentText, answerText
        SceneManager.LoadScene( index is null ? (int) SCENE_IDENTIFIER : (int) index );
        channelDictonary["contentText"] = contentText = null;
        channelDictonary["answerText"]  = answerText  = null;
        answerGameObj = mcAnsGameObj = null;
        mcAnswers     = null;
        checkButton   = null;
        
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //! this gets called AFTER a Scene is completely loaded
        //: to asure that all Objects are findable
        getSceneRelevantObj();
    }


    #region SCENES MANAGMENT
        void getSceneRelevantObj() {
            answerGameObj = GameObject.FindWithTag( TAG_ANSWER_GAMEOBJ );

            channelDictonary["contentText"] = contentText = tmpTextOrNull( TAG_CONTENT_TEXT );  //* add the Scenes current contentTexts: TMPText Component or NULL
            channelDictonary["answerText"]  = answerText  = tmpTextOrNull( TAG_ANSWER_TEXT );   //* add the Scenes current answerTexts: TMPText Component or NULL

            if (answerGameObj is null)
                return; //=> break HERE out if there is NO AnwerGameObj in this scene and (so no button can be set an onClick.Event)

            checkButton = answerGameObj.GetComponentInChildren<Button>();

            answerGameObj.SetActive(activateAnsObjOnLoad);  //? keep an eye on that thingy: wont allways deactivate then the Story_Scene loads up the first time

            //_ set the checkButtons onClick function call to either [checkAnswer] if we are in a single or multiple Answer Scene or [sceneButtonPress]
            if ( isAnswerScene() ) {
                // set battManager depending on the whether it was not yet started or in a Scene Change
                if ( !battManager.countdownStarted ) battManager.setup(this);
                if (  battManager.isInSceneSwitch  ) battManager.endSceneChange();

                checkButton.onClick.AddListener( checkAnswer );
            } else {
                checkButton.onClick.AddListener( sceneButtonPress );
            }

            if ( SCENE_IDENTIFIER == SCENE.MULTIPLE_ANSWER ) multipleChoiceSetup();
        }

        TMP_Text? tmpTextOrNull(string tag) {
            GameObject? obj = GameObject.FindWithTag(tag);
            return obj ? obj.GetComponent<TMP_Text>() : null;
        }
        
        void multipleChoiceSetup() {
            //* shortly activate the inputGameObject so that the Multiple Choice Holder can be found
            answerGameObj.SetActive(true);
            mcAnsGameObj = GameObject.FindWithTag( TAG_MULTIPLE_CHOICE );
            answerGameObj.SetActive(activateAnsObjOnLoad);

            mcAnswers = new MCToggle[segment.answer.multipleChoices.Length];

            for( int i = 0; i < mcAnswers.Length; i++ ) {
                GameObject mcGO = Instantiate( MultipleChoiceObj, mcAnsGameObj.transform );
                mcAnswers[i] = mcGO.GetComponent<MCToggle>().initialize( segment.answer.multipleChoices[i] );
            }
        }

        bool isAnswerScene() {
            return SCENE_IDENTIFIER == SCENE.SINGLE_ANSWER || SCENE_IDENTIFIER == SCENE.MULTIPLE_ANSWER;
        }

        void scenePrintToScreen() {
            //* is current Scene a Scene with stand alone (content) Text
            if ( SCENE_CONTENTS.ContainsKey( SCENE_IDENTIFIER ) )
                writeContent( SCENE_CONTENTS[SCENE_IDENTIFIER], true );

            //* write first page of the Story if we have switched and loaded up the story Scene
            if ( SCENE_IDENTIFIER == SCENE.STORY )
                writeContent( STORY_CONTENTS[STORY_INDEX], true );

            //* is current Scene a Scene with Answer
            if ( isAnswerScene() )
                writeSegment( true );
        }

        public void sceneButtonPress() {
            // clear Text fields and 'hide' the answerObject, i.e. the checkButton
            clearText();
            answerGameObj.SetActive(activateAnsObjOnLoad);

            //: if we are in the story Scene we check the page status to whether switch the Scene or turn the page
            if ( SCENE_IDENTIFIER == SCENE.STORY ) {
                // have all pages been flipped thru?
                if ( STORY_INDEX < STORY_CONTENTS.Length-1 ) {
                    writeContent( STORY_CONTENTS[ ++STORY_INDEX ], true );
                    return; //=> break HERE out to not futher execute code
                }

                STORY_INDEX = 0;

                segmentChoice( 1 );  // 'preview' the first scene to setup the segment data
            } else {
                //: Current Scene is not the story Scene ==> Switch to according Scene
                if      ( SCENE_IDENTIFIER == SCENE.MAIN_MENU ) SCENE_IDENTIFIER = SCENE.STORY;
                else if ( SCENE_IDENTIFIER == SCENE.GAME_OVER
                        ||SCENE_IDENTIFIER == SCENE.GAME_ENDED) SCENE_IDENTIFIER = SCENE.MAIN_MENU;
            }

            initiateLoadLevel();
            scenePrintToScreen();
        }
    
        public void changeSceneToIndex(SCENE index) {            
            SCENE_IDENTIFIER = index;
            initiateLoadLevel();
            scenePrintToScreen();
        }
    #endregion

    #region SEGMENTS MANAGMENT
        void segmentReset() {
            if ( answerGameObj is null ) return; //=> break out HERE, this case only happend then we enter the GAME_END_SCENE 

            answerGameObj.SetActive(false);
            clearText();
            writeSegment( true );
        }

        void segmentChoice(uint id) {
            //* stop getting other Segments after all have been solved; go to the Game_End_Scene
            if ( id > contents.GetSegmentAmount() ) {
                SCENE_IDENTIFIER = SCENE.GAME_ENDED;
                initiateLoadLevel( (int) SCENE_IDENTIFIER/10 ); //: regard comments on SCENE enum for clarity
                scenePrintToScreen();
                battManager.stop();
                return; //=> break out HERE if the need to go to the END SCENE
            }

            segmentNumber = id;
            segment       = contents.GetTextSegment(segmentNumber);
            isTextAnswer  = !segment.answer.isMultipleChoice;

            SCENE_IDENTIFIER = isTextAnswer ? SCENE.SINGLE_ANSWER : SCENE.MULTIPLE_ANSWER;
        }

        public void checkAnswer() {
            bool oldIsText = isTextAnswer;
            
            if (isTextAnswer) {
                TMP_InputField inputField = answerGameObj.GetComponentInChildren<TMP_InputField>();
                
                //* is NOT inputed answer equal to expected answer, disregarding Casings
                if( ! inputField.text.Equals(segment.answer.singleAnswer, System.StringComparison.CurrentCultureIgnoreCase) ) {
                    inputField.text = "~ WRONG ~";
                    battManager.wrongAnswer();
                    return; //=> break HERE out to not futher execute code
                }

                inputField.text = "";
            } else {
                //* scan that every Multiple Choice Answers is correctly selected by the user and return if other wise
                foreach ( MCToggle mct in mcAnswers ) {
                    if ( !mct.doesInputEqualChoice() ) {
                        //todo add some user feedback that his answer was wrong: e.g. some visuals like red color etc
                        battManager.wrongAnswer();
                        return; //=> break HERE out to not futher execute code
                    }
                }

                // remove and reset the whole scene setup
                mcAnswers = null;
                for ( int i = 0; i < mcAnsGameObj.transform.childCount; i++ ) {
                    Destroy( mcAnsGameObj.transform.GetChild(i).gameObject );
                }
            }

            segmentChoice(++segmentNumber);

            //* check if the new segment is not the same the segment before
            if (oldIsText != isTextAnswer) {
                battManager.initiateSceneChange();
                initiateLoadLevel();
                writeSegment( true );
                return; //=> break HERE out of the Function
            }
            
            segmentReset();
            if ( !isTextAnswer ) multipleChoiceSetup();
        }
    #endregion

    #region Darstellung des Textes auf dem Computerbildschirm
        void clearText() {
            if ( !(contentText is null) ) contentText.text = "";
            if ( !(answerText  is null) ) answerText.text  = "";
        }

        void playTypeSound(bool state) {
            typing = state;
            if ( typing ) audioSource.Play();
            else if ( audioSource.isPlaying ) audioSource.Stop();
        }

        /// Methode zum Anzeigen von den jetztigen Segment Texten nacheinander
        void writeSegment(bool activateAnsObj=false) {
            StartCoroutine( Type(
                new string[2] {segment.content, segment.textAnswer},
                new string[2] {"contentText", "answerText"},
                activateAnsObj: activateAnsObj
            ) );
        }

        /// Methode zum Anzeigen eines Texten im contentText
        void writeContent(string text, bool activateAnsObj=false) {
            writeText(text, "contentText", activateAnsObj: activateAnsObj);
        }

        /// Methode zum Anzeigen von einem einzelnen Text auf einem Channel
        void writeText(string text, string txtchannel, bool activateAnsObj=false) {
            StartCoroutine( Type( new string[1] {text}, new string[1] {txtchannel}, activateAnsObj:activateAnsObj ) );
        }

        /// Coroutine, die für die eigentliche Darstellung des Textes verantwortlich ist.
        IEnumerator Type( string[] texts, string[] channelsNames, bool activateAnsObj=false ){
            if ( channelsNames.Length == 0 )            yield return null; //=> break HERE: No Channels given
            if ( texts.Length != channelsNames.Length ) yield return null; //=> break HERE: Channels and Texts doesnt match

            // calculate seconds to wait between two chars foreach time Type gets called => inspector adjustment ability
            charWaitSeconds = (float) 1/charFrequency;
            
            // iterate thru every channel listed
            for (int i=0; i<channelsNames.Length; i++) {
                //: hold Coroutine until channel does exist and can be written to
                while ( channelDictonary[channelsNames[i]] is null )
                    yield return new WaitForSeconds(0.01f);

                playTypeSound(true);
                TMP_Text curChannel = channelDictonary[channelsNames[i]];
                
                // Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
                foreach (char letter in texts[i].ToCharArray()) {
                    curChannel.text += letter;                    
                    yield return new WaitForSeconds(charWaitSeconds);  // Es wird gewartet; Wartedauer = Tippgeschwindigkeit
                }
            }

            playTypeSound(false);
            if ( activateAnsObj && !(answerGameObj is null) ) answerGameObj.SetActive(true);  // Aktiviere das AnswerObjekt, wenn es existiert
        }
    #endregion
}
