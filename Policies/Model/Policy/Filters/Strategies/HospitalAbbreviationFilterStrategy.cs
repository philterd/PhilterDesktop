namespace Philter.Model.Policy.Filters.Strategies
{
    public class HospitalAbbreviationFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Hospital Abbreviation";
        }

        public override string GetIdentifierDescription()
        {
            return HospitalAbbreviation.GetDescription();
        }
    }
}
