using System;
using UnityEngine;

namespace DefaultNamespace
{
    [Serializable]
    public class UnitBattleInfo
    {
        public UnitType type;
        public Vector2Int boundsRect;
        public bool isEnemy;
        
        public string regimentName;
    }
}