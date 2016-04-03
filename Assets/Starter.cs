using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class Starter : MonoBehaviour {
	public GameObject startButton;
	public GameObject timeLabel;
	public GameObject replayButton;
	public Text scoreText;
	public Text timerText;
	public float firstLevelTime;
	public float timePerLevelDec;
	public float minTimePerLevel;
	Touch touch;
	float timer=0;
	// Use this for initialization
	void Start () {
		touch = GetComponent<Touch> ();
	}
	
	// Update is called once per frame
	void Update () {
		timer -= Time.deltaTime;
		if (timer <= 0 && !startButton.activeInHierarchy)
			GameOver ();
		timerText.text = timer.ToString ("#0.##");
	}

	public void BeginGame () {
		startButton.SetActive (false);
		timeLabel.SetActive (true);
		replayButton.SetActive (false);
		touch.enabled = true;
		timer = firstLevelTime;
		touch.level = 0;
	}

	void  GameOver () {
		replayButton.SetActive (true);
		scoreText.text = touch.level.ToString ("D");
		touch.ClearCurPoints ();
		touch.ClearLevelPoints ();
		touch.enabled = false;
		timeLabel.SetActive (false);
	}

	public void WinLevel () {
		touch.level++;
		touch.ClearCurPoints();
		touch.ClearLevelPoints();
		timer = firstLevelTime - timePerLevelDec * touch.level;
	}
}
