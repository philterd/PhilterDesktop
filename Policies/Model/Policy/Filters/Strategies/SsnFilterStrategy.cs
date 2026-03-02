namespace Philter.Model.Policy.Filters.Strategies
{
    public class SsnFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "SSN";
        }

        public override string GetIdentifierDescription()
        {
            return Ssn.GetDescription();
        }
    }
}
