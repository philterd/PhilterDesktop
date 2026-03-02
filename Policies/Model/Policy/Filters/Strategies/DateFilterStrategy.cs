namespace Philter.Model.Policy.Filters.Strategies
{
    public class DateFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Date";
        }

        public override string GetIdentifierDescription()
        {
            return Date.GetDescription();
        }
    }
}
