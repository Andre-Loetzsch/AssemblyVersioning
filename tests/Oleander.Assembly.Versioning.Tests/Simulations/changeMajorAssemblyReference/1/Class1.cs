namespace Oleander.AssemblyVersioning.Test
{
    using System.Text.Json;
    using AutoMapper;

    public class Class1
    {
        public void Method1()
        {
            var options = new JsonSerializerOptions();
            var autoMapAttribute = new AutoMapAttribute(typeof(int));
        }
    }
}
