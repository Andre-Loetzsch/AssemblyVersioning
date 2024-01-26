using System.Diagnostics;

namespace Multiple.TargetFrameworks.Test
{
    //[DebuggerDisplay("{Text}")]
    public class Class1
    {
        //[DebuggerStepThrough]
        //[DebuggerNonUserCode]
        //[DebuggerStepperBoundary]
        public void Test(int i1, int i2)
        {
            //
        }


        public int Test3(string text, string text2)
        {
            return 0;
        }

        //public string? Text { get; set; }
        //private string Text2 { get; set;}
    }
}
