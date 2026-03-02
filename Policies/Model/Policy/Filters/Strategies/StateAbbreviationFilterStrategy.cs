namespace Philter.Model.Policy.Filters.Strategies
{
    public class StateAbbreviationFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "State Abbreviation";
        }

        public override string GetIdentifierDescription()
        {
            return StateAbbreviation.GetDescription();
        }
    }
}
