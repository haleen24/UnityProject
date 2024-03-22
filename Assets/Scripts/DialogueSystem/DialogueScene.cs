using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueScene
{
    [SerializeField] public int id;
    [SerializeField] public string text;
    [SerializeField] public List<int> selectionIds;
    [SerializeField] public bool talker;
}