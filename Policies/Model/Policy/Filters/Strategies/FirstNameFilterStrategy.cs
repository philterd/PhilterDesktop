namespace Philter.Model.Policy.Filters.Strategies
{
    public class FirstNameFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "First Name";
        }

        public override string GetIdentifierDescription()
        {
            return FirstName.GetDescription();
        }
    }
}
