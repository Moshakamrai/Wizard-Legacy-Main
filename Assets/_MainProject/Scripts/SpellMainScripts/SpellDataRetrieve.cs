using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellDataRetrieve : MonoBehaviour
{
    [SerializeField]
    private SpellData spellDatas;

    public int cost;
    public int damage;
    public int speed;
    public string nameSpell;
    public bool unlocked;
    public RawImage image;
    public void RetrieveSpellData()
    {
        // Access and store the values from the SpellData scriptable object
        cost = spellDatas.spellCost;
        damage = spellDatas.spellDamage;
        speed = spellDatas.spellSpeed;
        nameSpell = spellDatas.spellName;
        unlocked = spellDatas.spellUnlocked;
        image = spellDatas.spellImage;

        
    }
    private void Start()
    {
        RetrieveSpellData();
    }

}
