namespace Philter.Model.Policy.Filters.Strategies
{
    public class HospitalFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Hospital";
        }

        public override string GetIdentifierDescription()
        {
            return Hospital.GetDescription();
        }
    }
}
