namespace Philter.Model.Policy.Filters.Strategies
{
    public class CreditCardFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Credit Card";
        }

        public override string GetIdentifierDescription()
        {
            return CreditCard.GetDescription();
        }
    }
}
