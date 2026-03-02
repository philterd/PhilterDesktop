namespace Philter.Model.Policy.Filters.Strategies
{
    public class CountyFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "County";
        }

        public override string GetIdentifierDescription()
        {
            return County.GetDescription();
        }
    }
}
