namespace Monster.AI.Blackboard
{
    public readonly struct BBKey<T>
    {
        public readonly string Name;
        public BBKey(string name) => Name = name;
        public override string ToString() => Name;
    }
}