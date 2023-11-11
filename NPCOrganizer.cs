using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ManorBoys
{
    public class NPCOrganizer : MonoBehaviour
    {
        public static NPCOrganizer instance { get; private set; }

        public void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }
        [SerializeField] private List<Transform> patrolZoneList = new List<Transform>();

        public Transform GetStartPatrolPoint()
        {
            if (patrolZoneList.Count > 0)
            {
                return patrolZoneList[0];
            }
            else
            {
                return null;
            }
        }
        public Transform GetPointByIndex(int index)
        {
            if (index < patrolZoneList.Count)
            {
                return patrolZoneList[index];
            }
            else if (index >= patrolZoneList.Count)
            {
                return patrolZoneList[index % patrolZoneList.Count];
            }
            else
            {
                return null;
            }
        }
        public int ReturnIndexFromList(Transform trans)
        {
            int returning = 0;
            bool found = false;
            for (int i = 0; i < patrolZoneList.Count; i++)
            {
                if (trans == patrolZoneList[i])
                {
                    found = true;
                    returning = i;
                }
            }
            if (found)
            {
                return returning;
            }
            else
            {
                return 0;
            }
        }
        public Transform ReturnClosestPatrolPoint(Transform point)
        {
            float distance = 0;
            Transform temp = point;
            for (int i = 0; i < patrolZoneList.Count; i++)
            {
                float temdistance = (patrolZoneList[i].position - point.position).magnitude;
                if (i == 0)
                {
                    distance = temdistance;
                    temp = patrolZoneList[i];
                }
                else
                {
                    if (temdistance <= distance)
                    {
                        distance = temdistance;
                        temp = patrolZoneList[i];
                    }
                }
            }
            return temp;
        }
        public int ReturnListCount()
        {
            return patrolZoneList.Count;
        }
        public float ReturnDistance(Transform trans1, Transform trans2)
        {
            return new Vector3(trans1.position.x - trans2.position.x, trans1.position.y - trans2.position.y, trans1.position.z - trans2.position.z).magnitude;
        }
    }

}
