using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class Networkable : Attribute { /* empty class just for handling Network-able methods */
    public readonly int packetHash;

    public Networkable(string packetName) {
        this.packetHash = packetName.GetHashCode();
    }
}