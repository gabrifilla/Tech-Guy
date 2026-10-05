public sealed class ProtocolRanks
{
    private readonly int[] _values = new int[7];
    public ProtocolRanks(ProtocolRootContext root)
    { for (int i = 0; i < _values.Length; i++) _values[i] = root.Rank((ProtocolKind)i); }
    public int Rank(ProtocolKind kind) => (int)kind >= 0 && (int)kind < _values.Length ? _values[(int)kind] : 0;
}
