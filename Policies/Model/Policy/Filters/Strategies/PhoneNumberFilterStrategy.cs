namespace Philter.Model.Policy.Filters.Strategies
{
    public class PhoneNumberFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Phone Number";
        }

        public override string GetIdentifierDescription()
        {
            return PhoneNumber.GetDescription();
        }
    }
}
