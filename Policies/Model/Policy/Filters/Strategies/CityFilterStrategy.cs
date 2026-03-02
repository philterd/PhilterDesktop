namespace Philter.Model.Policy.Filters.Strategies
{
    public class CityFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "City";
        }

        public override string GetIdentifierDescription()
        {
            return City.GetDescription();
        }
    }
}
