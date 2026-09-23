using System;

public class CommandObject
{
    public string name;
    public string shortHelp;
    public string fullHelp;
#nullable enable
    public Func<string>? additionalFullHelp = null;
#nullable disable
    public ExecuteDelegate execute;
    public bool hideFromDebugLog = false;

    public delegate string ExecuteDelegate(string[] arguments, string[] flags);
}