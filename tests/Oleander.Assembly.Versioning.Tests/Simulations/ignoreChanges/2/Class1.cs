namespace Oleander.AssemblyVersioning.Test
{
    public class Class1
    {
        public event Action<int>? DoIgnore;
        public event EventHandler<string>? DoIgnore2;

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
