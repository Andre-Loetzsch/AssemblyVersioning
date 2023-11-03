namespace Oleander.AssemblyVersioning.Test
{
    public class Class1 //: ITest
    {
        public int Test { get; set; }

        public void DoSomeThing()
        {

        }

        public int Count { get; }
        public string GetName()
        {
            throw new NotImplementedException();
        }

        public string GetName(string s)
        {
            throw new NotImplementedException();
        }
    }

    public interface ITest
    {
        int Count { get; }

        string GetName();

        string GetName(string s);
    }
}