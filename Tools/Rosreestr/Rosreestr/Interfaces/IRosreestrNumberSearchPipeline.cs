namespace Tools.Rosreestr
{
    public interface IRosreestrNumberSearchPipeline
    {
		bool Found { get; set; }
		RequestDownloadInfo DownloadRequest(string numRequest, string dirPath);
    }
}
