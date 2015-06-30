namespace NetworkListener
{
    public interface ISearchResponseParser
    {
        ServiceDescription Parse(string searchResponseString);
    }
}