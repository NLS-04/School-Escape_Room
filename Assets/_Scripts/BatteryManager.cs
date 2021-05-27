using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BatteryManager : MonoBehaviour
{
    //Der Ladestand der Batterie
    public float battery_percentage = 100f;

    //Die Zeit in Sekunden, die es dauert bis sich der Batterieladestand um 1 verringert
    private float time_per_percent = 20f;

    //Z�hlt wie oft bereits ein Prozent auf Grund der Zeit abgezogen wurde
    private int counter = 0;

    private float percent_per_wrong_answer = 5f;

    //Gibt an, ob die Entladung der Batterie gestartet ist (mit Beginn des ersten R�tsels)
    private bool countdownStarted = false;

    //Zeitpunkt bei dem die Entladung der Batterie startet
    private float startTime;

    public Sprite[] batt_sprites = new Sprite[4];

    private SpriteRenderer batt_sprite;
    private TMP_Text batt_text;


    void Start() {
        batt_sprite = GameObject.FindGameObjectWithTag("BattSprite").GetComponent<SpriteRenderer>();
        batt_text = GameObject.FindGameObjectWithTag("BattText").GetComponent<TMP_Text>();

        //Debug
        startCountdown();
    }

    void Update() {
        if (countdownStarted){
            if (Time.time > counter * time_per_percent + startTime) {
                counter++;
                battery_percentage--;
                updateBattery();
            }
        }
    }

    public void startCountdown() {
        countdownStarted = true;
        startTime = Time.time;
    }

    public void wrongAnswer() {
        battery_percentage -= percent_per_wrong_answer;
        updateBattery();
    }

    private void gameOver() {
        SceneManager.LoadScene("GameOver");
        battery_percentage = 100f;
        Destroy(gameObject);
    }

    private void updateBattery() {
        if (battery_percentage <= 0) 
            gameOver();

        batt_text.text = Mathf.RoundToInt(battery_percentage).ToString() + " %";
        batt_text.color = Color.Lerp(Color.red, Color.green, 0.01f*battery_percentage);
        
        // why this formular works: https://www.desmos.com/calculator/9vsolgfbw4
        batt_sprite.sprite = batt_sprites[ Mathf.FloorToInt( 4 - .04f*battery_percentage ) ];
    }

}
