using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

[Serializable]
public class Sound {
    public SOUND name;
    public AudioClip clip;
    public bool oneShot;
}

public enum SOUND {
    typing,
    correct,
    incorrect
}

public class TextWriter : MonoBehaviour {
    [Header("User Settables")]
    [SerializeField] uint charFrequency;    /// frequency char print for text displays
    [SerializeField] float charWaitSeconds; /// reciprocal of the frequency, i.e. the time to wait in seconds between two chars

    [SerializeField] bool typing;               /// Attribut, das angibt ob gerade Buchstaben auf den Bildschirm getippt werden.
    [SerializeField] bool activateAnsObjOnLoad; /// should the Answer Obj be activated after Scene Loads

    [Space, Header("Scene Specifics")]
    [SerializeField] TMP_Text?   contentText   = null;  /// The Scenes Text Obj to display the current Segments Content
    [SerializeField] TMP_Text?   answerText    = null;  /// The Scenes Text Obj to display the current Segments textAnswer
    [SerializeField] GameObject? answerGameObj = null;  /// Hodling the user Input Objs
    [SerializeField] GameObject? mcAnsGameObj  = null;  /// Hodling the refernce  to the scenes multiple Choice Objects Holder
    [SerializeField] MCToggle?[] mcAnswers     = null;  /// Hodling the refrences to the multiple Choice Answers
    [SerializeField] Button?     checkButton   = null;  /// Button which validates user input or turns pages

    [SerializeField] keyboardHandler? keyBHandler = null;  /// Script for single Answer scene which controls the Keyboard IO

    /// a look-up-table for contentText and answerText
    /// Coroutine<Type> looks this up and waits (if needed) until the selected channel is not null anymore, i.e. when the channel has a refrence in the current Scene
    public enum TEXT { contentText, answerText }
    Dictionary<TEXT, TMP_Text?> channelDictonary = new Dictionary<TEXT, TMP_Text?>(2) { {TEXT.contentText, null}, {TEXT.answerText, null} };


    [Space, Header("Script Specifics")]
    [SerializeField] Sound[] sounds;
    [SerializeField] AudioSource audioSource;
    [SerializeField] BatteryManager battManager;
    [SerializeField] GameObject MultipleChoiceObj;


    [Space, Header("State Indecators")]
    [SerializeField] uint segmentNumber;    /// scalar refrence to the current Segment
    [SerializeField] TextSegment segment;   /// current content Segment which the player is solving
    [SerializeField] Content contents;      /// contents for the Scenes containing the riddles
    bool isTextAnswer;                      /// indicates whether the current Segment is multiple or single Answer

    bool isInWrongAnswerMC = false;

    #region Scene management
        public static class TAG {
            public const string CONTENT_TEXT     = "ContentText";
            public const string ANSWER_TEXT      = "AnswerText";
            public const string ANSWER_GAMEOBJ   = "AnswerGameObj";
            public const string MULTIPLE_CHOICE  = "MultipleChoice";
        }

        //* static variables indicating in wich Scene/State we currently are
        //! Scene values MUST match with the corresponding BUILD INDEX
        public enum SCENE {
            AWAKE           = 0,  /// starting Scene, only called when App gets launched to asure to only load up the GAME_MANAGER Obj once
            MAIN_MENU       = 1,  /// Main Menu Scene, where you can start the Game
            STORY           = 2,  /// tells the Story this game underlies
            EMPTY_SCREEN    = 3,  /// Post Story Content which is displayed on the screen
            GAME_OVER       = 4,  /// Scene where the game pre ends due to an empty battery
            SINGLE_ANSWER   = 5,  /// Scene for Single Answer Segments
            MULTIPLE_ANSWER = 6,  /// Scene for Multiple Choise Answer Segments
            POS_STORY_SCENE = 7,  /// Scene last story paragraph

                //! GAME_ENEDED MUST be {10*GAME_OVER} since they point to the same SCENE
                //! but MUSTN'T be the same or else SCENE_CONTENTS can't distinguish between them
            GAME_ENDED = 10*GAME_OVER  /// Scene where the game ends after solving all Segments
        }

        static int _sceneAmount = Enum.GetNames(typeof(SCENE)).Length;  // the amount of referendable scenes, e.g MAIN_MENU, ...

        public static readonly Dictionary<SCENE, string> SCENE_CONTENTS = new Dictionary<SCENE, string>(_sceneAmount) {
            { SCENE.MAIN_MENU ,     "Willkommen bei der Mathematischen Schatzsuche!" },
            { SCENE.GAME_OVER ,     "Game Over!" },
            { SCENE.GAME_ENDED,     "Herzlichen Glückwunsch!\nDu hast alle Prüfungen erfolgreich gemeistert und hast dich damit als würdig erwiesen meinen Schatz zu erben. Du findest ihn in der Küche im Erdgeschoss des Hauses unter dem alten Holzboden. Ich wünsche dir viel Spaß damit!" },
            { SCENE.EMPTY_SCREEN,   "Hallo Fremder! Wenn du das hier liest, bin ich längst tot. Ich war viele Jahre ein erfolgreicher Abenteurer und Entdecker. Die Schätze, die ich auf diesen Reisen gefunden habe, möchte ich einer Person vererben die, dieser Schätze würdig ist. Dafür habe ich dich ausgesucht. Die Schätze sind irgendwo in diesem Haus versteckst. Bevor du sie aber erhältst musst du dich in mehreren Prüfungen und Rätseln beweisen. Dieser Computer werden dir dabei helfen die Prüfungen zu bestehen. Bedenke, dass sowohl deine Zeit als auch deine Lösungsversuche durch die Batterie dieses Computers begrenzt sind. Viel Glück!" },
            { SCENE.POS_STORY_SCENE,"Du bist erleichtert, dass du die Prüfungen deines Verwandten bestanden hast. Nach einer kurzen Verschnaufpause, stehst du auf und rennst so schnell du kannst in die Küche der alten Villa. Schnell hast du unter dem alten Boden ein Geheimfach entdeckt. Du ziehst aus dem Fach eine staubige Holzkiste. Welche kostbaren Schätze werden da wohl drinnen sein? Du öffnest mit großer Vorfreude den Verschluss der Kiste … Statt Reichtümern aus entfernten Ländern befinden sich in der Kiste bloß ein Stapel uralter Mathe-Bücher." }
        };

        public static readonly string[] STORY_CONTENTS = {
            "Vor ein paar Tagen erhieltst du überraschend ein Schreiben. Zuerst dachtest du, dass dieser Brief ein Fehler war, denn es wurde dir mitgeteilt, dass einer deiner entfernten Verwandten verstorben war und dass du damit der Erbe eines alten Hauses geworden bist. Da dir der Name dieser Person unbekannt ist, triffst du die Entscheidung das Haus dieses mysteriösen Verwandten zu besuchen um mehr über diesen Menschen herauszufinden.",
            "Kurze Zeit später stehst du vor einer alten, verwahrlosten Villa am Stadtrand. Kaum vorstellbar, dass hier bis vor kurzem noch jemand gewohnt hat! Als du das Haus betrittst stellst du überrascht fest, dass das Haus komplett leer ist. In keinem Raum der Villa stehen Möbel oder Gegenstände deines vermeintlichen Verwandten. Nachdem du alle Räum durchsucht hast, entscheidest du dich noch einen Blick auf den Dachboden zu werfen. Als du den Dachboden erreichst, dachtest du im ersten Moment, dass auch dieser vollkommen leer ist. Jedoch entdeckst du in einer Ecke des beengten Raums eine große Holztruhe.",
            "Du näherst dich der Kiste und öffnest den schweren Deckel. Mit großer Überraschung begutachtest du den Inhalt. Neben einem Stapel von alten Dokumenten befindet sich in der Truhe ein alter Computer der über ein Netz aus Kabeln mit einer überdimensionalen Batterie verbunden ist. Du hebst den Computer aus der Kiste und stellt ihn auf den knarzenden Holzboden. Nach einer kurzen Suche findest du an dem grauen Kasten einen Einschaltknopf. Als du die Taste drückst fängt der Computer an zu Surren und zeigt auf seinem Bildschirm eine Botschaft deines Verwandten an…"
        };

        public static readonly string[] POS_STORY_CONTENTS = {
            "Du bist erleichtert, dass du die Prüfungen deines Verwandten bestanden hast. Nach einer kurzen Verschnaufpause, stehst du auf und rennst so schnell du kannst in die Küche der alten Villa. Schnell hast du unter dem alten Boden ein Geheimfach entdeckt. Du ziehst aus dem Fach eine staubige Holzkiste. Welche kostbaren Schätze werden da wohl drinnen sein? Du öffnest mit großer Vorfreude den Verschluss der Kiste … Statt Reichtümern aus entfernten Ländern befinden sich in der Kiste bloß ein Stapel uralter Mathe-Bücher."
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
        typing = false;
        changeSceneToIndex( SCENE.MAIN_MENU );  // since we are in the Awake_Scene we must switch to the Main_Menu_Scene
    }


    void initiateLoadLevel(int? index=null) {
        // load Scene and set (every 'instance') of Scene relevant Objects to NULL, e.g. contentText, answerText
        SceneManager.LoadScene( index is null ? (int) SCENE_IDENTIFIER : (int) index );
        channelDictonary[TEXT.contentText] = contentText = null;
        channelDictonary[TEXT.answerText]  = answerText  = null;
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
            answerGameObj = GameObject.FindWithTag( TAG.ANSWER_GAMEOBJ );

            channelDictonary[TEXT.contentText] = contentText = tmpTextOrNull( TAG.CONTENT_TEXT );  //* add the Scenes current contentTexts: TMPText Component or NULL
            channelDictonary[TEXT.answerText]  = answerText  = tmpTextOrNull( TAG.ANSWER_TEXT );   //* add the Scenes current answerTexts: TMPText Component or NULL

            if (answerGameObj is null)
                return; //=> break HERE out if there is NO AnwerGameObj in this scene and (so no button can be set an onClick.Event)

            checkButton = answerGameObj.GetComponentInChildren<Button>();
            keyBHandler = answerGameObj.GetComponent<keyboardHandler>();

            answerGameObj.SetActive(activateAnsObjOnLoad);  //? keep an eye on that thingy: wont allways deactivate then the Story_Scene loads up the first time

            //_ set the checkButtons onClick function call to either [checkAnswer] if we are in a single or multiple Answer Scene or [sceneButtonPress]
            if ( isAnswerScene() ) {
                // set battManager depending on the whether it was not yet started or in a Scene Change
                if ( !battManager.countdownStarted ) battManager.setup(this);
                if (  battManager.isInSceneSwitch  ) battManager.endSceneChange();

                if ( SCENE_IDENTIFIER == SCENE.SINGLE_ANSWER )
                    keyBHandler.currentKeyMode = segment.answer.singleAnswer.mode;

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
            mcAnsGameObj = GameObject.FindWithTag( TAG.MULTIPLE_CHOICE );
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
                SCENE_IDENTIFIER = SCENE.EMPTY_SCREEN;
            } else {
                //: Current Scene is not the story Scene ==> Switch to according Scene
                switch( SCENE_IDENTIFIER ) {
                    case SCENE.MAIN_MENU:       SCENE_IDENTIFIER = SCENE.STORY;           break;
                    case SCENE.GAME_OVER:       SCENE_IDENTIFIER = SCENE.MAIN_MENU;       break;
                    case SCENE.GAME_ENDED:      SCENE_IDENTIFIER = SCENE.POS_STORY_SCENE; break;
                    case SCENE.EMPTY_SCREEN:    segmentChoice( 1 );                       break;  // 'preview' the first scene to setup the segment data and the current state
                    case SCENE.POS_STORY_SCENE: SCENE_IDENTIFIER = SCENE.MAIN_MENU;       break;
                }
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

            if ( SCENE_IDENTIFIER == SCENE.SINGLE_ANSWER ) {
                keyBHandler.currentKeyMode = segment.answer.singleAnswer.mode;
                keyBHandler.resetTexts();
            }
                
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

            playSound(SOUND.correct);
            
            if (isTextAnswer) {
                TMP_Text[] textFields = answerGameObj.GetComponentsInChildren<TMP_Text>();
                TMP_Text   textField  = textFields[textFields.Length-1];
                
                //* is NOT inputed answer equal to expected answer, disregarding Casings
                if( ! textField.text.Equals(segment.answer.singleAnswer.singleAnswer, System.StringComparison.CurrentCultureIgnoreCase) ) {
                    textField.text = "~ WRONG ~";
                    wrongAnswerAction();
                    return; //=> break HERE out to not futher execute code
                }

                textField.text = "";
            } else {
                //* scan that every Multiple Choice Answers is correctly selected by the user and return if other wise
                foreach ( MCToggle mct in mcAnswers ) {
                    if ( !mct.doesInputEqualChoice() ) {
                        if ( !isInWrongAnswerMC ) StartCoroutine( multipleChoiceWrongFeedBack() );
                        wrongAnswerAction();
                        return; //=> break HERE out to not futher execute code
                    }
                }

                // remove and reset the whole scene setup
                mcAnswers = null;
                foreach(Transform MCchild in getMCchildren<Transform>())
                    Destroy( MCchild.gameObject );
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

        void wrongAnswerAction() {
            battManager.wrongAnswer();
            playSound(SOUND.incorrect);
        }

        T[] getMCchildren<T>() {
            T[] GOs = new T[ mcAnsGameObj.transform.childCount ];
            for ( int i = 0; i < GOs.Length; i++ )
                GOs[i] = mcAnsGameObj.transform.GetChild(i).gameObject.GetComponent<T>();
            return GOs;
        }

        IEnumerator multipleChoiceWrongFeedBack( int times=2, float waitTime=0.1f ) {
            isInWrongAnswerMC = true;

            Image[]  childrenImgs    = getMCchildren<Image>();
            Toggle[] childrenToggles = getMCchildren<Toggle>();
            Color save = childrenImgs[0].color;  // Original Color to be reverted to
            
            setActionOnList( ref childrenToggles, x => x.isOn = true  );  // activate every Toggle so that the Image below is visible

            while( times-- > 0 ) {
                setActionOnList(ref childrenImgs, x => x.color = Color.red ); yield return new WaitForSecondsRealtime( waitTime );
                setActionOnList(ref childrenImgs, x => x.color = save      ); yield return new WaitForSecondsRealtime( waitTime );
            }

            setActionOnList( ref childrenToggles, x => x.isOn = false  );  // deactivate every Toggle so that the toggle is "reseted"
            isInWrongAnswerMC = false;
        }

        void setActionOnList<T>(ref T[] typeList, Action<T> dele) {
            try { foreach(T type in typeList) dele(type); } catch {}
        }
    #endregion

    #region Darstellung des Textes auf dem Computerbildschirm
        void clearText() {
            if ( !(contentText is null) ) contentText.text = "";
            if ( !(answerText  is null) ) answerText.text  = "";
        }
        
        void playSound(SOUND sound) {
            Sound s = Array.Find<Sound>(sounds, (ss) => { return ss.name == sound;} );

            audioSource.clip = s.clip;
            audioSource.loop = s.oneShot ? false : true;

            audioSource.Play();

            if ( s.name == SOUND.typing ) {
                if ( !(typing = !typing) ) audioSource.Stop();
            }
        }

        /// Methode zum Anzeigen von den jetztigen Segment Texten nacheinander
        void writeSegment(bool activateAnsObj=false) {
            StartCoroutine( Type(
                new string[2] {segment.content, segment.textAnswer},
                new TEXT[2]   {TEXT.contentText, TEXT.answerText},
                activateAnsObj: activateAnsObj
            ) );
        }

        /// Methode zum Anzeigen eines Texten im contentText
        void writeContent(string text, bool activateAnsObj=false) {
            writeText(text, TEXT.contentText, activateAnsObj: activateAnsObj);
        }

        /// Methode zum Anzeigen von einem einzelnen Text auf einem Channel
        void writeText(string text, TEXT txtchannel, bool activateAnsObj=false) {
            StartCoroutine( Type( new string[1] {text}, new TEXT[1] {txtchannel}, activateAnsObj:activateAnsObj ) );
        }

        /// Coroutine, die für die eigentliche Darstellung des Textes verantwortlich ist.
        IEnumerator Type( string[] texts, TEXT[] channels, bool activateAnsObj=false ){
            if ( channels.Length == 0 )            yield return null; //=> break HERE: No Channels given
            if ( texts.Length != channels.Length ) yield return null; //=> break HERE: Channels and Texts doesnt match

            // calculate seconds to wait between two chars foreach time Type gets called => inspector adjustment ability
            charWaitSeconds = (float) 1/charFrequency;
            
            // iterate thru every channel listed
            for (int i=0; i<channels.Length; i++) {
                //: hold Coroutine until channel does exist and can be written to
                yield return new WaitWhile( () => channelDictonary[channels[i]] is null || audioSource.isPlaying );

                playSound(SOUND.typing);

                TMP_Text curChannel = channelDictonary[channels[i]];
                
                // Es wird durch die einzelnen Zeichen des Textes durchiteriert und dann zum bisher angezeigten Text hinzugefügt
                foreach (char letter in texts[i].ToCharArray()) {
                    curChannel.text += letter;                    
                    yield return new WaitForSeconds(charWaitSeconds);  // Es wird gewartet; Wartedauer = Tippgeschwindigkeit
                }

                playSound(SOUND.typing);
            }

            if ( activateAnsObj && !(answerGameObj is null) ) answerGameObj.SetActive(true);  // Aktiviere das AnswerObjekt, wenn es existiert
        }
    #endregion
}
