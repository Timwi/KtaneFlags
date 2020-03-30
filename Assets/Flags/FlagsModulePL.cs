﻿using KMBombInfoExtensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class Panstwa {

    public string NazwaKraju { get; set; }
    public string Kontynent { get; set; }
    public string Stolica { get; set; }
    public string Waluta { get; set; }
    public string KrajISO { get; set; }
    public int KrajKod { get; set; }
    public int KrajID { get; set; }

}

public class FlagsModulePL : MonoBehaviour {

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    public KMAudio sound;
    public KMBombInfo bombInfo;
    public KMBombModule module;

    public KMSelectable leftButton;
    public KMSelectable rightButton;
    public KMSelectable submitButton;

    public MeshRenderer mainFlag;
    public MeshRenderer[] choiceFlags;
    public MeshRenderer[] scrollDots;
    public TextMesh numberDisplay;

    public GameObject scrollsObject;
    public GameObject flagsObject;

    public Material[] dotsMaterial;
    public Texture[] flags;
    public TextAsset jsonInfo;

    private int number;
    private int position = 0;
    private bool canInteract = true;
    private Panstwa[] order;
    private List<Panstwa> PanstwaInfo;

    private Panstwa mainPanstwa;
    private List<Panstwa> countries = new List<Panstwa>();
    private List<int> numbers = new List<int>(Enumerable.Range(0, 36).ToArray());

    private Panstwa[] getOrder(Panstwa main, List<Panstwa> list) {
        // BUT if there is an unlit BOB, and the serial number contains any characters from the phrase "WHITE FLAG"
        // while France is in the 7 flags, ignore all above instuctions and submit France four times.
        if (bombInfo.GetSerialNumber().Any("WHITEFLAG".Contains) && bombInfo.IsIndicatorOff(KMBI.KnownIndicatorLabel.BOB)
            && list.Contains(PanstwaInfo[13]))
            return Enumerable.Repeat(PanstwaInfo[13], 7).ToArray();

        // If the main Panstwa is in North America, and there are no lit indicators,
        // sort the 7 flags by their Panstwa name in alphabetical order.
        if (main.Kontynent == "North America" && bombInfo.GetOnIndicators().Count() == 0)
            return list.OrderBy(x => x.NazwaKraju).ToArray();

        // Otherwise, if the main Panstwa's dial code is higher than 100, and there is an RJ-45 port,
        // sort the 7 flags by their dial code in numerical order.
        if (main.KrajKod > 100 && bombInfo.IsPortPresent(KMBI.KnownPortType.RJ45))
            return list.OrderBy(x => x.KrajKod).ThenBy(x => x.NazwaKraju).ToArray();

        // Otherwise, if the main Panstwa's name contains the last letter of its Waluta,
        // sort the 7 flags by their ISO code in alphabetical order.
        if (main.NazwaKraju.ToUpperInvariant().Contains(main.Waluta[2]))
            return list.OrderBy(x => x.KrajISO).ToArray();

        // Otherwise, if the main Panstwa's Stolica has more than 9 letters,
        // sort the 7 flags by their Stolica in alphabetical order.
        if (main.Stolica.Count(char.IsLetter) > 9)
            return list.OrderBy(x => x.Stolica).ToArray();

        // Otherwise, if the main Panstwa is in Europe, and its Waluta is not EUR,
        // sort the 7 flags by their Kontynent in alphabetical order.
        if (main.Kontynent == "Europe" && main.Waluta != "EUR")
            return list.OrderBy(x => x.Kontynent).ThenBy(x => x.NazwaKraju).ToArray();

        // Otherwise, if there is a Panstwa in the 7 flags with the same Kontynent as the main Panstwa's,
        // sort the 7 flags by their Waluta in alphabetical order.
        if (list.Find(x => x.Kontynent == main.Kontynent) != null)
            return list.OrderBy(x => x.Waluta).ThenBy(x => x.NazwaKraju).ToArray();

        // Otherwise, sort the 7 flags by their Panstwa name in alphabetical order.
        return list.OrderBy(x => x.NazwaKraju).ToArray();
    }

    private string getRule(Panstwa main, List<Panstwa> list) {
        // BUT if there is an unlit BOB, and the serial number contains any characters from the phrase "WHITE FLAG"
        // while France is in the 7 flags, ignore all above instuctions and submit France four times.
        if (bombInfo.GetSerialNumber().Any("WHITEFLAG".Contains) && bombInfo.IsIndicatorOff(KMBI.KnownIndicatorLabel.BOB)
            && list.Contains(PanstwaInfo[13]))
            return "White Flag (Unicorn)";

        // If the main Panstwa is in North America, and there are no lit indicators,
        // sort the 7 flags by their Panstwa name in alphabetical order.
        if (main.Kontynent == "North America" && bombInfo.GetOnIndicators().Count() == 0)
            return "1st Condition (Panstwa Name)";

        // Otherwise, if the main Panstwa's dial code is higher than 100, and there is an RJ-45 port,
        // sort the 7 flags by their dial code in numerical order.
        if (main.KrajKod > 100 && bombInfo.IsPortPresent(KMBI.KnownPortType.RJ45))
            return "2nd Condition (Dial Code)";

        // Otherwise, if the main Panstwa's name contains the last letter of its Waluta,
        // sort the 7 flags by their ISO code in alphabetical order.
        if (main.NazwaKraju.ToUpperInvariant().Contains(main.Waluta[2]))
            return "3rd Condition (ISO Code)";

        // Otherwise, if the main Panstwa's Stolica has more than 9 letters,
        // sort the 7 flags by their Stolica in alphabetical order.
        if (main.Stolica.Count(char.IsLetter) > 9)
            return "4th Condition (Stolica)";

        // Otherwise, if the main Panstwa is in Europe, and its Waluta is not EUR,
        // sort the 7 flags by their Kontynent in alphabetical order.
        if (main.Kontynent == "Europe" && main.Waluta != "EUR")
            return "5th Condition (Kontynent)";

        // Otherwise, if there is a Panstwa in the 7 flags with the same Kontynent as the main Panstwa's,
        // sort the 7 flags by their Waluta in alphabetical order.
        if (list.Find(x => x.Kontynent == main.Kontynent) != null)
            return "6th Condition (Waluta)";

        // Otherwise, sort the 7 flags by their Panstwa name in alphabetical order.
        return "Last Rule (Panstwa Name)";
    }

    void Start() {
        _moduleId = _moduleIdCounter++;

        PanstwaInfo = JsonConvert.DeserializeObject<List<Panstwa>>(jsonInfo.ToString());

        int main = numbers[Random.Range(0, numbers.Count)];
        mainPanstwa = PanstwaInfo[main];
        mainPanstwa.KrajID = main;
        numbers.Remove(main);

        for (int i = 0; i < 7; i++) {
            int id = numbers[Random.Range(0, numbers.Count)];
            countries.Add(PanstwaInfo[id]);
            countries[i].KrajID = id;
            numbers.Remove(id);
        }

        order = getOrder(mainPanstwa, countries);
        number = Random.Range(1, 8);

        mainFlag.material.mainTexture = flags[mainPanstwa.KrajID];
        updateScreen();

        Debug.LogFormat("[Flags PL #{0}] Main Display: {1}", _moduleId, mainPanstwa.NazwaKraju);
        Debug.LogFormat("[Flags PL #{0}] Flag List: {1}", _moduleId, string.Join(", ", countries.Select(x => x.NazwaKraju).ToArray()));
        Debug.LogFormat("[Flags PL #{0}] Order Rule: {1}", _moduleId, getRule(mainPanstwa, countries));
        Debug.LogFormat("[Flags PL #{0}] Order: {1}", _moduleId, string.Join(", ", order.Select(x => x.NazwaKraju).ToArray()));
        Debug.LogFormat("[Flags PL #{0}] Answer: {1} (#{2})", _moduleId, order[number - 1].NazwaKraju, number);
    }

    void Awake() {
        leftButton.OnInteract += onLeft;
        rightButton.OnInteract += onRight;
        submitButton.OnInteract += onSubmit;
    }

    private void updateScreen() {
        for (int i = 0; i < 7; i++)
            scrollDots[i].material = dotsMaterial[0];
        scrollDots[position].material = dotsMaterial[1];

        int[] positions = new int[] {
            position - 2 < 0 ? position + 5 : position - 2,
            position - 1 < 0 ? position + 6 : position - 1,
            position,
            position + 1 > 6 ? position - 6 : position + 1,
            position + 2 > 6 ? position - 5 : position + 2
        };

        numberDisplay.text = number.ToString();

        for (int i = 0; i < 5; i++)
            choiceFlags[i].material.mainTexture = flags[countries[positions[i]].KrajID];
    }

    private bool onLeft() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, leftButton.transform);
        leftButton.AddInteractionPunch(0.5f);

        if (!canInteract)
            return false;

        position = position == 0 ? 6 : position - 1;
        updateScreen();

        return false;
    }
    	
    private bool onRight() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, rightButton.transform);
        leftButton.AddInteractionPunch(0.5f);

        if (!canInteract)
            return false;

        position = position == 6 ? 0 : position + 1;
        updateScreen();

        return false;
    }

    private bool onSubmit() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
        submitButton.AddInteractionPunch(0.5f);

        if (!canInteract)
            return false;

        bool correct = countries[position] == order[number - 1];

        Debug.LogFormat("[Flags PL #{0}] Submitted {1}, expected {2}. {3}.", _moduleId,
            countries[position].NazwaKraju, order[number - 1].NazwaKraju, correct ? "Correct" : "Strike");

        if (correct) {
            module.HandlePass();
            canInteract = false;

            Destroy(scrollsObject);
            Destroy(flagsObject);
            Destroy(mainFlag);
            numberDisplay.text = "";

            sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, submitButton.transform);

            return false;
        }

        module.HandleStrike();
        return false;
    }

    #pragma warning disable 414
    private string TwitchHelpMessage = "Cycle the flags with !{0} cycle. Move using !{0} left/right. " + 
        "Set to index 3 with !{0} set 3. Set to Canada with !{0} set Canada. Submit the current flag with !{0} submit. " +
        "Submit Canada with !{0} submit Canada.";
    #pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command) {
        command = command.ToUpperInvariant().Trim();

        if (command == "SUBMIT") {
            onSubmit();
            yield return null;
        }
        
        else if (command == "LEFT") {
            onLeft();
            yield return null;
        }
        
        else if (command == "RIGHT") {
            onRight();
            yield return null;
        }
        
        else if (command == "CYCLE")
            for (int i = 0; i < 7; i++) {
                yield return new WaitForSeconds(0.75f);
                onRight();
                yield return new WaitForSeconds(0.75f);
            }

        else if (command.Length > 4 && command.Substring(0, 4) == "SET " || command.Length > 7 && command.Substring(0, 7) == "SUBMIT ") {
            string args = command[1] == 'E' ? command.Substring(4) : command.Substring(7);

            if (args.Length == 1 && "1234567".Contains(args)) {
                int target = int.Parse(args);

                while (target != position + 1) {
                    onRight();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            else if (PanstwaInfo.Find(x => x.NazwaKraju.ToUpperInvariant() == args) != null) {
                int cycle = 0;

                while (countries[position].NazwaKraju.ToUpperInvariant() != args && cycle++ < 7) {
                    onRight();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            if (command[1] == 'U')
                onSubmit();
        }
    }
}