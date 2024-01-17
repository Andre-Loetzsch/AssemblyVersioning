namespace Oleander.AssemblyVersioning.Test
{
    public class Class1
    {
        public event Action? DoIgnore;

        public void Method1(string s)
        {
        }

        [Obsolete]
        public void Method2()
        {
        }

        public int Count { get; set; }
    }
}
