using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ManorBoys;
public enum NPCMode
{
    Patrol,
    Standing,
    Inspecting,
    Walking,
    Running,
    Hit
}
public enum SelectionMode
{
    TotalRandom,
    WeightedRandom,
    ClosestPoint
}
public class NPC : MonoBehaviour
{

    [Header("Required Components")]
    [SerializeField] private NavMeshAgent myAgent;
    [SerializeField] private Animator myAnimController;
    [SerializeField] private Transform npcHead;

    [Header("NPC mode")]
    [SerializeField] private NPCMode myMode = NPCMode.Standing;
    [SerializeField] private SelectionMode businessSelection = SelectionMode.TotalRandom;
    [SerializeField] private SelectionMode patrolSelection = SelectionMode.ClosestPoint;
    [SerializeField] private int NPCDetectionMask = 8;

    private float myDetectionRate = 0f;
    private float distanceToPlayer = 0f;

    private Transform nextDestination;
    private bool isBusyNow = false;
    private bool alarmed = false;
    private bool headAlignmentMode = false;
    private Coroutine myLife;
    void Start()
    {
        nextDestination = transform;
        myLife = StartCoroutine(Life());
    }

    private void LateUpdate()
    {
        if(alarmed && headAlignmentMode)
        {
            npcHead.LookAt(GameManager.Instance.GetPlayerHead(), Vector3.up);
        }
    }

    /*for debugging, remove later.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(npcHead.position, GameManager.Instance.GetPlayerHead().position - npcHead.position);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(npcHead.position, GameManager.Instance.GetPlayerTorso().position - npcHead.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(npcHead.position, GameManager.Instance.GetPlayerRoot().position - npcHead.position);
    } */

    private void SelectWork()
    {
        int random = Random.Range(0, 2);
        switch (random)
        {
            case 0:
                StartPatrolling();
                break;
            case 1:
                Idling();
                break;
        }
    }
    private void SetAnimStates(int activeState)
    {
        switch (activeState)
        {
            case 0:
                myAnimController.SetBool("initial", true);
                myAnimController.SetBool("walk", false);
                myAnimController.SetBool("inspect", false);
                myAnimController.SetBool("run", false);
                break;
            case 1:
                myAnimController.SetBool("initial", false);
                myAnimController.SetBool("walk", true);
                myAnimController.SetBool("inspect", false);
                myAnimController.SetBool("run", false);
                break;
            case 2:
                myAnimController.SetBool("initial", false);
                myAnimController.SetBool("walk", false);
                myAnimController.SetBool("inspect", true);
                myAnimController.SetBool("run", false);
                break;
            case 3:
                myAnimController.SetBool("initial", false);
                myAnimController.SetBool("walk", false);
                myAnimController.SetBool("inspect", false);
                myAnimController.SetBool("run", true);
                break;
        }
    }
    private bool IsPlayerDetectable()
    {
        if (GameManager.Instance.GetGameSettings().NPCMaxDetectionDistance > NPCOrganizer.instance.ReturnDistance(npcHead, GameManager.Instance.GetPlayerHead()))
        {
            Vector3 npcForward = this.transform.forward;
            //vertical angle check?
            float angleToPlayer = Vector3.Angle(npcForward, (GameManager.Instance.GetPlayerHead().position - npcHead.position));
            if(angleToPlayer <= GameManager.Instance.GetGameSettings().NPCFieldOfView * 0.5f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
            return false;
    }
    private bool IsTargetVisible(Transform start, Transform end, int layerMask)
    {
        RaycastHit hit;
        Vector3 rayDirection = end.position - start.position;
        if(Physics.Raycast(start.position, rayDirection, out hit,GameManager.Instance.GetGameSettings().NPCMaxDetectionDistance,layerMask))
        {
            //interesting error here. When i cast it via layermask, it can't see the player and prints layermask is 256 and layer of object is 8.
            if (hit.collider.gameObject.layer == layerMask || hit.collider.gameObject.CompareTag("Player"))
            {
                Debug.Log(hit.collider.gameObject.layer + " " + layerMask);
                return true;
            }
            else
            {
                Debug.Log(hit.collider.gameObject.layer);
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    private bool IsTargetVisible(Vector3 start, Vector3 end, int layerMask)
    {
        RaycastHit hit;
        Vector3 rayDirection = end - start;
        if (Physics.Raycast(start, rayDirection, out hit, layerMask))
        {
            if (hit.collider.gameObject.layer == layerMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    private float PlayerDetectionRateCalculation(int layerMask)
    {
        int hitDetectionCounter = 0;
        float detectionRate = 0f;
        //if not in range, no detection
        if (!IsPlayerDetectable())
        {
            return 0;
        }
        //base rate
        else
        {
            detectionRate = 0.4f;
        }
        Transform[] targetPoints = { GameManager.Instance.GetPlayerHead(),
            GameManager.Instance.GetPlayerTorso(),
            GameManager.Instance.GetPlayerRoot()};

        foreach (var i in targetPoints)
        {
            if (IsTargetVisible(npcHead, i, layerMask))
            {
                hitDetectionCounter++;
            }
        }
        //player is too close
        if (NPCOrganizer.instance.ReturnDistance(npcHead, GameManager.Instance.GetPlayerHead()) <= GameManager.Instance.GetGameSettings().NPCDetectionBoostRangeInFov)
        {
            detectionRate += 0.3f;
            //also clearly visible
            if (hitDetectionCounter > 0)
            {
                detectionRate += 0.1f * hitDetectionCounter;
            }
        }
        //player is visible but not too close
        else
        {
            if (hitDetectionCounter > 0)
            {
                detectionRate += 0.15f * hitDetectionCounter;
            }
        }
        return detectionRate;
    }
    private void DetectionTypeOrganizer(float detectRate)
    {
        if(detectRate > 0.4f && detectRate < 0.7f)
        {
            headAlignmentMode = false;
        }
        else if(detectRate >= 0.7f && detectRate <= 0.85f)
        {
            headAlignmentMode = true;
            if(myMode != NPCMode.Inspecting)
            {
                transform.LookAt(GameManager.Instance.GetPlayerRoot(), Vector3.up);
                StartInspecting();
            }
        }
        else if(detectRate > 0.85f)
        {
            if(myMode != NPCMode.Running)
            {
                StartRunning();
            }
        }
    }
    private void SetNextDestination(NPCMode mode)
    {
        if(mode == NPCMode.Patrol)
        {
            switch (patrolSelection)
            {
                case SelectionMode.ClosestPoint:
                    nextDestination = NPCOrganizer.instance.ReturnClosestPatrolPoint(transform);
                    break;
                case SelectionMode.TotalRandom:
                    int rand = Random.Range(0, NPCOrganizer.instance.ReturnListCount());
                    nextDestination = NPCOrganizer.instance.GetPointByIndex(rand);
                    break;
                case SelectionMode.WeightedRandom:
                    break;
            }
        }
        else if (mode == NPCMode.Running)
        {
            nextDestination = GameManager.Instance.GetPlayerRoot();
            myAgent.SetDestination(nextDestination.position);
        }
        else if (mode == NPCMode.Inspecting || mode == NPCMode.Standing)
        {
            nextDestination = transform;
            myAgent.ResetPath();
        }
    }
    private bool IsDestinationReached()
    {
        bool done = false;
        if (!myAgent.pathPending)
        {
            if (myAgent.remainingDistance <= myAgent.stoppingDistance)
            {
                if (!myAgent.hasPath || myAgent.velocity.sqrMagnitude == 0f)
                {
                    done = true;
                }
            }
        }
        return done;
    }
    private void StartPatrolling()
    {
        isBusyNow = true;
        myMode = NPCMode.Patrol;
        SetAnimStates(1);
        myAgent.speed = GameManager.Instance.GetGameSettings().NPCWalkSpeed;
        StartCoroutine(Patrolling());
    }
    private void Idling()
    {
        isBusyNow = true;
        myMode = NPCMode.Standing;
        SetAnimStates(0);
        SetNextDestination(NPCMode.Standing);
        StartCoroutine(Idle());
    }
    private void StartRunning()
    {
        isBusyNow = true;
        myMode = NPCMode.Running;
        SetAnimStates(3);
        myAgent.speed = GameManager.Instance.GetGameSettings().NPCRunSpeed;
        StartCoroutine(Running());
    }
    private void StartInspecting()
    {
        isBusyNow = true;
        myMode = NPCMode.Inspecting;
        SetAnimStates(2);
        SetNextDestination(NPCMode.Inspecting);
        StartCoroutine(Inspecting());
    }
    private IEnumerator Life()
    {
        while (true)
        {
            //is there somebody in it's vision?
            var dist = IsPlayerDetectable();
            distanceToPlayer = NPCOrganizer.instance.ReturnDistance(npcHead, GameManager.Instance.GetPlayerHead());
            if (dist && !alarmed)
            {
                alarmed = true;
            }
            else if(dist && alarmed)
            {
                myDetectionRate = PlayerDetectionRateCalculation(1 << NPCDetectionMask);
            }
            else
            {
                alarmed = false;
                myDetectionRate = 0;
                headAlignmentMode = false;
            }
            //is it doing something?
            if (!isBusyNow && !alarmed)
            {
                SelectWork();
            }
            //it caught something
            else if(alarmed && myDetectionRate > 0.4f)
            {
                DetectionTypeOrganizer(myDetectionRate);
            }


            yield return new WaitForSeconds(0.2f);
        }
    }
    private IEnumerator Inspecting()
    {
        while(alarmed && myMode != NPCMode.Running)
        {
            yield return new WaitForSeconds(0.2f);
        }
        isBusyNow = false;
        yield break;
    }
    private IEnumerator Running()
    {
        yield return new WaitForSeconds(0.5f);
        SetNextDestination(NPCMode.Running);

        while (alarmed && !IsDestinationReached())
        {
            //maybe we can do some additional things.
            if (IsDestinationReached())
            {
                //hit
                nextDestination = transform;
                myAnimController.Play("Hit");
                yield return new WaitForSeconds(2f);
                isBusyNow = false;
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
        isBusyNow = false;
        yield break;
    }
    private IEnumerator Idle()
    {
        float random = Random.Range(GameManager.Instance.GetGameSettings().NPCMaxWaitTime / 2, GameManager.Instance.GetGameSettings().NPCMaxWaitTime);
        float currTime = 0f;
        while(currTime < random && myMode == NPCMode.Standing)
        {
            currTime += 1;
            yield return new WaitForSeconds(1f);
        }
        isBusyNow = false;
        yield break;
    }
    private IEnumerator Patrolling()
    {
        float random = Random.Range(GameManager.Instance.GetGameSettings().NPCMaxWaitTime / 2, GameManager.Instance.GetGameSettings().NPCMaxWaitTime);
        float currTime = 0f;
        if (nextDestination == null || nextDestination == transform)
        {
            SetNextDestination(NPCMode.Patrol);
            myAgent.SetDestination(nextDestination.position);
        }
        while(myMode == NPCMode.Patrol)
        {
            if(myMode == NPCMode.Patrol && currTime < random)
            {
                if (nextDestination != transform && nextDestination != null)
                {
                    if (IsDestinationReached())
                    {
                        int index = NPCOrganizer.instance.ReturnIndexFromList(nextDestination);
                        nextDestination = NPCOrganizer.instance.GetPointByIndex(index + 1);
                        myAgent.SetDestination(nextDestination.position);
                    }
                    else
                    {
                        currTime += 0.2f;
                        yield return new WaitForSeconds(0.2f);
                    }
                }
            }
            else
            {
                nextDestination = transform;
                isBusyNow = false;
                yield break;
            }
        }
    }
}
