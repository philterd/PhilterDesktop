namespace Philter.Model.Policy.Filters.Strategies
{
    public class NerFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Entity";
        }

        public override string GetIdentifierDescription()
        {
            return Ner.GetDescription();
        }
    }
}
