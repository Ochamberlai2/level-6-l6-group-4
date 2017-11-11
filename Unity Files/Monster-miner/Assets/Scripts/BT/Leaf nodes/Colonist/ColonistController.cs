using System;
using UnityEngine;
using UnityEngine.AI;

public enum ColonistJobType
{
    Hunter,
    Builder,
    Scout,
    Farmer,
    Blacksmith,
}

[RequireComponent(typeof(NavMeshAgent))]
public class ColonistController : MonoBehaviour {

    #region Variables
    #region basic info
    public string colonistName;
    public float colonistBaseMoveSpeed = 5f;
    public float colonistMoveSpeed;

    public float colonistBaseWorkSpeed = 15f;
    public float colonistWorkSpeed;

    public int maxHealth = 100;
    public float health;

    public int requiredNutritionPerDay = 25;
    public GameTime timeOfNextMeal;
    #endregion
    #region Job
    public ColonistJobType colonistJob;
    public Job currentJob;
    public GameTime lastWorked;
    #endregion
    #region Equipment and combat
    public float nextAttack;
    public MonsterController target;

    public SkinnedMeshRenderer colonistBodyMesh;

    public Weapon colonistWeapon;
    public Armour[] equippedArmour;
    public SkinnedMeshRenderer[] equippedItemMeshes;
    public float damageReduction = 0;
    
    #endregion
    #region misc
    [HideInInspector]
    public bool hasPath;

    [HideInInspector]
    public new Collider collider;

    [HideInInspector]
    public GameObject gathererStockpile;

    NavMeshAgent agent;

    [HideInInspector]
    public bool selected;

    [Header("Selection Circle must be first child")]
    public Projector selectionCircle;

    public NavMeshAgent NavMeshAgent
    {
        get
        {
            return agent;
        }
        private set
        {
            agent = value;
        }
    }
    [HideInInspector]
    public float wanderRepathTimer;
    [HideInInspector]
    public float wanderTimer;

    public float wanderRadius = 4f;
    #endregion
    #endregion

    private void Start()
    {
        BehaviourTreeManager.Colonists.Add(this);

        collider = gameObject.GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();

        lastWorked = TimeManager.Instance.IngameTime;


        health = maxHealth;
        colonistMoveSpeed = colonistBaseMoveSpeed;
        colonistWorkSpeed = colonistBaseWorkSpeed;
        UpdateMoveSpeed(colonistBaseMoveSpeed);
        //set the selection cirlce
        selectionCircle = transform.GetChild(0).GetComponent<Projector>();
        SetTimeOfNextMeal();
        equippedArmour = new Armour[Enum.GetNames(typeof(ArmourSlot)).Length-1];
        equippedItemMeshes = new SkinnedMeshRenderer[Enum.GetNames(typeof(ArmourSlot)).Length - 1];
        if(gameObject.name == "Colonist")
        colonistBodyMesh = gameObject.transform.Find("Graphics").Find("BodyMesh").GetComponent<SkinnedMeshRenderer>();
    }

    public void OnDrawGizmos()
    {
        if(colonistWeapon != null && colonistJob == ColonistJobType.Hunter)
        Gizmos.DrawWireSphere(transform.position, colonistWeapon.Range);
    }

    public void SetTimeOfNextMeal()
    {
        GameTime time = TimeManager.Instance.IngameTime;
        time.Date.x++;
        //figure out if we need to increment the date at all
        if (time.Date.x >= 28)
        {
            time.Date.y += 1;
            time.Date.x = 1;
            //if we get to 1 year
            if (time.Date.y >= 12)
            {
                time.Date.z += 1;
                time.Date.y = 1;
            }
        }
        time.hours = 6;

        timeOfNextMeal = time;
    }

    public void TakeDamage(float damage)
    {
        //find the damage we resisted (converting damage reduction to a value between 0 and 1)
        float resistedDamage = damage * (damageReduction / 100);

        health -= (damage - resistedDamage);

        if (CheckDead())
            Death();
    }

    public bool CheckDead()
    {
        if (health < 0)
            return true;
        return false;
    }

    void Death()
    {
        Debug.Log(colonistName + " has died.");
    }
    public void UpdateMoveSpeed(float MoveSpeed)
    {
        //store the old move base move speed
        float OldBase = colonistBaseMoveSpeed;
        //then update the move speed
        colonistBaseMoveSpeed = MoveSpeed;
        //figure out the difference between the old and current move speeds
        float diffBaseCurrent = OldBase - colonistMoveSpeed;
        //then apply the new move speed with the current move speed penalty enacted
        colonistMoveSpeed = colonistBaseMoveSpeed - diffBaseCurrent;
        //and update the navmeshagent
        GetComponent<NavMeshAgent>().speed = colonistMoveSpeed;
    }
    public void UpdateWorkSpeed(float WorkSpeed)
    {
        //store the old base work speed
        float OldBase = colonistBaseWorkSpeed;
        //update the base work speed
        colonistBaseWorkSpeed = WorkSpeed;
        //then figure out the difference between the old base and the current speed
        float diffBaseCurrent = OldBase - colonistWorkSpeed;
        //and apply the  new work speed with the current pentalty enacted
        colonistWorkSpeed = colonistBaseWorkSpeed - diffBaseCurrent;
    }

    public void UpdateDamageResistance(Armour oldArmour, Armour newArmour)
    {
        //regardless of if we have a new armour piece we need to be reducing the DR of the old piece
        damageReduction -= oldArmour.DamageReduction;
        //then if we have new armour to swap out
        if(newArmour != null)
        {
            //we increase the DR again
            damageReduction += newArmour.DamageReduction;
        }
        Mathf.Clamp(damageReduction, 0, 100);
    }
    //inspired by https://www.youtube.com/watch?v=ZBLvKR2E62Q&t=401s
    public void EquipWearable(Wearable wearable)
    {
        int slotIndex = (int)wearable.armourSlot;
        if (slotIndex != (int)ArmourSlot.Weapon)
        {
            //check to see if the colonist is wearing armour
            if (equippedArmour[slotIndex] != null)
            {
                //if so get the new DR
                UpdateDamageResistance(equippedArmour[slotIndex], wearable as Armour);
            }
            //then just make the corresponding slot contain the new armour info
            equippedArmour[slotIndex] = Instantiate(wearable) as Armour;
            SkinnedMeshRenderer newMesh = Instantiate(wearable.equippableMesh);
            newMesh.transform.parent = colonistBodyMesh.transform;
            newMesh.bones = colonistBodyMesh.bones;
            newMesh.rootBone = colonistBodyMesh.rootBone;
            equippedItemMeshes[slotIndex] = newMesh;
           
          
        }
        else
        {
            colonistWeapon = wearable as Weapon;
        }
       


    }
    public void UnequipArmour(ArmourSlot slot)
    {
        int slotIndex = (int)slot;
        //if there is no armour, we shouldnt be unequipping anyway
       if(equippedArmour[slotIndex] != null)
        {
            if(equippedItemMeshes[slotIndex] != null)
            {
                Destroy(equippedItemMeshes[slotIndex].gameObject);
            }
            UpdateDamageResistance(equippedArmour[slotIndex],null);
            equippedArmour[slotIndex] = null;
        }
    }

}
