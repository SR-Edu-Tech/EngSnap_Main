using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "LessonSO", menuName = "ScriptableObjects/LessonSO")]
public class Masters_LessonSO : ScriptableObject {


    [SerializeField]
    private Masters_Unit unit;
    [SerializeField]
    private Masters_Topic topic;
    [SerializeField]
    private GameObject lessonPrefab;


    public Masters_Unit Unit => unit;
    public Masters_Topic Topic => topic;
    public GameObject LessonPrefab => lessonPrefab;


}
