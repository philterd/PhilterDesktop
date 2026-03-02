namespace Philter.Model.Policy.Filters.Strategies
{
    public class AgeFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Age";
        }

        public override string GetIdentifierDescription()
        {
            return Age.GetDescription();
        }
    }
}
