namespace EDIVE.External.DomainReloadHelper
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class DomainReloadHelperAttribute : System.Attribute
    {
        public int Order { get; }
        
        public DomainReloadHelperAttribute(int order) { Order = order; }

        public DomainReloadHelperAttribute() { Order = 0; }
    }
}
