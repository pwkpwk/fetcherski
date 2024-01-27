using fetcherski.client;

namespace fetcherski.clienthost;

public class ItemsOrder(bool descending) : IComparer<Client.Item>
{
    public readonly bool IsDescending = descending;

    int IComparer<Client.Item>.Compare(Client.Item x, Client.Item y)
    {
        int result = x.timestamp.CompareTo(y.timestamp);
        if (result == 0)
        {
            result = x.sid.CompareTo(y.sid);
        }

        if (descending)
        {
            result = -result;
        }

        return result;
    }
}