namespace Philter.Model.Policy.Filters.Strategies
{
    public class VinFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "VIN";
        }

        public override string GetIdentifierDescription()
        {
            return Vin.GetDescription();
        }
    }
}
