namespace Philter.Model.Policy.Filters.Strategies
{
    public class PhoneNumberExtensionFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Phone Number Extension";
        }

        public override string GetIdentifierDescription()
        {
            return PhoneNumberExtension.GetDescription();
        }
    }
}
