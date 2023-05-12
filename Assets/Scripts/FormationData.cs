using System;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;

public class FormationData : MonoBehaviour
{
    [Serializable]
    public class Placeholder
    {
        [FormerlySerializedAs("regiment")] public UnitType unitType;
    }

    [Serializable]
    public class Row
    {
        public Placeholder[] placeholders;
    }

    public Row[] rows;
    public float betweenRowsDistance;

}
