public static class CommandGameRule
{
    public static CommandObject commandObject = new()
    {
        name = "gamerule",
        shortHelp = "gamerule    - Edit game rules (runtime)",
        fullHelp = @"
gamerule - Edit game rules (runtime)

Syntax:
gamerule <param>
gamerule <param> <value>

Args:
<param>  - Shop configuration parameter
<value>  - Value to set for parameter

Params:
priceMultiplier (float)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if ((args.Length != 2 && args.Length != 1) || flags.Length != 0)
            return "gamerule: invalid syntax".MarkErrorCommandOutput();

        object oldValue;
        object currentValue;
        switch (args[0])
        {
            case "priceMultiplier":
                if (args.Length == 1)
                    return $"gamerule: priceMultiplier = {NetworkShop.Singleton.PriceMultiplier}".MarkInfoCommandOutput();
                if (!args[1].TryParseNoLocale(out float priceMultiplier) || priceMultiplier < 0)
                    return $"gamerule: cannot resolve priceMultiplier value {args[1]}\n(must be positive float)".MarkErrorCommandOutput();
                oldValue = NetworkShop.Singleton.PriceMultiplier;
                currentValue = priceMultiplier;
                NetworkShop.Singleton.PriceMultiplier = priceMultiplier;
                break;
            //case "resetUpgrades":
            //    if (args.Length == 1)
            //        return $"gamerule: resetUpgrades = {!GameTanksManager.Singleton.saveAllTanks}".MarkInfoCommandOutput();
            //    if (!bool.TryParse(args[1], out bool resetUpgrades))
            //        return $"gamerule: cannot resolve resetUpgrades value {args[1]}\n(must be boolean: true, false)".MarkErrorCommandOutput();
            //    oldValue = !GameTanksManager.Singleton.saveAllTanks;
            //    currentValue = resetUpgrades;
            //    GameTanksManager.Singleton.saveAllTanks = !resetUpgrades;
            //    break;
            default:
                return $"gamerule: cannot find <param>: {args[0]}".MarkErrorCommandOutput();
        }

        return $"gamerule: {args[0]} is set to: {currentValue}\n(old value: {oldValue})"
            .MarkSuccessCommandOutput();
    }
}