namespace Philter.Model.Policy.Filters.Strategies
{
    public class IpAddressFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "IP Address";
        }

        public override string GetIdentifierDescription()
        {
            return IpAddress.GetDescription();
        }
    }
}
