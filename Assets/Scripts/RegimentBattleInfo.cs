using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class RegimentBattleInfo : MonoBehaviour
    {
        public UnitBattleInfo[] unitsInRegiment;
        public string regimentName;
        public Vector3 RegimentPosition => transform.position;
        public bool isEnemy;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isEnemy ? Color.red : Color.blue;
            
            Gizmos.DrawWireSphere(RegimentPosition, 2f);
        }
    }
}