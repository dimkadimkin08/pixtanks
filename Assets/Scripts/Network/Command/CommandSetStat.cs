using System.Linq;
using System.Text.RegularExpressions;

public static class CommandSetStat
{
    private static readonly string[] allowedFlags = new string[] { "inc", "dec" };

    public static CommandObject commandObject = new()
    {
        name = "setstat",
        shortHelp = "setstat    - Set tank parameter to specified value",
        fullHelp = @"
setstat - Set tank parameter to specified value

Syntax:
setstat <target> <stat> <value> <flags>

Args:
<target>   - Tank netId or plrId
<stat>     - parameter name
<value>    - value for parameter

Stats (parameter names):
gears
hp
maxHp
damage
speed
reload
incomeDamage
shield_<id>

Flags:
-inc       - increase stat instead of set stat
-dec       - decrease stat instead of set stat",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 3 || flags.Length > 0 && !flags.IsFits(allowedFlags))
            return "setstat: invalid syntax".MarkErrorCommandOutput();

        var increase = flags.Contains("inc");
        var decrease = flags.Contains("dec");

        if (increase && decrease)
            return "setstat: cannot use -inc with -dec".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"setstat: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        object oldParameter;
        object currentParameter;

        var shieldRegex = new Regex("shield_([a-zA-Z0-9]+)$");
        if (shieldRegex.IsMatch(args[1]))
        {
            var shieldId = shieldRegex.Match(args[1]).Groups[1].Value;
            if (!args[2].TryParseNoLocale(out ushort shieldValue))
                return $"setstat: cannot resolve shield value {args[2]}\n(must be an unsigned short integer)".MarkErrorCommandOutput();
            tank.Parameters.shields.TryGetValue(shieldId, out var oldShieldValue);
            oldParameter = oldShieldValue;
            if (increase)
                tank.Parameters.shields[shieldId] = oldShieldValue.Add(shieldValue);
            else if (decrease)
                tank.Parameters.shields[shieldId] = oldShieldValue.Sub(shieldValue);
            else
                tank.Parameters.shields[shieldId] = shieldValue;
            tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue);
            currentParameter = currentShieldValue;

            return $"setstat (tank: {tank.netId}):\nshield with id \"{shieldId}\" is set to: {currentParameter}\n(previous value: {oldParameter})"
                .MarkSuccessCommandOutput();
        }

        switch (args[1])
        {
            case "gears":
                if (!args[2].TryParseNoLocale(out ushort gearsValue))
                    return $"setstat: cannot resolve gears value {args[2]}\n(must be an unsigned short integer)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.Gears;
                if (increase)
                    tank.Parameters.Gears = tank.Parameters.Gears.Add(gearsValue);
                else if (decrease)
                    tank.Parameters.Gears = tank.Parameters.Gears.Sub(gearsValue);
                else
                    tank.Parameters.Gears = gearsValue;
                currentParameter = tank.Parameters.Gears;
                break;
            case "hp":
                if (!args[2].TryParseNoLocale(out ushort hpValue))
                    return $"setstat: cannot resolve hp value {args[2]}\n(must be an unsigned short integer)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.CurrentHp;
                ushort newValue;
                if (increase)
                    newValue = tank.Parameters.CurrentHp.Add(hpValue);
                else if (decrease)
                    newValue = tank.Parameters.CurrentHp.Sub(hpValue);
                else
                    newValue = hpValue;
                if (newValue > tank.Parameters.MaxHp)
                    tank.Parameters.MaxHp = newValue;
                tank.Parameters.CurrentHp = newValue;
                currentParameter = newValue;
                break;
            case "maxHp":
                if (!args[2].TryParseNoLocale(out ushort maxHpValue))
                    return $"setstat: cannot resolve maxHp value {args[2]}\n(must be an unsigned short integer)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.MaxHp;
                if (increase)
                    tank.Parameters.MaxHp = tank.Parameters.MaxHp.Add(maxHpValue);
                else if (decrease)
                    tank.Parameters.MaxHp = tank.Parameters.MaxHp.Sub(maxHpValue);
                else
                    tank.Parameters.MaxHp = maxHpValue;
                currentParameter = tank.Parameters.MaxHp;
                break;
            case "damage":
                if (!args[2].TryParseNoLocale(out float damageValue))
                    return $"setstat: cannot resolve damage value {args[2]}\n(must be a float number)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.DamageMultiplier;
                if (increase)
                    tank.Parameters.DamageMultiplier += damageValue;
                else if (decrease)
                    tank.Parameters.DamageMultiplier -= damageValue;
                else
                    tank.Parameters.DamageMultiplier = damageValue;
                currentParameter = tank.Parameters.DamageMultiplier;
                break;
            case "speed":
                if (!args[2].TryParseNoLocale(out float speedValue))
                    return $"setstat: cannot resolve speed value {args[2]}\n(must be a float number)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.SpeedMultiplier;
                if (increase)
                    tank.Parameters.SpeedMultiplier += speedValue;
                else if (decrease)
                    tank.Parameters.SpeedMultiplier -= speedValue;
                else
                    tank.Parameters.SpeedMultiplier = speedValue;
                currentParameter = tank.Parameters.SpeedMultiplier;
                break;
            case "reload":
                if (!args[2].TryParseNoLocale(out float reloadValue))
                    return $"setstat: cannot resolve reload value {args[2]}\n(must be a float number)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.ReloadMultiplier;
                if (increase)
                    tank.Parameters.ReloadMultiplier += reloadValue;
                else if (decrease)
                    tank.Parameters.ReloadMultiplier -= reloadValue;
                else
                    tank.Parameters.ReloadMultiplier = reloadValue;
                currentParameter = tank.Parameters.ReloadMultiplier;
                break;
            case "incomeDamage":
                if (!args[2].TryParseNoLocale(out float incomingDamageValue))
                    return $"setstat: cannot resolve incomeDamage value {args[2]}\n(must be a float number)".MarkErrorCommandOutput();
                oldParameter = tank.Parameters.IncomingDamageMultiplier;
                if (increase)
                    tank.Parameters.IncomingDamageMultiplier += incomingDamageValue;
                else if (decrease)
                    tank.Parameters.IncomingDamageMultiplier -= incomingDamageValue;
                else
                    tank.Parameters.IncomingDamageMultiplier = incomingDamageValue;
                currentParameter = tank.Parameters.IncomingDamageMultiplier;
                break;
            default:
                return $"setstat: unknown <stat>: {args[1]}\n(Allowed stats: hp, maxHp, damage, speed, reload, incomeDamage, shield_<id>)"
                    .MarkErrorCommandOutput();
        }

        return $"setstat (tank: {tank.netId}):\n{args[1]} is set to: {currentParameter}\n(old value: {oldParameter})"
            .MarkSuccessCommandOutput();
    }
}