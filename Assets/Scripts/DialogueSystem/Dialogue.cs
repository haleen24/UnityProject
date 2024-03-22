using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;


public class Dialogue : MonoBehaviour
{
    [SerializeField] private GameObject dialogueSystemGameObject;
    [SerializeField] private GameObject dialogueBoxGameObject;
    [SerializeField] private GameObject dialogueAnswerGameObject;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float textDelay;
    [SerializeField] private TextAsset objectDialogueSourcePath;
    [SerializeField] private List<int> startIds;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject answerContent;
    [SerializeField] private string interlocutor;

    [SerializeField] private List<DialogueScene> dialogueScenes;

    private DialogueSystem _dialogueSystem;
    private float _textDelay;
    private int _maxCapacityOnScreen;
    private readonly Queue<GameObject> _choiceQueue = new Queue<GameObject>();

    public void Awake()
    {
        dialogueSystemGameObject =
            dialogueSystemGameObject ? dialogueSystemGameObject : GameObject.Find("DialogueSystem");
        dialogueBoxGameObject = dialogueBoxGameObject ? dialogueBoxGameObject : GameObject.Find("DialogueBox");
        dialogueAnswerGameObject = dialogueAnswerGameObject ? dialogueAnswerGameObject : GameObject.Find("AnswerBox");
        if (!dialogueSystemGameObject || !dialogueBoxGameObject || !dialogueAnswerGameObject)
        {
            Debug.Log("Dialog system can not be found");
            Application.Quit();
        }
        else
        {
            var dialogueSystem = dialogueSystemGameObject.GetComponent<DialogueSystem>();
            dialogueText = dialogueText
                ? dialogueText
                : dialogueSystemGameObject.GetComponentInChildren<TextMeshProUGUI>();
            _maxCapacityOnScreen = dialogueSystem.maxCapacityOnScreen;
            buttonPrefab = buttonPrefab
                ? buttonPrefab
                : dialogueSystem.buttonPrefab;
            answerContent = dialogueSystem.answerContent;
        }

        var listOfDialogues =
            JsonUtility.FromJson<JsonDialogueData>(objectDialogueSourcePath.text) ?? new JsonDialogueData();

        dialogueScenes = listOfDialogues.scenes;
        dialogueText.text = "";
        _dialogueSystem = dialogueSystemGameObject.GetComponent<DialogueSystem>();
    }

    public void Start()
    {
        dialogueSystemGameObject.SetActive(false);
    }

    public void StartDialogue()
    {
        if (startIds.Count == 0)
        {
            return;
        }

        lock (dialogueSystemGameObject)
        {
            if (_dialogueSystem.IsOccupied)
            {
                return;
            }

            _dialogueSystem.IsOccupied = true;
        }

        int currentScene = startIds[0];
        startIds.RemoveAt(0);
        dialogueSystemGameObject.SetActive(true);
        dialogueAnswerGameObject.SetActive(false);
        StartCoroutine(TypeScene(currentScene));
    }

    IEnumerable<string> Split(string str)
    {
        int i = 0;
        while (i < str.Length)
        {
            yield return str.Substring(i, Math.Min(_maxCapacityOnScreen, str.Length - i));
            i += _maxCapacityOnScreen;
        }
    }

    private IEnumerator TypeScene(int sceneId)
    {
        _textDelay = textDelay;
        var scene = dialogueScenes[sceneId];
        var text = scene.text;
        var i = 0;
        foreach (var line in Split(text))
        {
            _textDelay = textDelay;
            yield return new WaitForSeconds(_textDelay);
            dialogueText.text = $"{(scene.talker ? "YOU" : interlocutor)}:\n";
            foreach (var character in line)
            {
                dialogueText.text += character;
                yield return new WaitForSeconds(_textDelay);
            }

            i += _maxCapacityOnScreen;
            if (i < text.Length)
            {
                dialogueText.text += "..";
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }

        if (dialogueScenes[sceneId].selectionIds.Count == 1)
        {
            yield return StartCoroutine(TypeScene(dialogueScenes[sceneId].selectionIds[0]));
            dialogueAnswerGameObject.SetActive(false);
        }
        else if (dialogueScenes[sceneId].selectionIds.Count > 1)
        {
            StartCoroutine(AddButtons(sceneId));
        }
        else
        {
            dialogueSystemGameObject.SetActive(false);
            dialogueText.text = "";
            _dialogueSystem.IsOccupied = false;
        }
    }

    private void DeleteButtons()
    {
        dialogueAnswerGameObject.SetActive(false);
        while (_choiceQueue.Count != 0)
        {
            Destroy(_choiceQueue.Dequeue());
        }
    }

    private void AddButton(DialogueScene choiceSlide, int id)
    {
        var buttonObject = Instantiate(buttonPrefab, answerContent.transform, true);
        var buttonComponent = buttonObject.GetComponent<Button>();
        _choiceQueue.Enqueue(buttonObject);
        buttonObject.GetComponent<TextMeshProUGUI>().text = choiceSlide.text;
        buttonComponent.onClick.AddListener(() =>
        {
            lock (dialogueSystemGameObject)
            {
                StartCoroutine(TypeScene(id));
                DeleteButtons();
            }
        });
    }

    private IEnumerator AddButtons(int sceneId)
    {
        foreach (int id in dialogueScenes[sceneId].selectionIds)
        {
            if (id < 0)
            {
                continue;
            }

            AddButton(dialogueScenes[id], id);
            yield return new WaitForFixedUpdate();
        }

        dialogueAnswerGameObject.SetActive(true);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _textDelay = 0;
        }
    }
}