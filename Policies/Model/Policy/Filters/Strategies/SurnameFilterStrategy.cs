namespace Philter.Model.Policy.Filters.Strategies
{
    public class SurnameFilterStrategy : BaseFilterStrategy
    {
        public override string GetIdentifierType()
        {
            return "Surname";
        }

        public override string GetIdentifierDescription()
        {
            return Surname.GetDescription();
        }
    }
}
