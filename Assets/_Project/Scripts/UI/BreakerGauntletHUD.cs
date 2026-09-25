using UnityEngine;

[RequireComponent(typeof(BreakerGauntletCombat), typeof(AbilityHolder))]
public sealed class BreakerGauntletHUD : MonoBehaviour
{
    private BreakerGauntletCombat _combat;
    private AbilityHolder _holder;

    private void Awake()
    {
        _combat = GetComponent<BreakerGauntletCombat>();
        _holder = GetComponent<AbilityHolder>();
    }

    private void OnGUI()
    {
        if (!_combat || !_combat.isActiveAndEnabled || !_combat.IsEquipped) return;
        float width = Mathf.Min(620f, Screen.width - 20f);
        var area = new Rect((Screen.width - width) * 0.5f, Screen.height - 105f, width, 95f);
        GUI.Box(area, "");
        GUILayout.BeginArea(area);
        GUILayout.Label(_combat.IsReady ? "ASURA PRONTO - use a Rajada!" :
            $"ASURA {_combat.Energy}/100 - alterne Impulso e Choque");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < _holder.ActiveAbilities.Count; i++)
        {
            if (!(_holder.ActiveAbilities[i] is BreakerGauntletAbility ability)) continue;
            float cooldown = _holder.GetRemainingCooldown(i);
            string status = _holder.IsAbilityActive(i) ? "Executando" : cooldown > 0f ? $"{cooldown:0.0}s" :
                ability.AsuraBurst && !_combat.IsReady ? "100 energia" : "Pronto";
            GUILayout.Label($"[{_holder.GetAbilityKey(i)}] {ability.name}\n{status}", GUILayout.Width((width - 12f) / 4f));
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
