using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/SpellData")]
public class SpellData : ScriptableObject
{
    public int spellCost;
    public int spellDamage;
    public int spellSpeed;
    public string spellName;
    public bool spellUnlocked;
}