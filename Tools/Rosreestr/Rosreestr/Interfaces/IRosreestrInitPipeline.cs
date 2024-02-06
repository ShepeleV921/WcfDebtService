namespace Tools.Rosreestr
{
    public interface IRosreestrInitPipeline
    {
        IRosreestrRealEstateSearchPipeline OpenRealEstateSearchForm();

        IRosreestrNumberSearchPipeline OpenNumberSearchFrom();
    }
}
