namespace Tools.Rosreestr
{
    /// <summary>
    /// Форма поиска объектов недвижимости
    /// </summary>
    public interface IRosreestrRealEstateSearchPipeline
    {
        IRosreestrRealEstateSearchResultsPipeline SearchAddress(
            string region, string cadastralNumber);

        IRosreestrRealEstateSearchResultsPipeline SearchAddress(
                string region, string district, string city, string street, string home, string corp, string flat);
    }
}
