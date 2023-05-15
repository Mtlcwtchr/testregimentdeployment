using System.Collections.Generic;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FrontlaneDeployer : MonoBehaviour
{
    public BattleUnitData battleUnitData;

    public FormationData formationData;

    private List<RegimentInfo> regimentsToPlace = new List<RegimentInfo>();

    private List<UnitPlaceholder> enemyPlaceholders = new List<UnitPlaceholder>();
    private List<UnitPlaceholder> allyPlaceholders = new List<UnitPlaceholder>();

    public bool enemyToTheLeft = false;

    public float maxFillDistance = 10f;

    private List<Link> links = new List<Link>();

    public void FillPlaceholders()
    {
        regimentsToPlace.Clear();
        
        links.Clear();
        enemyPlaceholders.Clear();
        allyPlaceholders.Clear();
        
        for (var i = 0; i < battleUnitData.regimentsInBattle.Length; i++)
        {
            var data = battleUnitData.regimentsInBattle[i];
            for (var j = 0; j < data.unitsInRegiment.Length; j++)
            {
                data.unitsInRegiment[j].regimentName = data.regimentName;
            }
            
            var regimentInfo = new RegimentInfo(data.unitsInRegiment, data.RegimentPosition, data.isEnemy);
            regimentsToPlace.Add(regimentInfo);
        }

        GetLinks();

        for (var i = 0; i < links.Count; i++)
        {
            FillLinkWithEnemyPlaceholders(links[i]);
            FillLinkWithAllyPlaceholders(links[i]);

            FillLinkPlaceholdersWithBestEnemyRegiments(links[i]);
            FillLinkPlaceholdersWithBestAllyRegiments(links[i]);
        }

        for (var i = 0; i < links.Count; i++)
        {
            FillEmptyPlaceholdersWithBestEnemyRegiments(links[i]);
            FillEmptyPlaceholdersWithBestAllyRegiments(links[i]);
        }

        PutOutOfFormationsEnemyRegiments();
        PutOutOfFormationsAllyRegiments();
    }
    
    private void GetLinks()
    {
        float longestLinkLength = 0f;
        for (var i = 0; i < formationData.rows.Length; i++)
        {
            var row = formationData.rows[i];
            var linkLength = 0f;
            for (var j = 0; j < row.placeholders.Length; j++)
            {
                var placeholder = row.placeholders[j];
                var regimentLongestLength = 0f;
                for (var k = 0; k < battleUnitData.regimentsInBattle.Length; k++)
                {
                    var regiment = battleUnitData.regimentsInBattle[k];

                    for (var p = 0; p < regiment.unitsInRegiment.Length; p++)
                    {
                        var unit = regiment.unitsInRegiment[p];
                    
                        if(unit.type != placeholder.unitType) 
                            continue;
                    
                        const float regimentSizeFactor = 1.3f;
                    
                        if (unit.boundsRect.x * regimentSizeFactor <= regimentLongestLength)
                            continue;
                        
                        regimentLongestLength = unit.boundsRect.x * regimentSizeFactor;
                    }
                }

                linkLength += regimentLongestLength;
            }

            if (linkLength > longestLinkLength)
            {
                longestLinkLength = linkLength;
            }
        }

        var frontlaneAnchors = battleUnitData.frontlaneAnchors;

        for (int i = 0; i < frontlaneAnchors.Length - 1; ++i)
        {
            var beg = frontlaneAnchors[i].position;
            var end = frontlaneAnchors[i + 1].position;

            var linePartLength = (beg-end).magnitude;
            var linksInPart = linePartLength / longestLinkLength;
            if (linksInPart <= 1.9f)
            {
                var link = new Link()
                {
                    start = beg,
                    end = end
                };
                links.Add(link);
            }
            else
            {
                for (float j = 0; j <= linksInPart - 1; ++j)
                {
                    var link = new Link()
                    {
                        start = beg + ((end - beg) * j / linksInPart),
                        end = beg + ((end - beg) * (linksInPart - (j + 1) < 1 ? 1 : (j + 1) / linksInPart))
                    };
                    links.Add(link);
                }
            }
        }
    }

    private void FillLinkWithEnemyPlaceholders(Link link) 
    {
        var linkDir = link.end - link.start;
        var linkCenter = (link.end + link.start) / 2f;
        var linkDirNormal = linkDir.normalized;
        var linkLeftNormal = new Vector3(-linkDir.z, linkDir.y, linkDir.x).normalized * (enemyToTheLeft ? -1 : 1);

        for (var i = 0; i < formationData.rows.Length; i++)
        {
            var row = formationData.rows[i];
            var rowOffset = linkLeftNormal * ((i+1) * formationData.betweenRowsDistance);
            var rowCenter = linkCenter + rowOffset;
            var placeholderOffsetLength = linkDir.magnitude / row.placeholders.Length;
            
            for (var j = 0; j < row.placeholders.Length; j++)
            {
                var placeholder = row.placeholders[j];

                Vector3 pos;
                if (row.placeholders.Length % 2 > 0)
                {
                    pos = rowCenter + ((j - (row.placeholders.Length/2)) * placeholderOffsetLength) * linkDirNormal;
                }
                else
                {
                    pos = rowCenter + ((j < row.placeholders.Length/2 ? -(j+1) : (j+1)-row.placeholders.Length/2) * placeholderOffsetLength * 0.5f) * linkDirNormal;
                }
                
                var rgPlaceholder = new UnitPlaceholder()
                {
                    position = pos,
                    UnitType = placeholder.unitType,
                    size = new Vector3(1, 1, 1),
                    rotation = Quaternion.LookRotation(linkDirNormal, Vector3.up),
                    enemy = false,
                };

                link.enemyPlaceholders.Add(rgPlaceholder);
                enemyPlaceholders.Add(rgPlaceholder);
            }
        }
    }
    
    private void FillLinkWithAllyPlaceholders(Link link)
    {
        var linkDir = link.end - link.start;
        var linkCenter = (link.end + link.start) / 2f;
        var linkDirNormal = linkDir.normalized;
        var linkLeftNormal = new Vector3(-linkDir.z, linkDir.y, linkDir.x).normalized * (enemyToTheLeft ? 1 : -1);

        for (var i = 0; i < formationData.rows.Length; i++)
        {
            var row = formationData.rows[i];
            var rowOffset = linkLeftNormal * ((i+1) * formationData.betweenRowsDistance);
            var rowCenter = linkCenter + rowOffset;
            var placeholderOffsetLength = linkDir.magnitude / row.placeholders.Length;
            
            for (var j = 0; j < row.placeholders.Length; j++)
            {
                var placeholder = row.placeholders[j];

                Vector3 pos;
                if (row.placeholders.Length % 2 > 0)
                {
                    pos = rowCenter + ((j - (row.placeholders.Length/2)) * placeholderOffsetLength) * linkDirNormal;
                }
                else
                {
                    pos = rowCenter + ((j < row.placeholders.Length/2 ? -(j+1) : (j+1)-row.placeholders.Length/2) * placeholderOffsetLength * 0.5f) * linkDirNormal;
                }
                
                var rgPlaceholder = new UnitPlaceholder()
                {
                    position = pos,
                    UnitType = placeholder.unitType,
                    size = new Vector3(1, 1, 1),
                    rotation = Quaternion.LookRotation(linkDirNormal, Vector3.up),
                    enemy = true,
                };

                link.allyPlaceholders.Add(rgPlaceholder);
                allyPlaceholders.Add(rgPlaceholder);
            }
        }
    }

    private void FillLinkPlaceholdersWithBestEnemyRegiments(Link link)
    {
        if (regimentsToPlace.Count < 1)
            return;
        
        var linkCenter = (link.end + link.start) / 2f;

        var regiment = regimentsToPlace[0];
        for (var i = 0; i < regimentsToPlace.Count; i++)
        {
            var candidate = regimentsToPlace[i];
            
            if (!candidate.isEnemy)
                continue;
            
            if(candidate.unitsInRegiment.Count < 1)
                continue;

            if (!regiment.isEnemy ||
                (candidate.regimentPosition - linkCenter).magnitude < (regiment.regimentPosition - linkCenter).magnitude)
            {
                regiment = candidate;
            }
        }

        if (regiment.unitsInRegiment.Count < 1)
            return;

        if (!regiment.isEnemy)
            return;

        for (var i = 0; i < link.enemyPlaceholders.Count; ++i)
        {
            var placeholder = link.enemyPlaceholders[i];

            if (link.filledPlaceholders.Contains(placeholder))
                continue;

            if (regiment.unitsInRegiment.Count < 1)
                return;

            for (var j = 0; j < regiment.unitsInRegiment.Count; ++j)
            {
                var unit = regiment.unitsInRegiment[j];
                if (unit.type != placeholder.UnitType)
                    continue;

                // imitate fill unit into placeholder
                placeholder.size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x);
                placeholder.actualUnit = unit;

                regiment.unitsInRegiment.RemoveAt(j);
                if (regiment.unitsInRegiment.Count < 1)
                {
                    regimentsToPlace.Remove(regiment);
                }

                link.filledPlaceholders.Add(placeholder);
                break;
            }
        }
    }

    private void FillLinkPlaceholdersWithBestAllyRegiments(Link link)
    {
        if (regimentsToPlace.Count < 1)
            return;

        var linkCenter = (link.end + link.start) / 2f;

        var regiment = regimentsToPlace[0];
        for (var i = 0; i < regimentsToPlace.Count; i++)
        {
            var candidate = regimentsToPlace[i];

            if (candidate.isEnemy)
                continue;

            if (candidate.unitsInRegiment.Count < 1)
                continue;

            if (regiment.isEnemy ||
                (candidate.regimentPosition - linkCenter).magnitude < (regiment.regimentPosition - linkCenter).magnitude)
            {
                regiment = candidate;
            }
        }

        if (regiment.unitsInRegiment.Count < 1)
            return;

        if (regiment.isEnemy)
            return;

        for (var i = 0; i < link.allyPlaceholders.Count; ++i)
        {
            var placeholder = link.allyPlaceholders[i];

            if (link.filledPlaceholders.Contains(placeholder))
                continue;

            if (regiment.unitsInRegiment.Count < 1)
                return;

            for (var j = 0; j < regiment.unitsInRegiment.Count; ++j)
            {
                var unit = regiment.unitsInRegiment[j];
                if (unit.type != placeholder.UnitType)
                    continue;

                // imitate fill unit into placeholder
                placeholder.size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x);
                placeholder.actualUnit = unit;

                regiment.unitsInRegiment.RemoveAt(j);
                if(regiment.unitsInRegiment.Count < 1)
                {
                    regimentsToPlace.Remove(regiment);
                }

                link.filledPlaceholders.Add(placeholder);
                break;
            }
        }
    }

    private void FillEmptyPlaceholdersWithBestEnemyRegiments(Link link)
    {
        if (regimentsToPlace.Count < 1)
            return;

        var linkCenter = (link.end + link.start) / 2f;

        for (int i = 0; i < link.enemyPlaceholders.Count; ++i)
        {
            var placeholder = link.enemyPlaceholders[i];
            if (link.filledPlaceholders.Contains(placeholder))
                continue;

            //find best regiment to fill
            if (regimentsToPlace.Count < 1)
                return;

            var checkingRegiments = new List<RegimentInfo>();
            checkingRegiments.AddRange(regimentsToPlace);

            for(int j=0; j<checkingRegiments.Count; ++j)
            {
                var reg = checkingRegiments[j];

                if(!reg.isEnemy)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                if(reg.unitsInRegiment.Count < 1)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                if ((reg.regimentPosition - linkCenter).magnitude > maxFillDistance)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                for (int k=j; k<checkingRegiments.Count; ++k)
                {
                    var cand = checkingRegiments[k];

                    if (!cand.isEnemy)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if (cand.unitsInRegiment.Count < 1)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if ((cand.regimentPosition - linkCenter).magnitude > maxFillDistance)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if ((reg.regimentPosition - linkCenter).magnitude > (cand.regimentPosition - linkCenter).magnitude)
                    {
                        checkingRegiments[j] = cand;
                        checkingRegiments[k] = reg;
                    }
                }
            }

            for(int j=0; j<checkingRegiments.Count; ++j)
            {
                var regiment = checkingRegiments[j];

                if (regiment.unitsInRegiment.Count < 1)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                for(int k=0; k<regiment.unitsInRegiment.Count; ++k)
                {
                    var unit = regiment.unitsInRegiment[k];

                    if (unit.type != placeholder.UnitType)
                        continue;

                    placeholder.size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x);
                    placeholder.actualUnit = unit;

                    regiment.unitsInRegiment.RemoveAt(k);
                    if (regiment.unitsInRegiment.Count < 1)
                    {
                        regimentsToPlace.Remove(regiment);
                    }

                    link.filledPlaceholders.Add(placeholder);
                    break;
                }
            }
        }
    }

    private void FillEmptyPlaceholdersWithBestAllyRegiments(Link link)
    {
        if (regimentsToPlace.Count < 1)
            return;

        var linkCenter = (link.end + link.start) / 2f;

        for (int i = 0; i < link.allyPlaceholders.Count; ++i)
        {
            var placeholder = link.allyPlaceholders[i];
            if (link.filledPlaceholders.Contains(placeholder))
                continue;

            //find best regiment to fill
            if (regimentsToPlace.Count < 1)
                return;

            var checkingRegiments = new List<RegimentInfo>();
            checkingRegiments.AddRange(regimentsToPlace);

            for (int j = 0; j < checkingRegiments.Count; ++j)
            {
                var reg = checkingRegiments[j];

                if (reg.isEnemy)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                if (reg.unitsInRegiment.Count < 1)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                if ((reg.regimentPosition - linkCenter).magnitude > maxFillDistance)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                for (int k = j; k < checkingRegiments.Count; ++k)
                {
                    var cand = checkingRegiments[k];

                    if (cand.isEnemy)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if (cand.unitsInRegiment.Count < 1)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if ((cand.regimentPosition - linkCenter).magnitude > maxFillDistance)
                    {
                        checkingRegiments.RemoveAt(k--);
                        continue;
                    }

                    if ((reg.regimentPosition - linkCenter).magnitude > (cand.regimentPosition - linkCenter).magnitude)
                    {
                        checkingRegiments[j] = cand;
                        checkingRegiments[k] = reg;
                    }
                }
            }

            for (int j = 0; j < checkingRegiments.Count; ++j)
            {
                var regiment = checkingRegiments[j];

                if (regiment.unitsInRegiment.Count < 1)
                {
                    checkingRegiments.RemoveAt(j--);
                    continue;
                }

                for (int k = 0; k < regiment.unitsInRegiment.Count; ++k)
                {
                    var unit = regiment.unitsInRegiment[k];

                    if (unit.type != placeholder.UnitType)
                        continue;

                    placeholder.size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x);
                    placeholder.actualUnit = unit;

                    regiment.unitsInRegiment.RemoveAt(k);
                    if (regiment.unitsInRegiment.Count < 1)
                    {
                        regimentsToPlace.Remove(regiment);
                    }

                    link.filledPlaceholders.Add(placeholder);
                    break;
                }
            }
        }
    }

    private void PutOutOfFormationsEnemyRegiments()
    {
        if (regimentsToPlace.Count < 1)
            return;

        var placeholders = new List<UnitPlaceholder>();
        for(int i=0; i<regimentsToPlace.Count;++i)
        {
            var regiment = regimentsToPlace[i];

            if (regiment.unitsInRegiment.Count < 1)
                continue;

            if (!regiment.isEnemy)
                continue;

            var link = links[0];
            var linkCenter = (link.start + link.end) / 2f;

            for (int j=0; j<links.Count; ++j)
            {
                var candidate = links[j];
                var candidateCenter = (candidate.start + candidate.end) / 2f;

                if((regiment.regimentPosition - linkCenter).magnitude > (regiment.regimentPosition - candidateCenter).magnitude)
                {
                    link = candidate;
                    linkCenter = candidateCenter;
                }
            }

            var linkDir = link.end - link.start;
            var linkDirNormal = linkDir.normalized;
            var linkLeftNormal = new Vector3(-linkDir.z, linkDir.y, linkDir.x).normalized * (enemyToTheLeft ? -1 : 1);

            var placeholdersInRow = 0;
            var placeholdersRowOffset = 0;
            for (int k = 0; k < regiment.unitsInRegiment.Count; ++k)
            {
                var unit = regiment.unitsInRegiment[k];

                var rowStart = link.start + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));
                var rowEnd = link.end + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));

                var pos = rowStart + ((placeholdersInRow+1) * unit.boundsRect.x * 1.6f) * linkDirNormal;

                if ((pos - rowStart).magnitude > (rowEnd - rowStart).magnitude)
                {
                    placeholdersRowOffset++;
                    placeholdersInRow = 0;
                    rowStart = link.start + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));
                    pos = rowStart + ((placeholdersInRow + 1) * unit.boundsRect.x * 1.6f) * linkDirNormal;
                }

                var rgPlaceholder = new UnitPlaceholder()
                {
                    position = pos,
                    UnitType = unit.type,
                    size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x),
                    rotation = Quaternion.LookRotation(linkDirNormal, Vector3.up),
                    actualUnit = unit,
                    enemy = true,
                };

                link.enemyPlaceholders.Add(rgPlaceholder);
                link.filledPlaceholders.Add(rgPlaceholder);
                enemyPlaceholders.Add(rgPlaceholder);

                placeholdersInRow++;
            }
        }
    }

    private void PutOutOfFormationsAllyRegiments()
    {
        if (regimentsToPlace.Count < 1)
            return;

        var placeholders = new List<UnitPlaceholder>();
        for (int i = 0; i < regimentsToPlace.Count; ++i)
        {
            var regiment = regimentsToPlace[i];

            if (regiment.unitsInRegiment.Count < 1)
                continue;

            if (regiment.isEnemy)
                continue;

            var link = links[0];
            var linkCenter = (link.start + link.end) / 2f;

            for (int j = 0; j < links.Count; ++j)
            {
                var candidate = links[j];
                var candidateCenter = (candidate.start + candidate.end) / 2f;

                if ((regiment.regimentPosition - linkCenter).magnitude > (regiment.regimentPosition - candidateCenter).magnitude)
                {
                    link = candidate;
                    linkCenter = candidateCenter;
                }
            }

            var linkDir = link.end - link.start;
            var linkDirNormal = linkDir.normalized;
            var linkLeftNormal = new Vector3(-linkDir.z, linkDir.y, linkDir.x).normalized * (enemyToTheLeft ? 1 : -1);

            var placeholdersInRow = 0;
            var placeholdersRowOffset = 0;
            for (int k = 0; k < regiment.unitsInRegiment.Count; ++k)
            {
                var unit = regiment.unitsInRegiment[k];

                var rowStart = link.start + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));
                var rowEnd = link.end + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));

                var pos = rowStart + ((placeholdersInRow + 1) * unit.boundsRect.x * 1.6f) * linkDirNormal;

                if ((pos - rowStart).magnitude > (rowEnd - rowStart).magnitude)
                {
                    placeholdersRowOffset++;
                    placeholdersInRow = 0;
                    rowStart = link.start + linkLeftNormal * (formationData.betweenRowsDistance * (formationData.rows.Length + placeholdersRowOffset + 1));
                    pos = rowStart + ((placeholdersInRow + 1) * unit.boundsRect.x * 1.6f) * linkDirNormal;
                }

                var rgPlaceholder = new UnitPlaceholder()
                {
                    position = pos,
                    UnitType = unit.type,
                    size = new Vector3(unit.boundsRect.y, 1, unit.boundsRect.x),
                    rotation = Quaternion.LookRotation(linkDirNormal, Vector3.up),
                    actualUnit = unit,
                    enemy = false,
                };

                link.allyPlaceholders.Add(rgPlaceholder);
                link.filledPlaceholders.Add(rgPlaceholder);
                allyPlaceholders.Add(rgPlaceholder);

                placeholdersInRow++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(battleUnitData == null) return;

        Transform[] frontlanePoints = battleUnitData.frontlaneAnchors;
        var frontlanePointsCount = frontlanePoints.Length;
        
        for (int i = 0; i < frontlanePointsCount - 1; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(frontlanePoints[i].position, frontlanePoints[i + 1].position);
        }
        
        for (int i = 0; i < links.Count; ++i)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(links[i].start, links[i].end);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(links[i].start, links[i].start + Vector3.up * 10);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(links[i].end, links[i].end + Vector3.up * 10);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine((links[i].start+links[i].end) /2f, (links[i].start+links[i].end) /2f + Vector3.up * 10);
        }
        
        for (int j = 0; j < enemyPlaceholders.Count; ++j)
        {
            Gizmos.color = Color.red;
            var rotationMatrix = Matrix4x4.TRS(enemyPlaceholders[j].position, enemyPlaceholders[j].rotation, enemyPlaceholders[j].size);
            var matrix = Gizmos.matrix;
            Gizmos.matrix = rotationMatrix;
            if(enemyPlaceholders[j].actualUnit == null)
            {
                Gizmos.DrawWireCube(Vector3.one, Vector3.one);
                Handles.Label(enemyPlaceholders[j].position, $"{enemyPlaceholders[j].UnitType.ToString()}");
            }
            else
            {
                Gizmos.DrawCube(Vector3.one, Vector3.one);
                Handles.Label(enemyPlaceholders[j].position, $"{enemyPlaceholders[j].UnitType.ToString()} from {enemyPlaceholders[j].actualUnit.regimentName}");
            }
            Gizmos.matrix = matrix;
        }
        
        for (int j = 0; j < allyPlaceholders.Count; ++j)
        {
            Gizmos.color = Color.blue;
            var rotationMatrix = Matrix4x4.TRS(allyPlaceholders[j].position, allyPlaceholders[j].rotation, allyPlaceholders[j].size);
            var matrix = Gizmos.matrix;
            Gizmos.matrix = rotationMatrix;
            if (allyPlaceholders[j].actualUnit == null)
            {
                Gizmos.DrawWireCube(Vector3.one, Vector3.one);
                Handles.Label(allyPlaceholders[j].position, $"{allyPlaceholders[j].UnitType.ToString()}");
            }
            else
            {
                Gizmos.DrawCube(Vector3.one, Vector3.one);
                Handles.Label(allyPlaceholders[j].position, $"{allyPlaceholders[j].UnitType.ToString()} from {allyPlaceholders[j].actualUnit.regimentName}");
            }
            Gizmos.matrix = matrix;
        }
    }

    public class Link
    {
        public List<UnitPlaceholder> filledPlaceholders = new List<UnitPlaceholder>();
        
        public List<UnitPlaceholder> enemyPlaceholders = new List<UnitPlaceholder>();
        public List<UnitPlaceholder> allyPlaceholders = new List<UnitPlaceholder>();
        
        public Vector3 start;
        public Vector3 end;
    }
}
