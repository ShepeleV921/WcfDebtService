using System.Collections.Generic;

namespace Tools.Rosreestr
{
    public interface IRosreestrRealEstateSearchResultsPipeline
    {
        List<AddressSearchInfo> Addresses { get; }

        bool NotFound { get; }

        IRosreestrOrderFormPipeline OpenOrderForm(int addressIndex, bool withCaptcha, bool isanul = false);

        IRosreestrRealEstateSearchPipeline ChangeSearchParameters();
    }
}
