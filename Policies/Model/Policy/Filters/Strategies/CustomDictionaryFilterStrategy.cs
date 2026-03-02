namespace Philter.Model.Policy.Filters.Strategies
{
    public class CustomDictionaryFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Custom Dictionary";
        }

        public override string GetIdentifierDescription()
        {
            return CustomDictionary.GetDescription();
        }
    }
}
