using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

public class StableTimeSignatures : MonoBehaviour {
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

   public List<string> randomSequenceTop = new List<string> { };
   public List<string> randomSequenceBottom = new List<string> { };
   private int amountCorrect = 0;

   private static int _moduleIdCounter = 1;
   private int _moduleId;

   private bool moduleSolved = false;
   private bool solving = false;

   private Coroutine buttonHold;
   private bool holding = false;

   bool okayExishThisModuleHasSolved;

   bool PlayedOnce = false;

   int GoalNumber;

   private Coroutine playingSound;
   #region ModSettings
      StableTimeSignaturesSettings Settings = new StableTimeSignaturesSettings();
   #pragma warning disable 414
   private static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
   {
      new Dictionary<string, object>
      {
        { "Filename", "StableTimeSignaturesSettings.json"},
        { "Name", "Stable Time Signatures" },
        { "Listings", new List<Dictionary<string, object>>
        {
          new Dictionary<string, object>
          {
            { "Key", "Amount" },
            { "Text", "Edit how many time signatures the module will give."}
          }
        }}
      }
   };
#pragma warning restore 414

   class StableTimeSignaturesSettings {
      public int Amount = 3;
   }

   #endregion☺

   void Awake () {
      _moduleId = _moduleIdCounter++;
      buttonBack.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, .6f, 1f, .4f, 1f);
      ModConfig<StableTimeSignaturesSettings> modConfig = new ModConfig<StableTimeSignaturesSettings>("StableTimeSignaturesSettings");
      Settings = modConfig.Read();
      GoalNumber = Settings.Amount;

      string missionDesc = KTMissionGetter.Mission.Description;
      if (missionDesc != null) {
         Regex regex = new Regex(@"\[Stable Time Signatures\] (TS=\d{1,4})");
         var match = regex.Match(missionDesc);
         if (match.Success) {
            string[] options = match.Value.Replace("[Stable Time Signatures] TS=", "").Split(',');
            int value = 3;
            int.TryParse(options[0], out value);

            GoalNumber = value;
         }
      }

      if (GoalNumber < 1) {
         GoalNumber = 3;
         Settings.Amount = 3;
      }
      if (GoalNumber > 1000) {
         GoalNumber = 1000;
         Settings.Amount = 1000;
      }
      // If there are any isues, write the default settings
      modConfig.Write(Settings);
   }


   void Start () {
      currentState = topButtonStates[Random.Range(0, 9)].ToString() + bottomButtonStates[Random.Range(0, 4)].ToString(); // Pick two random numbers
      GenerateColor();
      UpdateText();

      topButton.OnInteract += delegate { ButtonPressed(0); return false; };
      bottomButton.OnInteract += delegate { ButtonPressed(1); return false; };

      topButton.OnInteractEnded += delegate { ButtonDepressed(0); };
      bottomButton.OnInteractEnded += delegate { ButtonDepressed(1); };
   }

   void ButtonPressed (int buttonNum) {
      moduleAudio.PlaySoundAtTransform("Click", module.transform);
      ButtonMove(buttonNum, "down");
      if (moduleSolved) return;

      if (buttonHold != null) {
         holding = false;
         StopCoroutine(buttonHold);
         buttonHold = null;
      }

      buttonHold = StartCoroutine(HoldChecker());
   }

   void ButtonDepressed (int buttonNum) {
      moduleAudio.PlaySoundAtTransform("ClickOff", module.transform);
      ButtonMove(buttonNum, "up");
      if (moduleSolved) return;

      StopCoroutine(buttonHold);

      if (holding) {
         SubmitPressed(buttonNum);
      }
      else {
         if (buttonNum == 0) {
            if (currentState[0].ToString() == redNumber) {
               StopPlaying();
               GenerateColor();
               UpdateText();
               //amountCorrect = 0;
               playingSound = StartCoroutine(PlayRandomSequence());
            }
            else {
               CycleBottomScreen();
            }
         }
         else {
            CycleTopScreen();
         }
         UpdateText();
      }
   }

   /*void TwitchHandleForcedSolve () {
      StartCoroutine(ModuleSolve());
   }*/

   IEnumerator HoldChecker () {
      yield return new WaitForSeconds(.6f);
      holding = true;
      UpdateDisplayTo("  ");
   }

   void CycleTopScreen () {
      var newText = int.Parse(currentState[0].ToString()) % 9 + 1;
      currentState = newText + currentState[1].ToString();
   }

   void CycleBottomScreen () {
      var newText = int.Parse(currentState[1].ToString()) * 2 % 15;
      currentState = currentState[0].ToString() + newText;
   }

   void UpdateText () {
      topButtonText.text = currentState[0].ToString();
      bottomButtonText.text = currentState[1].ToString();

      topButtonText.color = topButtonText.text == redNumber ? new Color(.78f, 0, 0) : new Color(0, 0, 0);
   }

   void GenerateColor () {
      var redNum = topButtonStates[Random.Range(0, topButtonStates.Length)].ToString();
      redNumber = redNum;
      DebugLog("The red number is now {0}.", redNum);
   }

   private void SubmitPressed (int buttonNum) {
      if (randomSequenceTop.Count() == 0) {
         DebugLog("...you haven't generated a sequence yet!");
         module.HandleStrike();
         StopPlaying();
         GenerateColor();
         UpdateText();
         return;
      }
      if (currentState[0].ToString() == randomSequenceTop[amountCorrect] && currentState[1].ToString() == randomSequenceBottom[amountCorrect]) {
         amountCorrect++;
         UpdateText();
         StartCoroutine(PlayCorrectSound());
         DebugLog("You submitted [{0} {1}]. That's correct!", currentState[0], currentState[1]);
      }
      else {
         module.HandleStrike();
         StopPlaying();
         GenerateColor();
         UpdateText();
         amountCorrect = 0;
         DebugLog("You submitted [{0} {1}]. Thats wrong...", currentState[0], currentState[1]);
      }
      if (amountCorrect == GoalNumber) StartCoroutine(ModuleSolve());
   }

   void UpdateDisplayTo (string display) {
      topButtonText.text = display[0].ToString();
      bottomButtonText.text = display[1].ToString();
   }

   void StopPlaying () {
      if (playingSound != null) {
         StopCoroutine(playingSound);
         playingSound = null;
      }
   }

   void ButtonMove (int buttonNum, string direction) {
      switch (direction) {
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

   IEnumerator ModuleSolve () {
      StopPlaying();
      moduleSolved = true;
      solving = true;
      topButtonText.color = new Color(0, 0, 0);
      bottomButtonText.color = new Color(0, 0, 0);
      UpdateDisplayTo("  ");
      yield return new WaitForSeconds(1f);

      for (int i = 8; i > 0; i--) {
         moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
         UpdateDisplayTo(i.ToString() + "8");
         yield return new WaitForSeconds(.2f);

         for (int j = 0; j < i - 1; j++) {
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
      okayExishThisModuleHasSolved = true;
      DebugLog("Module solved!");
   }

   IEnumerator PlayRandomSequence () {
      // Make the sequence

      if (!PlayedOnce)
         for (int i = 0; i < GoalNumber; i++) {
            randomSequenceTop.Add(topButtonStates[Random.Range(0, 9)].ToString());
            randomSequenceBottom.Add(bottomButtonStates[Random.Range(0, 4)].ToString());
         }


      if (!PlayedOnce) {
         DebugLog("The sequence is:");
         for (int i = 0; i < GoalNumber; i++) {
            DebugLog("[{0} {1}]", randomSequenceTop[i], randomSequenceBottom[i]);
         }
      }

      PlayedOnce = true;

      // Play the sequence
      for (int i = 0; i < GoalNumber; i++) {
         var topNum = int.Parse(randomSequenceTop[i].ToString());
         var bottomNum = int.Parse(randomSequenceBottom[i].ToString());
         var bps = Random.Range(.25f, .375f);
         var numOfTaps = topNum;

         switch (bottomNum) {
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

         for (int j = 0; j < numOfTaps; j++) {
            if (j == 0) {
               moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
               moduleAudio.PlaySoundAtTransform("hatquiter", module.transform);
            }
            else if (bottomNum == 8 || bottomNum == 4 && j % 2 == 0 || bottomNum == 2 && j % 4 == 0 || bottomNum == 1 && j % 8 == 0) {
               moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
               moduleAudio.PlaySoundAtTransform("hatquiter", module.transform);
            }
            else {
               moduleAudio.PlaySoundAtTransform("Tap", module.transform);
            }

            yield return new WaitForSeconds(bps);
         }
      }
   }

   IEnumerator PlayCorrectSound () {
      moduleAudio.PlaySoundAtTransform("EmphasizedTap", module.transform);
      yield return new WaitForSeconds(.1f);
      moduleAudio.PlaySoundAtTransform("HighIshTap", module.transform);
      yield return new WaitForSeconds(.1f);
      moduleAudio.PlaySoundAtTransform("HighTap", module.transform);
   }

   private void DebugLog (string log, params object[] args) {
      var logData = string.Format(log, args);
      Debug.LogFormat("[Stable Time Signatures #{0}] {1}", _moduleId, logData);
   }

   string TwitchHelpMessage = "Use '!{0} t1 h b2 c' to hit the top button once, hold the button, hit the bottom button twice, and cycle the top button.";

   int TwitchModuleScore = 12;

   IEnumerator ProcessTwitchCommand (string command) {
      var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (parts.All(x => x.Length == 2 && "tb".Contains(x[0]) && "123456789".Contains(x[1]) || x.Length == 1 && "hc".Contains(x))) {
         yield return null;

         for (int i = 0; i < parts.Length; i++) {
            var part = parts[i];

            if (part.Length == 2) {
               var buttonNumToPress = part[0] == 't' ? 0 : 1;
               var numPresses = int.Parse(part[1].ToString());

               for (int j = 0; j < numPresses; j++) {
                  yield return "trycancel";
                  ButtonPressed(buttonNumToPress);
                  yield return new WaitForSeconds(.1f);
                  ButtonDepressed(buttonNumToPress);
                  yield return new WaitForSeconds(.1f);
               }
            }
            else {
               if (part == "c") {
                  for (int j = 0; j < 9; j++) {
                     yield return "trycancel";
                     ButtonPressed(1);
                     yield return new WaitForSeconds(.1f);
                     ButtonDepressed(1);
                     yield return new WaitForSeconds(.4f);
                  }
               }
               else {
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

   IEnumerator TwitchHandleForcedSolve () {
      if (randomSequenceTop.Count() == 0) {
         while (!(currentState[0] + "").Equals(redNumber)) {
            bottomButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
            bottomButton.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
         }
         topButton.OnInteract();
         yield return new WaitForSeconds(0.1f);
         topButton.OnInteractEnded();
         yield return new WaitForSeconds(0.1f);
      }
      for (int i = amountCorrect; i < Settings.Amount; i++) {
         if ((currentState[0] + "").Equals(redNumber)) {
            bottomButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
            bottomButton.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
         }
         while (!(currentState[1] + "").Equals(randomSequenceBottom[i])) {
            topButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
            topButton.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
         }
         while (!(currentState[0] + "").Equals(randomSequenceTop[i])) {
            bottomButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
            bottomButton.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
         }
         int rando = Random.Range(0, 2);
         if (rando == 0 && !(currentState[0] + "").Equals(redNumber)) {
            topButton.OnInteract();
            while (!holding) { yield return true; }
            topButton.OnInteractEnded();
         }
         else {
            bottomButton.OnInteract();
            while (!holding) { yield return true; }
            bottomButton.OnInteractEnded();
         }
         yield return new WaitForSeconds(0.1f);
      }
      while (!okayExishThisModuleHasSolved) { yield return true; }
   }
}
