using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class RegimentInfo
    {
        public List<UnitBattleInfo> unitsInRegiment;
        public Vector3 regimentPosition;
        public bool isEnemy;

        public RegimentInfo(UnitBattleInfo[] unitsInRegiment, Vector3 regimentPosition, bool isEnemy)
        {
            this.unitsInRegiment = new List<UnitBattleInfo>(unitsInRegiment.Length);
            this.unitsInRegiment.AddRange(unitsInRegiment);
            
            this.regimentPosition = regimentPosition;
            this.isEnemy = isEnemy;
        }
    }
}