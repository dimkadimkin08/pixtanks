using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CommandAddEffect
{
    private static IEnumerable<string> StatusEffects => GameStatusEffectsManager.Singleton.statusEffects
        .Where(statusEffect => !statusEffect.preventCommands && statusEffect.type == GameStatusEffectsManager.EffectType.Tank)
        .Select(statusEffect => statusEffect.name);
    private static string StatusEffectString => string.Join('\n', StatusEffects);

    public static CommandObject commandObject = new()
    {
        name = "addeffect",
        shortHelp = "addeffect    - Apply status effect",
        fullHelp = @"
addeffect - Apply status effect to a tank
addeffect list - Show list of all available status effects

Syntax:
addeffect <effect> <id> <target>

Args:
<effect>   - Effect resource name
<id>       - Any identifier to distinguish your effect from other effects on this target
<target>   - Target tank (netId or plrId or playerName)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length == 1 && flags.Length == 0 && args[0] == "list")
            return $@"addeffect (list):

{StatusEffectString}";
        if (args.Length != 3 || flags.Length > 0)
            return "addeffect: invalid syntax".MarkErrorCommandOutput();

        if (!StatusEffects.Contains(args[0]))
            return $"addeffect: cannot find <effect>: {args[0]}\n(use 'addeffect list' to view available effects)".MarkErrorCommandOutput();

        if (!Regex.IsMatch(args[1], "[a-zA-Z0-9_]+"))
            return $"addeffect: <id> contains unallowed characters (allowed characters: a-z A-Z 0-9 _)".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[2], out var tank))
            return $"addeffect: cannot find <target>: {args[2]}".MarkErrorCommandOutput();

        var existingEffectName = tank.StatusEffects.GetEffectName(args[1]);
        if (existingEffectName != "" && existingEffectName != args[0])
            return $"addeffect: an effect with this id is already applied to the target, but the effect type is different".MarkErrorCommandOutput();

        tank.StatusEffects.ServerApplyStatusEffect(id: args[1], statusEffectName: args[0], fromConnId: null);

        return $"addeffect: applied {args[0]} to tank {tank.netId} (effect id: {args[1]})".MarkSuccessCommandOutput();
    }
}