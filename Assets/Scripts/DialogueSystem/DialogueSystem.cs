using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] public GameObject buttonPrefab;
    [SerializeField] public int maxCapacityOnScreen;
    [SerializeField] public GameObject answerContent;

    public bool IsOccupied { get; set; }
}