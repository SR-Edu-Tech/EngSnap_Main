using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    [CreateAssetMenu(fileName = "NewUnitData_Masters_Phonics", menuName = "Masters Phonics/Data/Unit Data")]
    public class U1_SA_UnitDataSO_Masters_Phonics : ScriptableObject
    {
        [Header("Unit Information")]
        public string unitID; // e.g., "U1"
        public string unitName;
        public Sprite badgeIcon;

        [Header("Unit Flow (Sequential)")]
        [Tooltip("List of activities in the order they should be played")]
        public List<U1_SA_ActivityDataSO_Masters_Phonics> activities;
    }
}
