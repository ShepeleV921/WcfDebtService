namespace Tools.Rosreestr
{
    /// <summary>
    /// Форма заказа выписки + доп. информация по адресу
    /// </summary>
    public interface IRosreestrOrderFormPipeline
    {
        string ResolvedCaptcha { get; set; }

        string RequestNumber { get; }

        bool HasSuccess { get; }

        bool CaptchaError { get; }

        bool HasTimeout { get; }

        bool IsAnnul { get; }

        IRosreestrOrderFormPipeline AddCaptcha();

        IRosreestrOrderFormPipeline SaveCaptcha(string path);

        IRosreestrOrderFormPipeline Send();

        IRosreestrRealEstateSearchResultsPipeline Close();

        IRosreestrRealEstateSearchResultsPipeline Continue();

        IRosreestrOrderFormPipeline ChangeCaptcha();

        /// <summary>
        /// Ввод капчи
        /// </summary>
        /// <param name="value">Значение капчи</param>
        /// <returns></returns>
        IRosreestrOrderFormPipeline EnterCaptcha(string value);

        /// <summary>
        /// Запросить сведения
        /// </summary>
        /// <param name="num">
        /// 1 - Запросить сведения об объекте;
        /// 2 - Запросить сведения о переходе прав на объект</param>
        /// <returns></returns>
        IRosreestrOrderFormPipeline CheckRequestObject(int num);
    }
}
