namespace Philter.Model.Policy.Filters.Strategies
{
    public class UrlFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "URL";
        }

        public override string GetIdentifierDescription()
        {
            return Url.GetDescription();
        }
    }
}
