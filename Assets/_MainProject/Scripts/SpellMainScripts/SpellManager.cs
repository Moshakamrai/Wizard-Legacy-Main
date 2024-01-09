using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public SpellData[] spells;

    void Start()
    {
        LoadSpellProgress();
    }

    public void UnlockSpell(int spellIndex)
    {
        spells[spellIndex].spellUnlocked = true;
        SaveSpellProgress();
    }

    void SaveSpellProgress()
    {
        for (int i = 0; i < spells.Length; i++)
        {
            PlayerPrefs.SetInt($"SpellUnlocked_{i}", spells[i].spellUnlocked ? 1 : 0);
        }
    }

    void LoadSpellProgress()
    {
        for (int i = 0; i < spells.Length; i++)
        {
            spells[i].spellUnlocked = PlayerPrefs.GetInt($"SpellUnlocked_{i}", 0) == 1;
        }
    }
}
