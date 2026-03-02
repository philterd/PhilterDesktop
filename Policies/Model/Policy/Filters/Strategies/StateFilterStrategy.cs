namespace Philter.Model.Policy.Filters.Strategies
{
    public class StateFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "State";
        }

        public override string GetIdentifierDescription()
        {
            return State.GetDescription();
        }
    }
}
