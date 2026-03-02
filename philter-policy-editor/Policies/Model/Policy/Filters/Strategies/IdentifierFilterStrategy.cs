namespace Philter.Model.Policy.Filters.Strategies
{
    public class IdentifierFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Identifier";
        }

        public override string GetIdentifierDescription()
        {
            return Identifier.GetDescription();
        }
    }
}
