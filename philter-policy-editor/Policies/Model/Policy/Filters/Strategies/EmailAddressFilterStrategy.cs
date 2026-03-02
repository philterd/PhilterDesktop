namespace Philter.Model.Policy.Filters.Strategies
{
    public class EmailAddressFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Email Address";
        }

        public override string GetIdentifierDescription()
        {
            return EmailAddress.GetDescription();
        }
    }
}
