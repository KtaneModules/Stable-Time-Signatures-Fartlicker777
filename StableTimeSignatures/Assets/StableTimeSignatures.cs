using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StableTimeSignatures : MonoBehaviour
{
	// Use this for initialization

	public KMBombModule module;
	public KMAudio moduleAudio;
	public KMSelectable topButton;
	public KMSelectable bottomButton;

	public GameObject buttonBack;
	public TextMesh topButtonText;
	public TextMesh bottomButtonText;

	private string topButtonStates = "123456789";
	private string bottomButtonStates = "1248";
	private string currentState = "##";
	private string redNumber = "#";

	private string[] randomSequence = new string[3];
	private int amountCorrect = 0;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	private bool moduleSolved = false;
	private bool solving = false;

	private Coroutine buttonHold;
	private bool holding = false;

	bool PlayedOnce = false;

	private Coroutine playingSound;

	private

	void Awake()
	{
		_moduleId = _moduleIdCounter++;
		buttonBack.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, .6f, 1f, .4f, 1f);
	}

	void Start()
	{
		currentState = topButtonStates[Random.Range(0, 9)].ToString() + bottomButtonStates[Random.Range(0, 4)].ToString(); // Pick two random numbers
		GenerateColor();
		UpdateText();

		topButton.OnInteract += delegate { ButtonPressed(0); return false; };
		bottomButton.OnInteract += delegate { ButtonPressed(1); return false; };

		topButton.OnInteractEnded += delegate { ButtonDepressed(0); };
		bottomButton.OnInteractEnded += delegate { ButtonDepressed(1); };
	}

	void ButtonPressed(int buttonNum)
	{
		moduleAudio.PlaySoundAtTransform("Click", module.transform);
		ButtonMove(buttonNum, "down");
		if (moduleSolved) return;

		if (buttonHold != null)
		{
			holding = false;
			StopCoroutine(buttonHold);
			buttonHold = null;
		}

		buttonHold = StartCoroutine(HoldChecker());
	}

	void ButtonDepressed(int buttonNum)
	{
		moduleAudio.PlaySoundAtTransform("ClickOff", module.transform);
		ButtonMove(buttonNum, "up");
		if (moduleSolved) return;

		StopCoroutine(buttonHold);

		if (holding)
		{
			SubmitPressed(buttonNum);
		}
		else
		{
			if (buttonNum == 0)
			{
				if (currentState[0].ToString() == redNumber)
				{
					StopPlaying();
					GenerateColor();
					UpdateText();
					//amountCorrect = 0;
					playingSound = StartCoroutine(PlayRandomSequence());
				}
				else
				{
					CycleBottomScreen();
				}
			}
			else
			{
				CycleTopScreen();
			}
			UpdateText();
		}
	}

	void TwitchHandleForcedSolve()
	{
		StartCoroutine(ModuleSolve());
	}

	IEnumerator HoldChecker()
	{
		yield return new WaitForSeconds(.6f);
		holding = true;
		UpdateDisplayTo("  ");
	}

	void CycleTopScreen()
	{
		var newText = int.Parse(currentState[0].ToString()) % 9 + 1;
		currentState = newText + currentState[1].ToString();
	}

	void CycleBottomScreen()
	{
		var newText = int.Parse(currentState[1].ToString()) * 2 % 15;
		currentState = currentState[0].ToString() + newText;
	}

	void UpdateText()
	{
		topButtonText.text = currentState[0].ToString();
		bottomButtonText.text = currentState[1].ToString();

		topButtonText.color = topButtonText.text == redNumber ? new Color(.78f, 0, 0) : new Color(0, 0, 0);
	}

	void GenerateColor()
	{
		var redNum = topButtonStates[Random.Range(0, topButtonStates.Length)].ToString();
		redNumber = redNum;
		DebugLog("The red number is now {0}.", redNum);
	}

	private void SubmitPressed(int buttonNum)
	{
		if (currentState == randomSequence[amountCorrect])
		{
			amountCorrect++;
			UpdateText();
			StartCoroutine(PlayCorrectSound());
			DebugLog("You submitted [{0} {1}]. That's correct!", currentState[0], currentState[1]);
		}
		else
		{
			module.HandleStrike();
			StopPlaying();
			GenerateColor();
			UpdateText();
			amountCorrect = 0;
			DebugLog("You submitted [{0} {1}]. Thats wrong...", currentState[0], currentState[1]);

			if (randomSequence[amountCorrect] == null)
			{
				DebugLog("...you haven't generated a sequence yet!");
			}
		}
		if (amountCorrect == 3) StartCoroutine(ModuleSolve());
	}

	void UpdateDisplayTo(string display)
	{
		topButtonText.text = display[0].ToString();
		bottomButtonText.text = display[1].ToString();
	}

	void StopPlaying()
	{
		if (playingSound != null)
		{
			StopCoroutine(playingSound);
			playingSound = null;
		}
	}

	void ButtonMove(int buttonNum, string direction)
	{
		switch (direction)
		{
			case "down":
				buttonBack.transform.localEulerAngles = buttonNum == 0 ? new Vector3(92f, 0f, -180f) : new Vector3(88f, 0f, -180f);
				buttonBack.transform.localPosition = new Vector3(0f, .004f, 0f);
				break;
			case "up":
				buttonBack.transform.localEulerAngles = new Vector3(90f, 0f, -180f);
				buttonBack.transform.localPosition = new Vector3(0f, .007f, 0f);
				break;
		}
	}

	IEnumerator ModuleSolve()
	{
		StopPlaying();
		moduleSolved = true;
		solving = true;
		topButtonText.color = new Color(0, 0, 0);
		bottomButtonText.color = new Color(0, 0, 0);
		UpdateDisplayTo("  ");
		yield return new WaitForSeconds(1f);

		for (int i = 8; i > 0; i--)
		{
			moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
			UpdateDisplayTo(i.ToString() + "8");
			yield return new WaitForSeconds(.2f);

			for (int j = 0; j < i - 1; j++)
			{
				moduleAudio.PlaySoundAtTransform("Tap", module.transform);
				yield return new WaitForSeconds(.2f);
			}
		}
		moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
		topButtonText.color = new Color(.78f, 0, 0);
		bottomButtonText.color = new Color(.78f, 0, 0);
		yield return new WaitForSeconds(.2f);
		moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
		topButtonText.color = new Color(0, 0, 0);
		bottomButtonText.color = new Color(0, 0, 0);
		yield return new WaitForSeconds(.2f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
		UpdateDisplayTo("TS");
		yield return new WaitForSeconds(.3f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
		UpdateDisplayTo("II");
		yield return new WaitForSeconds(.3f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
		UpdateDisplayTo("MG");
		yield return new WaitForSeconds(.2f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
		UpdateDisplayTo("EN");
		yield return new WaitForSeconds(.4f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
		UpdateDisplayTo("  ");
		yield return new WaitForSeconds(.4f);
		moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
		moduleAudio.PlaySoundAtTransform("Ding", module.transform);
		topButtonText.color = new Color(0, .58f, 0);
		bottomButtonText.color = new Color(0, .58f, 0);
		UpdateDisplayTo("✓✓");
		module.HandlePass();
		DebugLog("Module solved!");
	}

	IEnumerator PlayRandomSequence()
	{
		// Make the sequence

		if (!PlayedOnce)
			for (int i = 0; i < randomSequence.Length; i++)
				randomSequence[i] = topButtonStates[Random.Range(0, 9)].ToString() + bottomButtonStates[Random.Range(0, 4)].ToString();


		if (!PlayedOnce)
			DebugLog("The sequence is [{0} {1}], [{2} {3}], [{4} {5}].", randomSequence[0][0], randomSequence[0][1], randomSequence[1][0], randomSequence[1][1], randomSequence[2][0], randomSequence[2][1]);

		PlayedOnce = true;

		// Play the sequence
		for (int i = 0; i < randomSequence.Length; i++)
		{
			var topNum = int.Parse(randomSequence[i][0].ToString());
			var bottomNum = int.Parse(randomSequence[i][1].ToString());
			var bps = Random.Range(.25f, .375f);
			var numOfTaps = topNum;

			switch (bottomNum)
			{
				case 1:
					numOfTaps *= 8;
					break;
				case 2:
					numOfTaps *= 4;
					break;
				case 4:
					numOfTaps *= 2;
					break;
				default:
					break;
			}

			for (int j = 0; j < numOfTaps; j++)
			{
				if (j == 0)
				{
					moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
					moduleAudio.PlaySoundAtTransform("hatquiter", module.transform);
				}
				else if (bottomNum == 8 || bottomNum == 4 && j % 2 == 0 || bottomNum == 2 && j % 4 == 0 || bottomNum == 1 && j % 8 == 0)
				{
					moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
					moduleAudio.PlaySoundAtTransform("hatquiter", module.transform);
				}
				else
				{
					moduleAudio.PlaySoundAtTransform("Tap", module.transform);
				}

				yield return new WaitForSeconds(bps);
			}
		}
	}

	IEnumerator PlayCorrectSound()
	{
		moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
		yield return new WaitForSeconds(.1f);
		moduleAudio.PlaySoundAtTransform("HighIshTap", module.transform);
		yield return new WaitForSeconds(.1f);
		moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
	}

	private void DebugLog(string log, params object[] args)
	{
		var logData = string.Format(log, args);
		Debug.LogFormat("[Stable Time Signatures #{0}] {1}", _moduleId, logData);
	}

	string TwitchHelpMessage = "Use '!{0} t1 h b2 c' to hit the top button once, hold the button, hit the bottom button twice, and cycle the top button.";

	int TwitchModuleScore = 12;

	IEnumerator ProcessTwitchCommand(string command)
	{
		var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (parts.All(x => x.Length == 2 && "tb".Contains(x[0]) && "123456789".Contains(x[1]) || x.Length == 1 && "hc".Contains(x)))
		{
			yield return null;

			for (int i = 0; i < parts.Length; i++)
			{
				var part = parts[i];

				if (part.Length == 2)
				{
					var buttonNumToPress = part[0] == 't' ? 0 : 1;
					var numPresses = int.Parse(part[1].ToString());

					for (int j = 0; j < numPresses; j++)
					{
						yield return "trycancel";
						ButtonPressed(buttonNumToPress);
						yield return new WaitForSeconds(.1f);
						ButtonDepressed(buttonNumToPress);
						yield return new WaitForSeconds(.1f);
					}
				}
				else
				{
					if (part == "c")
					{
						for (int j = 0; j < 9; j++)
						{
							yield return "trycancel";
							ButtonPressed(1);
							yield return new WaitForSeconds(.1f);
							ButtonDepressed(1);
							yield return new WaitForSeconds(.4f);
						}
					}
					else
					{
						yield return "trycancel";
						ButtonPressed(0);
						yield return new WaitForSeconds(1f);
						ButtonDepressed(0);
						yield return new WaitForSeconds(.1f);
					}

				}
			}

			if (solving)
				yield return "solve";
		}
	}
}
