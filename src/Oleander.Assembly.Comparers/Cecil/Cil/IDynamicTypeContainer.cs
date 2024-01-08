namespace Oleander.Assembly.Comparers.Cecil.Cil
{
    /*Telerik Authorship*/
    public interface IDynamicTypeContainer
    {
        bool IsDynamic { get; }
        bool[] DynamicPositioningFlags { get; set; }
        TypeReference DynamicContainingType { get; }
    }
}
