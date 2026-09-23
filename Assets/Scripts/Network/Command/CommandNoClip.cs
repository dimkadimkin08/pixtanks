public static class CommandNoClip
{
    public static CommandObject commandObject = new()
    {
        name = "noclip",
        shortHelp = "noclip   - disable collision for tank",
        fullHelp = @"
noclip - disable layer collision for tank

Syntax:
noclip <target> <value> <layer>
noclip listlayer

Args:
<target>      - Tank netId or plrId or player name
<value>       - 1 to disable collisions; 0 to enable
<layer>       - layer number or name (empty for all layers)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if ((args.Length != 1 && args.Length != 2 && args.Length != 3) || flags.Length != 0)
            return "noclip: invalid syntax".MarkErrorCommandOutput();

        var layersCount = 0;
        for (int i = 0; i < 32; i++)
            if (!string.IsNullOrEmpty(UnityEngine.LayerMask.LayerToName(i)))
                layersCount = i;

        if (args[0] == "listlayer")
        {
            var layersText = "";
            for (int i = 0; i <= layersCount; i++)
            {
                var layerName = UnityEngine.LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                    layersText += $"\n{i} - {layerName}";
            }
            return $"noclip listlayer:{layersText}".MarkInfoCommandOutput();
        }
        else if (args.Length == 1)
            return "noclip: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"noclip: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        var noclip = args[1] == "1";

        int layer = -1;

        if (args.Length == 3)
        {
            if (int.TryParse(args[2], out layer) && layer <= layersCount)
            {
                if (noclip)
                    tank.Rigidbody.excludeLayers |= (1 << layer);
                else
                    tank.Rigidbody.excludeLayers &= ~(1 << layer);
            }
            else
            {
                layer = UnityEngine.LayerMask.NameToLayer(args[2]);
                if (layer >= 0)
                {
                    if (noclip)
                        tank.Rigidbody.excludeLayers |= (1 << layer);
                    else
                        tank.Rigidbody.excludeLayers &= ~(1 << layer);
                }
                else
                    return $"noclip: invalid <layer>: {args[2]}".MarkErrorCommandOutput();
            }
        }
        else
            tank.Rigidbody.excludeLayers = noclip ? ~0 : 0;

        var finalLayerName = layer >= 0 ? UnityEngine.LayerMask.LayerToName(layer) : "";
        return $"noclip (tank: {tank.netId}): noclip {(noclip ? "enabled" : "disabled")} {(layer >= 0 ? $" for layer \"{finalLayerName}\"" : " for all layers")}"
            .MarkSuccessCommandOutput();
    }
}